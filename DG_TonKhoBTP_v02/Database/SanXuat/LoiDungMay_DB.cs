using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    internal static class LoiDungMay_DB
    {
        private const string DinhDangNgay = "yyyy-MM-dd";
        private const string TenLoiLamViecKhac = "Làm việc khác";

        /// <summary>
        /// Lấy các công đoạn có ít nhất một máy.
        /// Thứ tự hiển thị bám theo id đã lưu trong database.
        /// </summary>
        public static List<DanhSachCongDoan_Model> GetDanhSachCongDoanCoMay()
        {
            const string sql = @"
                SELECT
                    cd.id,
                    cd.MaCongDoan,
                    cd.TenCongDoan
                FROM DanhSachCongDoan cd
                WHERE EXISTS
                (
                    SELECT 1
                    FROM DanhSachMay m
                    WHERE m.DanhSachCongDoan_MaCongDoan = cd.MaCongDoan
                )
                ORDER BY cd.id ASC;";

            var result = new List<DanhSachCongDoan_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new DanhSachCongDoan_Model
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        MaCongDoan = Convert.ToInt32(reader["MaCongDoan"]),
                        TenCongDoan = Convert.ToString(reader["TenCongDoan"]) ?? string.Empty
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách máy theo MaCongDoan, sắp xếp theo id đã lưu trong database.
        /// </summary>
        public static List<DanhSachMay_Model> GetDanhSachMayTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT
                    id,
                    DanhSachCongDoan_MaCongDoan,
                    TenMay
                FROM DanhSachMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";

            var result = new List<DanhSachMay_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new DanhSachMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"]),
                            TenMay = Convert.ToString(reader["TenMay"]) ?? string.Empty
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách lỗi dừng máy theo MaCongDoan.
        /// "Làm việc khác" được lấy trực tiếp từ database như các lỗi khác.
        /// </summary>
        public static List<TenLoiDungMay_Model> GetDanhSachTenLoiTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT
                    id,
                    TenLoi,
                    MoTaLoi,
                    DanhSachCongDoan_MaCongDoan
                FROM TenLoiDungMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";

            var result = new List<TenLoiDungMay_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TenLoiDungMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            TenLoi = Convert.ToString(reader["TenLoi"]) ?? string.Empty,
                            MoTaLoi = reader["MoTaLoi"] == DBNull.Value
                                ? string.Empty
                                : Convert.ToString(reader["MoTaLoi"]) ?? string.Empty,
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"])
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Kiểm tra tổ hợp Ngày + Máy + Ca đã có ít nhất một bản ghi hay chưa.
        /// </summary>
        public static bool DaCoDuLieuTheoMayNgayCa(DateTime ngay, int danhSachMayId, int ca)
        {
            const string sql = @"
                SELECT EXISTS
                (
                    SELECT 1
                    FROM DanhSachLoiDungMay
                    WHERE Ngay = @Ngay
                      AND DanhSachMay_ID = @DanhSachMay_ID
                      AND Ca = @Ca
                    LIMIT 1
                );";

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Ngay", ngay.Date.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@DanhSachMay_ID", danhSachMayId);
                cmd.Parameters.AddWithValue("@Ca", ca);

                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
            }
        }

        /// <summary>
        /// Lấy toàn bộ bản ghi đã lưu của cùng Ngày + Máy + Ca.
        /// Dùng để hiển thị lại dữ liệu trên UI và đồng bộ khi lưu.
        /// </summary>
        public static List<DanhSachLoiDungMay_Model> GetDanhSachDaLuuTheoMayNgayCa(
            DateTime ngay,
            int danhSachMayId,
            int ca)
        {
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            {
                return GetDanhSachDaLuuTheoMayNgayCa(conn, null, ngay, danhSachMayId, ca);
            }
        }

        /// <summary>
        /// Tính số phút dừng máy.
        /// Nếu kết thúc nhỏ hơn bắt đầu thì hiểu là kết thúc ở ngày kế tiếp.
        /// Bắt đầu bằng kết thúc là không hợp lệ.
        /// </summary>
        public static bool TryTinhThoiGianDung(
            TimeSpan thoiGianBatDau,
            TimeSpan thoiGianKetThuc,
            out int soPhutDung)
        {
            soPhutDung = 0;

            DateTime mocNgay = new DateTime(2000, 1, 1);
            DateTime batDau = mocNgay.Add(thoiGianBatDau);
            DateTime ketThuc = mocNgay.Add(thoiGianKetThuc);

            if (ketThuc == batDau)
            {
                return false;
            }

            if (ketThuc < batDau)
            {
                ketThuc = ketThuc.AddDays(1);
            }

            double totalMinutes = (ketThuc - batDau).TotalMinutes;

            if (totalMinutes <= 0 || totalMinutes >= 24 * 60)
            {
                return false;
            }

            soPhutDung = Convert.ToInt32(totalMinutes);
            return soPhutDung > 0;
        }

        /// <summary>
        /// Tính thời gian dừng theo khung giờ sản xuất của từng ca.
        /// Ca 1: 06:00 - 18:00 cùng ngày.
        /// Ca 2: 18:00 - 06:00 sáng hôm sau.
        /// Ca 3 giữ cách tính cũ để không thay đổi nghiệp vụ chưa được định nghĩa.
        /// </summary>
        public static bool TryTinhThoiGianDungTheoCa(
            TimeSpan thoiGianBatDau,
            TimeSpan thoiGianKetThuc,
            int ca,
            out int soPhutDung,
            out string thongBao)
        {
            soPhutDung = 0;
            thongBao = string.Empty;

            if (ca == 1)
            {
                TimeSpan batDauCa = TimeSpan.FromHours(6);
                TimeSpan ketThucCa = TimeSpan.FromHours(18);

                if (thoiGianBatDau < batDauCa || thoiGianBatDau >= ketThucCa)
                {
                    thongBao =
                        "Thời gian bắt đầu phải từ 06:00 và trước 18:00.";
                    return false;
                }

                if (thoiGianKetThuc <= batDauCa || thoiGianKetThuc > ketThucCa)
                {
                    thongBao =
                        "Thời gian kết thúc phải sau 06:00 và không được vượt quá 18:00.";
                    return false;
                }

                if (thoiGianKetThuc <= thoiGianBatDau)
                {
                    thongBao =
                        "Thời gian kết thúc phải sau thời gian bắt đầu.";
                    return false;
                }

                soPhutDung = Convert.ToInt32(
                    (thoiGianKetThuc - thoiGianBatDau).TotalMinutes);
                return soPhutDung > 0;
            }

            if (ca == 2)
            {
                int batDauTheoCa;
                int ketThucTheoCa;

                if (!TryGetSoPhutTinhTuDauCa2(thoiGianBatDau, out batDauTheoCa))
                {
                    thongBao =
                        "Thời gian bắt đầu phải nằm trong khoảng từ 18:00 đến 06:00.";
                    return false;
                }

                if (!TryGetSoPhutTinhTuDauCa2(thoiGianKetThuc, out ketThucTheoCa))
                {
                    thongBao =
                        "Thời gian kết thúc phải nằm trong khoảng từ 18:00 đến 06:00.";
                    return false;
                }

                if (ketThucTheoCa <= batDauTheoCa)
                {
                    thongBao =
                        "Thời gian kết thúc phải sau thời gian bắt đầu.";
                    return false;
                }

                soPhutDung = ketThucTheoCa - batDauTheoCa;
                return soPhutDung > 0 && soPhutDung <= 12 * 60;
            }

            if (ca == 3)
            {
                if (!TryTinhThoiGianDung(
                    thoiGianBatDau,
                    thoiGianKetThuc,
                    out soPhutDung))
                {
                    thongBao =
                        "Thời gian kết thúc phải sau thời gian bắt đầu.";
                    return false;
                }

                return true;
            }

            thongBao = "Ca làm việc không hợp lệ.";
            return false;
        }

        /// <summary>
        /// Chuyển khoảng thời gian nhập thành DateTime thực tế trên trục thời gian của ca.
        /// Dùng chung cho kiểm tra overlap ở UI và DB.
        /// </summary>
        public static bool TryLayKhoangThoiGianTheoCa(
            DateTime ngay,
            TimeSpan thoiGianBatDau,
            TimeSpan thoiGianKetThuc,
            int ca,
            out DateTime batDau,
            out DateTime ketThuc)
        {
            batDau = DateTime.MinValue;
            ketThuc = DateTime.MinValue;

            int soPhutDung;
            string thongBao;
            if (!TryTinhThoiGianDungTheoCa(
                thoiGianBatDau,
                thoiGianKetThuc,
                ca,
                out soPhutDung,
                out thongBao))
            {
                return false;
            }

            if (ca == 1)
            {
                DateTime dauCa = ngay.Date.AddHours(6);
                batDau = dauCa.AddMinutes(
                    (thoiGianBatDau - TimeSpan.FromHours(6)).TotalMinutes);
                ketThuc = batDau.AddMinutes(soPhutDung);
                return true;
            }

            if (ca == 2)
            {
                int batDauTheoCa;
                if (!TryGetSoPhutTinhTuDauCa2(
                    thoiGianBatDau,
                    out batDauTheoCa))
                {
                    return false;
                }

                DateTime dauCa = ngay.Date.AddHours(18);
                batDau = dauCa.AddMinutes(batDauTheoCa);
                ketThuc = batDau.AddMinutes(soPhutDung);
                return true;
            }

            // Ca 3 giữ trục thời gian theo cách cũ.
            batDau = ngay.Date.Add(thoiGianBatDau);
            ketThuc = ngay.Date.Add(thoiGianKetThuc);

            if (ketThuc < batDau)
            {
                ketThuc = ketThuc.AddDays(1);
            }

            return true;
        }

        private static bool TryGetSoPhutTinhTuDauCa2(
            TimeSpan thoiGian,
            out int soPhut)
        {
            soPhut = 0;

            if (thoiGian < TimeSpan.Zero ||
                thoiGian >= TimeSpan.FromHours(24))
            {
                return false;
            }

            TimeSpan mocBatDauCa = TimeSpan.FromHours(18);
            TimeSpan mocKetThucCa = TimeSpan.FromHours(6);

            if (thoiGian >= mocBatDauCa)
            {
                soPhut = Convert.ToInt32(
                    (thoiGian - mocBatDauCa).TotalMinutes);
                return true;
            }

            if (thoiGian <= mocKetThucCa)
            {
                soPhut = 6 * 60 + Convert.ToInt32(thoiGian.TotalMinutes);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Đồng bộ toàn bộ trạng thái của một Ngày + Máy + Ca trong một transaction.
        /// ID đã tồn tại được UPDATE khi có thay đổi; dòng mới được INSERT;
        /// ID cũ không còn trong danh sách được DELETE.
        /// </summary>
        public static void LuuDanhSach(List<DanhSachLoiDungMay_Model> danhSach)
        {
            if (danhSach == null || danhSach.Count == 0)
            {
                throw new InvalidOperationException("Không có dữ liệu lỗi dừng máy để lưu.");
            }

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteTransaction transaction = conn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    ValidateDanhSachTruocKhiLuu(conn, transaction, danhSach);

                    DanhSachLoiDungMay_Model first = danhSach[0];
                    List<DanhSachLoiDungMay_Model> daLuu =
                        GetDanhSachDaLuuTheoMayNgayCa(
                            conn,
                            transaction,
                            first.Ngay,
                            first.DanhSachMayId,
                            first.Ca);

                    var daLuuTheoId = new Dictionary<int, DanhSachLoiDungMay_Model>();
                    foreach (DanhSachLoiDungMay_Model oldItem in daLuu)
                    {
                        daLuuTheoId[oldItem.Id] = oldItem;
                    }

                    var idCuConTrenGrid = new HashSet<int>();

                    foreach (DanhSachLoiDungMay_Model item in danhSach)
                    {
                        if (item.Id > 0 && daLuuTheoId.ContainsKey(item.Id))
                        {
                            idCuConTrenGrid.Add(item.Id);
                        }
                    }

                    // Xoá các bản ghi người dùng đã loại khỏi grid trước. Việc này
                    // giúp UPDATE/INSERT sau đó ít có nguy cơ đụng ràng buộc UNIQUE.
                    foreach (DanhSachLoiDungMay_Model oldItem in daLuu)
                    {
                        if (!idCuConTrenGrid.Contains(oldItem.Id))
                        {
                            DeleteBanGhiTheoId(conn, transaction, oldItem.Id);
                        }
                    }

                    foreach (DanhSachLoiDungMay_Model item in danhSach)
                    {
                        DanhSachLoiDungMay_Model oldItem;

                        if (item.Id > 0 && daLuuTheoId.TryGetValue(item.Id, out oldItem))
                        {
                            // Không UPDATE nếu người dùng không thay đổi bản ghi.
                            if (!BanGhiGiongNhau(item, oldItem))
                            {
                                UpdateBanGhi(conn, transaction, item);
                            }
                        }
                        else
                        {
                            // Last-write-wins: nếu ID cũ đã bị nơi khác xoá trước khi
                            // người dùng bấm Lưu, trạng thái trên grid vẫn được phục hồi
                            // bằng một INSERT mới.
                            item.Id = InsertBanGhi(conn, transaction, item);
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Giữ nguyên exception gốc.
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Xoá ngay toàn bộ dữ liệu của Ngày + Máy + Ca.
        /// Dùng khi người dùng xác nhận xoá dòng cuối cùng trong Edit mode.
        /// </summary>
        public static void XoaToanBoTheoMayNgayCa(
            DateTime ngay,
            int danhSachMayId,
            int ca)
        {
            const string sql = @"
                DELETE FROM DanhSachLoiDungMay
                WHERE Ngay = @Ngay
                  AND DanhSachMay_ID = @DanhSachMay_ID
                  AND Ca = @Ca;";

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteTransaction transaction = conn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Ngay",
                            ngay.Date.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@DanhSachMay_ID", danhSachMayId);
                        cmd.Parameters.AddWithValue("@Ca", ca);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Giữ nguyên exception gốc.
                    }

                    throw;
                }
            }
        }

        private static void ValidateDanhSachTruocKhiLuu(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            List<DanhSachLoiDungMay_Model> danhSach)
        {
            DanhSachLoiDungMay_Model first = danhSach[0];

            if (first.DanhSachMayId <= 0)
            {
                throw new InvalidOperationException("Máy không hợp lệ.");
            }

            if (first.Ca < 1 || first.Ca > 3)
            {
                throw new InvalidOperationException("Ca làm việc không hợp lệ.");
            }

            if (first.MaCongDoan < 0)
            {
                throw new InvalidOperationException("Công đoạn không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(first.NguoiLam))
            {
                throw new InvalidOperationException("Người làm không được để trống.");
            }

            int maCongDoanCuaMay = GetMaCongDoanCuaMay(conn, transaction, first.DanhSachMayId);
            if (maCongDoanCuaMay != first.MaCongDoan)
            {
                throw new InvalidOperationException("Máy không thuộc công đoạn đã chọn.");
            }

            var idDaXuatHien = new HashSet<int>();

            for (int i = 0; i < danhSach.Count; i++)
            {
                DanhSachLoiDungMay_Model item = danhSach[i];

                if (item.Id < 0)
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: ID bản ghi không hợp lệ.", i + 1));
                }

                if (item.Id > 0 && !idDaXuatHien.Add(item.Id))
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: ID bản ghi bị trùng trong danh sách.", i + 1));
                }

                if (item.Ngay.Date != first.Ngay.Date ||
                    item.DanhSachMayId != first.DanhSachMayId ||
                    item.Ca != first.Ca ||
                    item.MaCongDoan != first.MaCongDoan)
                {
                    throw new InvalidOperationException(
                        "Các dòng trong một lần lưu phải cùng Ngày, Máy, Ca và Công đoạn.");
                }

                if (string.IsNullOrWhiteSpace(item.NguoiLam))
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: Người làm không được để trống.", i + 1));
                }

                int soPhutDung;
                string thongBaoThoiGian;
                if (!TryTinhThoiGianDungTheoCa(
                    item.ThoiGianBatDau,
                    item.ThoiGianKetThuc,
                    item.Ca,
                    out soPhutDung,
                    out thongBaoThoiGian))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Dòng {0}: {1}",
                            i + 1,
                            thongBaoThoiGian));
                }

                // Không tin giá trị duration từ UI; luôn tính lại trước INSERT/UPDATE.
                item.ThoiGianDung = soPhutDung;

                TenLoiDungMay_Model tenLoi = GetTenLoiTheoId(conn, transaction, item.TenLoiDungMayId);
                if (tenLoi.MaCongDoan != first.MaCongDoan)
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: Lỗi dừng máy không thuộc công đoạn đã chọn.", i + 1));
                }

                item.TenLoi = tenLoi.TenLoi;

                if (IsLamViecKhac(tenLoi.TenLoi) && string.IsNullOrWhiteSpace(item.GhiChu))
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: Khi chọn 'Làm việc khác' bắt buộc phải nhập ghi chú.", i + 1));
                }
            }

            // Grid đại diện cho trạng thái cuối cùng, nên chỉ cần kiểm tra overlap
            // giữa các dòng trong chính danh sách hiện tại.
            for (int i = 0; i < danhSach.Count; i++)
            {
                for (int j = i + 1; j < danhSach.Count; j++)
                {
                    if (HaiKhoangThoiGianChongLan(danhSach[i], danhSach[j]))
                    {
                        throw new InvalidOperationException(
                            string.Format("Dòng {0} và dòng {1} có thời gian dừng máy chồng lấn nhau.", i + 1, j + 1));
                    }
                }
            }
        }

        private static int InsertBanGhi(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            DanhSachLoiDungMay_Model item)
        {
            const string sql = @"
                INSERT INTO DanhSachLoiDungMay
                (
                    TenLoiDungMay_ID,
                    Ngay,
                    DanhSachMay_ID,
                    NguoiLam,
                    ThoiGianBatDau,
                    ThoiGianKetThuc,
                    ThoiGianDung,
                    GhiChu,
                    Ca,
                    DanhSachCongDoan_MaCongDoan
                )
                VALUES
                (
                    @TenLoiDungMay_ID,
                    @Ngay,
                    @DanhSachMay_ID,
                    @NguoiLam,
                    @ThoiGianBatDau,
                    @ThoiGianKetThuc,
                    @ThoiGianDung,
                    @GhiChu,
                    @Ca,
                    @MaCongDoan
                );";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                GanThamSoBanGhi(cmd, item);
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand idCmd =
                new SQLiteCommand("SELECT last_insert_rowid();", conn, transaction))
            {
                object result = idCmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        private static void UpdateBanGhi(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            DanhSachLoiDungMay_Model item)
        {
            const string sql = @"
                UPDATE DanhSachLoiDungMay
                SET TenLoiDungMay_ID = @TenLoiDungMay_ID,
                    Ngay = @Ngay,
                    DanhSachMay_ID = @DanhSachMay_ID,
                    NguoiLam = @NguoiLam,
                    ThoiGianBatDau = @ThoiGianBatDau,
                    ThoiGianKetThuc = @ThoiGianKetThuc,
                    ThoiGianDung = @ThoiGianDung,
                    GhiChu = @GhiChu,
                    Ca = @Ca,
                    DanhSachCongDoan_MaCongDoan = @MaCongDoan
                WHERE id = @Id;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                GanThamSoBanGhi(cmd, item);
                cmd.Parameters.AddWithValue("@Id", item.Id);

                if (cmd.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException(
                        string.Format("Không thể cập nhật bản ghi id={0}.", item.Id));
                }
            }
        }

        private static void DeleteBanGhiTheoId(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            int id)
        {
            const string sql = "DELETE FROM DanhSachLoiDungMay WHERE id = @Id;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void GanThamSoBanGhi(
            SQLiteCommand cmd,
            DanhSachLoiDungMay_Model item)
        {
            cmd.Parameters.AddWithValue("@TenLoiDungMay_ID", item.TenLoiDungMayId);
            cmd.Parameters.AddWithValue(
                "@Ngay",
                item.Ngay.Date.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@DanhSachMay_ID", item.DanhSachMayId);
            cmd.Parameters.AddWithValue("@NguoiLam", item.NguoiLam.Trim());
            cmd.Parameters.AddWithValue("@ThoiGianBatDau", FormatTime(item.ThoiGianBatDau));
            cmd.Parameters.AddWithValue("@ThoiGianKetThuc", FormatTime(item.ThoiGianKetThuc));
            cmd.Parameters.AddWithValue("@ThoiGianDung", item.ThoiGianDung);
            cmd.Parameters.AddWithValue(
                "@GhiChu",
                string.IsNullOrWhiteSpace(item.GhiChu)
                    ? (object)DBNull.Value
                    : item.GhiChu.Trim());
            cmd.Parameters.AddWithValue("@Ca", item.Ca);
            cmd.Parameters.AddWithValue("@MaCongDoan", item.MaCongDoan);
        }

        private static bool BanGhiGiongNhau(
            DanhSachLoiDungMay_Model current,
            DanhSachLoiDungMay_Model oldItem)
        {
            return current.TenLoiDungMayId == oldItem.TenLoiDungMayId
                && current.Ngay.Date == oldItem.Ngay.Date
                && current.DanhSachMayId == oldItem.DanhSachMayId
                && string.Equals(
                    (current.NguoiLam ?? string.Empty).Trim(),
                    (oldItem.NguoiLam ?? string.Empty).Trim(),
                    StringComparison.Ordinal)
                && current.ThoiGianBatDau == oldItem.ThoiGianBatDau
                && current.ThoiGianKetThuc == oldItem.ThoiGianKetThuc
                && current.ThoiGianDung == oldItem.ThoiGianDung
                && string.Equals(
                    (current.GhiChu ?? string.Empty).Trim(),
                    (oldItem.GhiChu ?? string.Empty).Trim(),
                    StringComparison.Ordinal)
                && current.Ca == oldItem.Ca
                && current.MaCongDoan == oldItem.MaCongDoan;
        }

        private static List<DanhSachLoiDungMay_Model> GetDanhSachDaLuuTheoMayNgayCa(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            DateTime ngay,
            int danhSachMayId,
            int ca)
        {
            const string sql = @"
                SELECT
                    id,
                    TenLoiDungMay_ID,
                    Ngay,
                    DanhSachMay_ID,
                    NguoiLam,
                    ThoiGianBatDau,
                    ThoiGianKetThuc,
                    ThoiGianDung,
                    GhiChu,
                    Ca,
                    DanhSachCongDoan_MaCongDoan
                FROM DanhSachLoiDungMay
                WHERE Ngay = @Ngay
                  AND DanhSachMay_ID = @DanhSachMay_ID
                  AND Ca = @Ca
                ORDER BY id ASC;";

            var result = new List<DanhSachLoiDungMay_Model>();

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Ngay", ngay.Date.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@DanhSachMay_ID", danhSachMayId);
                cmd.Parameters.AddWithValue("@Ca", ca);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TimeSpan batDau = ParseTime(reader["ThoiGianBatDau"], "ThoiGianBatDau");
                        TimeSpan ketThuc = ParseTime(reader["ThoiGianKetThuc"], "ThoiGianKetThuc");
                        int duration;

                        string thongBaoThoiGian;
                        if (!TryTinhThoiGianDungTheoCa(
                            batDau,
                            ketThuc,
                            ca,
                            out duration,
                            out thongBaoThoiGian))
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "Bản ghi DanhSachLoiDungMay id={0} có thời gian không hợp lệ: {1}",
                                    reader["id"],
                                    thongBaoThoiGian));
                        }

                        result.Add(new DanhSachLoiDungMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            TenLoiDungMayId = Convert.ToInt32(reader["TenLoiDungMay_ID"]),
                            Ngay = ParseDate(reader["Ngay"]),
                            DanhSachMayId = Convert.ToInt32(reader["DanhSachMay_ID"]),
                            NguoiLam = reader["NguoiLam"] == DBNull.Value
                                ? string.Empty
                                : Convert.ToString(reader["NguoiLam"]) ?? string.Empty,
                            ThoiGianBatDau = batDau,
                            ThoiGianKetThuc = ketThuc,
                            ThoiGianDung = duration,
                            GhiChu = reader["GhiChu"] == DBNull.Value
                                ? string.Empty
                                : Convert.ToString(reader["GhiChu"]) ?? string.Empty,
                            Ca = Convert.ToInt32(reader["Ca"]),
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"])
                        });
                    }
                }
            }

            return result;
        }

        private static int GetMaCongDoanCuaMay(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            int danhSachMayId)
        {
            const string sql = @"
                SELECT DanhSachCongDoan_MaCongDoan
                FROM DanhSachMay
                WHERE id = @Id
                LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", danhSachMayId);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException("Không tìm thấy máy đã chọn trong database.");
                }

                return Convert.ToInt32(result);
            }
        }

        private static TenLoiDungMay_Model GetTenLoiTheoId(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            int tenLoiId)
        {
            const string sql = @"
                SELECT
                    id,
                    TenLoi,
                    MoTaLoi,
                    DanhSachCongDoan_MaCongDoan
                FROM TenLoiDungMay
                WHERE id = @Id
                LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", tenLoiId);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("Không tìm thấy lỗi dừng máy đã chọn trong database.");
                    }

                    return new TenLoiDungMay_Model
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        TenLoi = Convert.ToString(reader["TenLoi"]) ?? string.Empty,
                        MoTaLoi = reader["MoTaLoi"] == DBNull.Value
                            ? string.Empty
                            : Convert.ToString(reader["MoTaLoi"]) ?? string.Empty,
                        MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"])
                    };
                }
            }
        }

        private static bool IsLamViecKhac(string tenLoi)
        {
            return string.Equals(
                (tenLoi ?? string.Empty).Trim(),
                TenLoiLamViecKhac,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool LaBanGhiTrung(
            DanhSachLoiDungMay_Model a,
            DanhSachLoiDungMay_Model b)
        {
            return a.Ngay.Date == b.Ngay.Date
                && a.DanhSachMayId == b.DanhSachMayId
                && a.Ca == b.Ca
                && a.TenLoiDungMayId == b.TenLoiDungMayId
                && a.ThoiGianBatDau == b.ThoiGianBatDau
                && a.ThoiGianKetThuc == b.ThoiGianKetThuc;
        }

        private static bool HaiKhoangThoiGianChongLan(
            DanhSachLoiDungMay_Model a,
            DanhSachLoiDungMay_Model b)
        {
            DateTime aStart;
            DateTime aEnd;
            DateTime bStart;
            DateTime bEnd;

            if (!TryLayKhoangThoiGianTheoCa(
                    a.Ngay,
                    a.ThoiGianBatDau,
                    a.ThoiGianKetThuc,
                    a.Ca,
                    out aStart,
                    out aEnd) ||
                !TryLayKhoangThoiGianTheoCa(
                    b.Ngay,
                    b.ThoiGianBatDau,
                    b.ThoiGianKetThuc,
                    b.Ca,
                    out bStart,
                    out bEnd))
            {
                return false;
            }

            return aStart < bEnd && bStart < aEnd;
        }

        private static void GetKhoangThoiGian(
            DateTime ngay,
            TimeSpan batDau,
            TimeSpan ketThuc,
            out DateTime start,
            out DateTime end)
        {
            start = ngay.Date.Add(batDau);
            end = ngay.Date.Add(ketThuc);

            if (end < start)
            {
                end = end.AddDays(1);
            }
        }

        private static string FormatTime(TimeSpan value)
        {
            return value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        private static TimeSpan ParseTime(object value, string columnName)
        {
            string text = value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            TimeSpan result;
            if (!TimeSpan.TryParseExact(
                text,
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                TimeSpanStyles.None,
                out result))
            {
                throw new InvalidOperationException(
                    string.Format("Dữ liệu cột {0} trong DanhSachLoiDungMay không đúng định dạng HH:mm.", columnName));
            }

            return result;
        }

        private static DateTime ParseDate(object value)
        {
            string text = value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            DateTime result;
            if (!DateTime.TryParseExact(
                text,
                DinhDangNgay,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
            {
                throw new InvalidOperationException(
                    "Dữ liệu cột Ngay trong DanhSachLoiDungMay không đúng định dạng yyyy-MM-dd.");
            }

            return result;
        }
    }
}
