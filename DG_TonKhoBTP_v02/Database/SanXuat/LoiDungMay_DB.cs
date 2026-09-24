using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

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


        #region Lõi mới theo TTThanhPham_ID

        /// <summary>
        /// Lấy TTThanhPham.id theo MaBin sau khi luồng thêm mới/sao chép đã lưu thành công.
        /// TTThanhPham.MaBin được khai báo UNIQUE trong schema.
        /// </summary>
        public static long GetTTThanhPhamIdTheoMaBin(string maBin)
        {
            string key = (maBin ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Không xác định được MaBin để lấy TTThanhPham_ID.");

            const string sql = @"
                SELECT id
                FROM TTThanhPham
                WHERE MaBin = @MaBin
                LIMIT 1;";

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaBin", key);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException(
                        "Không tìm thấy TTThanhPham vừa lưu theo MaBin: " + key);

                long id = Convert.ToInt64(result);
                if (id <= 0)
                    throw new InvalidOperationException(
                        "TTThanhPham_ID vừa lưu không hợp lệ.");

                return id;
            }
        }

        public static bool DaCoDuLieuTheoTTThanhPhamId(long ttThanhPhamId)
        {
            if (ttThanhPhamId <= 0) return false;

            const string sql = @"
                SELECT EXISTS
                (
                    SELECT 1
                    FROM DanhSachLoiDungMay
                    WHERE TTThanhPham_ID = @TTThanhPham_ID
                    LIMIT 1
                );";

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
            }
        }

        public static List<DanhSachLoiDungMay_Model> GetDanhSachDaLuuTheoTTThanhPhamId(
            long ttThanhPhamId)
        {
            if (ttThanhPhamId <= 0)
                return new List<DanhSachLoiDungMay_Model>();

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            {
                return GetDanhSachTheoTTThanhPham(conn, null, ttThanhPhamId);
            }
        }

        public static void LuuDanhSachTheoTTThanhPham(
            long ttThanhPhamId,
            List<DanhSachLoiDungMay_Model> danhSach)
        {
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteTransaction transaction = conn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    DongBoDanhSachTheoTTThanhPham(
                        conn,
                        transaction,
                        ttThanhPhamId,
                        danhSach);
                    transaction.Commit();
                }
                catch
                {
                    try { transaction.Rollback(); } catch { }
                    throw;
                }
            }
        }

        public static void DongBoDanhSachTheoTTThanhPham(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            long ttThanhPhamId,
            List<DanhSachLoiDungMay_Model> danhSach)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (ttThanhPhamId <= 0)
                throw new InvalidOperationException("TTThanhPham_ID không hợp lệ.");

            List<DanhSachLoiDungMay_Model> current = danhSach
                ?? new List<DanhSachLoiDungMay_Model>();

            ValidateDanhSachTheoTTThanhPham(
                conn,
                transaction,
                ttThanhPhamId,
                current);

            List<DanhSachLoiDungMay_Model> daLuu =
                GetDanhSachTheoTTThanhPham(conn, transaction, ttThanhPhamId);

            var daLuuTheoId = daLuu.ToDictionary(x => x.Id);
            DongBoThoiGianCuKhiNhapThuCong(current, daLuuTheoId);

            var idCuConLai = new HashSet<int>(
                current
                    .Where(x => x.Id > 0 && daLuuTheoId.ContainsKey(x.Id))
                    .Select(x => x.Id));

            foreach (DanhSachLoiDungMay_Model oldItem in daLuu)
            {
                if (!idCuConLai.Contains(oldItem.Id))
                    DeleteBanGhiTheoId(conn, transaction, oldItem.Id);
            }

            foreach (DanhSachLoiDungMay_Model item in current)
            {
                item.TTThanhPhamId = ttThanhPhamId;

                DanhSachLoiDungMay_Model oldItem;
                if (item.Id > 0 && daLuuTheoId.TryGetValue(item.Id, out oldItem))
                {
                    if (!BanGhiGiongNhauTheoTTThanhPham(item, oldItem))
                        UpdateBanGhiTheoTTThanhPham(conn, transaction, item);
                }
                else
                {
                    item.Id = InsertBanGhiTheoTTThanhPham(conn, transaction, item);
                }
            }
        }

        private static void ValidateDanhSachTheoTTThanhPham(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            long ttThanhPhamId,
            List<DanhSachLoiDungMay_Model> danhSach)
        {
            if (danhSach == null || danhSach.Count == 0)
                return;

            DanhSachLoiDungMay_Model first = danhSach[0];

            if (first.DanhSachMayId <= 0)
                throw new InvalidOperationException("Máy không hợp lệ.");

            if (first.MaCongDoan < 0)
                throw new InvalidOperationException("Công đoạn không hợp lệ.");

            int maCongDoanCuaMay =
                GetMaCongDoanCuaMay(conn, transaction, first.DanhSachMayId);
            if (maCongDoanCuaMay != first.MaCongDoan)
                throw new InvalidOperationException("Máy không thuộc công đoạn đã chọn.");

            var idDaXuatHien = new HashSet<int>();

            for (int i = 0; i < danhSach.Count; i++)
            {
                DanhSachLoiDungMay_Model item = danhSach[i];
                if (item == null)
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: dữ liệu không hợp lệ.", i + 1));

                if (item.Id < 0)
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: ID bản ghi không hợp lệ.", i + 1));

                if (item.Id > 0 && !idDaXuatHien.Add(item.Id))
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: ID bản ghi bị trùng trong danh sách.", i + 1));

                if (item.DanhSachMayId != first.DanhSachMayId ||
                    item.MaCongDoan != first.MaCongDoan)
                {
                    throw new InvalidOperationException(
                        "Các dòng trong một lần lưu phải cùng Máy và Công đoạn.");
                }

                if (item.TTThanhPhamId.HasValue &&
                    item.TTThanhPhamId.Value > 0 &&
                    item.TTThanhPhamId.Value != ttThanhPhamId)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Dòng {0}: TTThanhPham_ID không khớp bản ghi đang lưu.",
                            i + 1));
                }

                if (item.ThoiGianDung <= 0)
                    throw new InvalidOperationException(
                        string.Format(
                            "Dòng {0}: Thời gian dừng phải là số phút nguyên lớn hơn 0.",
                            i + 1));

                TenLoiDungMay_Model tenLoi =
                    GetTenLoiTheoId(conn, transaction, item.TenLoiDungMayId);

                if (tenLoi.MaCongDoan != first.MaCongDoan)
                    throw new InvalidOperationException(
                        string.Format(
                            "Dòng {0}: Lỗi dừng máy không thuộc công đoạn đã chọn.",
                            i + 1));

                item.TenLoi = tenLoi.TenLoi;
                item.TTThanhPhamId = ttThanhPhamId;

                if (IsLamViecKhac(tenLoi.TenLoi) &&
                    string.IsNullOrWhiteSpace(item.GhiChu))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Dòng {0}: Khi chọn 'Làm việc khác' bắt buộc phải nhập ghi chú.",
                            i + 1));
                }
            }
        }

        private static List<DanhSachLoiDungMay_Model> GetDanhSachTheoTTThanhPham(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            long ttThanhPhamId)
        {
            const string sql = @"
                SELECT
                    id,
                    TenLoiDungMay_ID,
                    DanhSachMay_ID,
                    ThoiGianBatDau,
                    ThoiGianKetThuc,
                    ThoiGianDung,
                    GhiChu,
                    DanhSachCongDoan_MaCongDoan,
                    TTThanhPham_ID
                FROM DanhSachLoiDungMay
                WHERE TTThanhPham_ID = @TTThanhPham_ID
                ORDER BY id ASC;";

            var result = new List<DanhSachLoiDungMay_Model>();

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new DanhSachLoiDungMay_Model
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            TenLoiDungMayId = Convert.ToInt32(reader["TenLoiDungMay_ID"]),
                            DanhSachMayId = Convert.ToInt32(reader["DanhSachMay_ID"]),
                            ThoiGianBatDau = TryParseTimeOrNull(reader["ThoiGianBatDau"]),
                            ThoiGianKetThuc = TryParseTimeOrNull(reader["ThoiGianKetThuc"]),
                            ThoiGianDung = Convert.ToInt32(reader["ThoiGianDung"]),
                            GhiChu = reader["GhiChu"] == DBNull.Value
                                ? string.Empty
                                : Convert.ToString(reader["GhiChu"]) ?? string.Empty,
                            MaCongDoan = Convert.ToInt32(reader["DanhSachCongDoan_MaCongDoan"]),
                            TTThanhPhamId = Convert.ToInt64(reader["TTThanhPham_ID"])
                        });
                    }
                }
            }

            return result;
        }

        private static int InsertBanGhiTheoTTThanhPham(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            DanhSachLoiDungMay_Model item)
        {
            const string sql = @"
                INSERT INTO DanhSachLoiDungMay
                (
                    TenLoiDungMay_ID,
                    DanhSachMay_ID,
                    ThoiGianBatDau,
                    ThoiGianKetThuc,
                    ThoiGianDung,
                    GhiChu,
                    DanhSachCongDoan_MaCongDoan,
                    TTThanhPham_ID
                )
                VALUES
                (
                    @TenLoiDungMay_ID,
                    @DanhSachMay_ID,
                    @ThoiGianBatDau,
                    @ThoiGianKetThuc,
                    @ThoiGianDung,
                    @GhiChu,
                    @MaCongDoan,
                    @TTThanhPham_ID
                );";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                GanThamSoBanGhiTheoTTThanhPham(cmd, item);
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand idCmd =
                new SQLiteCommand("SELECT last_insert_rowid();", conn, transaction))
            {
                return Convert.ToInt32(idCmd.ExecuteScalar());
            }
        }

        private static void UpdateBanGhiTheoTTThanhPham(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            DanhSachLoiDungMay_Model item)
        {
            const string sql = @"
                UPDATE DanhSachLoiDungMay
                SET TenLoiDungMay_ID = @TenLoiDungMay_ID,
                    DanhSachMay_ID = @DanhSachMay_ID,
                    ThoiGianBatDau = @ThoiGianBatDau,
                    ThoiGianKetThuc = @ThoiGianKetThuc,
                    ThoiGianDung = @ThoiGianDung,
                    GhiChu = @GhiChu,
                    DanhSachCongDoan_MaCongDoan = @MaCongDoan,
                    TTThanhPham_ID = @TTThanhPham_ID
                WHERE id = @Id
                  AND TTThanhPham_ID = @TTThanhPham_ID;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                GanThamSoBanGhiTheoTTThanhPham(cmd, item);
                cmd.Parameters.AddWithValue("@Id", item.Id);

                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException(
                        string.Format("Không thể cập nhật bản ghi id={0}.", item.Id));
            }
        }

        private static void GanThamSoBanGhiTheoTTThanhPham(
            SQLiteCommand cmd,
            DanhSachLoiDungMay_Model item)
        {
            cmd.Parameters.AddWithValue("@TenLoiDungMay_ID", item.TenLoiDungMayId);
            cmd.Parameters.AddWithValue("@DanhSachMay_ID", item.DanhSachMayId);
            cmd.Parameters.AddWithValue(
                "@ThoiGianBatDau",
                item.ThoiGianBatDau.HasValue
                    ? (object)FormatTime(item.ThoiGianBatDau.Value)
                    : DBNull.Value);
            cmd.Parameters.AddWithValue(
                "@ThoiGianKetThuc",
                item.ThoiGianKetThuc.HasValue
                    ? (object)FormatTime(item.ThoiGianKetThuc.Value)
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@ThoiGianDung", item.ThoiGianDung);
            cmd.Parameters.AddWithValue(
                "@GhiChu",
                string.IsNullOrWhiteSpace(item.GhiChu)
                    ? (object)DBNull.Value
                    : item.GhiChu.Trim());
            cmd.Parameters.AddWithValue("@MaCongDoan", item.MaCongDoan);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", item.TTThanhPhamId.Value);
        }

        private static bool BanGhiGiongNhauTheoTTThanhPham(
            DanhSachLoiDungMay_Model current,
            DanhSachLoiDungMay_Model oldItem)
        {
            return current.TenLoiDungMayId == oldItem.TenLoiDungMayId
                && current.DanhSachMayId == oldItem.DanhSachMayId
                && current.ThoiGianBatDau == oldItem.ThoiGianBatDau
                && current.ThoiGianKetThuc == oldItem.ThoiGianKetThuc
                && current.ThoiGianDung == oldItem.ThoiGianDung
                && string.Equals(
                    (current.GhiChu ?? string.Empty).Trim(),
                    (oldItem.GhiChu ?? string.Empty).Trim(),
                    StringComparison.Ordinal)
                && current.MaCongDoan == oldItem.MaCongDoan
                && current.TTThanhPhamId == oldItem.TTThanhPhamId;
        }

        #endregion

        /// <summary>
        /// Kiểm tra tổ hợp Ngày + Máy + Ca đã có ít nhất một bản ghi hay chưa.
        /// </summary>

        private static void DongBoThoiGianCuKhiNhapThuCong(
            List<DanhSachLoiDungMay_Model> danhSach,
            Dictionary<int, DanhSachLoiDungMay_Model> daLuuTheoId)
        {
            if (danhSach == null || daLuuTheoId == null)
                return;

            foreach (DanhSachLoiDungMay_Model item in danhSach)
            {
                if (item == null || item.Id <= 0)
                    continue;

                DanhSachLoiDungMay_Model oldItem;
                if (!daLuuTheoId.TryGetValue(item.Id, out oldItem))
                    continue;

                // Khi người dùng chỉ sửa các trường khác và giữ nguyên số phút dừng,
                // bảo toàn hai mốc giờ cũ nếu draft không mang lại giá trị mới.
                if (item.ThoiGianDung == oldItem.ThoiGianDung)
                {
                    if (!item.ThoiGianBatDau.HasValue)
                        item.ThoiGianBatDau = oldItem.ThoiGianBatDau;

                    if (!item.ThoiGianKetThuc.HasValue)
                        item.ThoiGianKetThuc = oldItem.ThoiGianKetThuc;
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

        private static int GetMaCongDoanCuaMay(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            int danhSachMayId)
        {
            const string sql = @"
                SELECT DanhSachCongDoan_MaCongDoan
                FROM DanhSachMay
                WHERE id = @Id;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", danhSachMayId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("Không tìm thấy máy đã chọn.");

                return Convert.ToInt32(value);
            }
        }

        private static TenLoiDungMay_Model GetTenLoiTheoId(
            SQLiteConnection conn,
            SQLiteTransaction transaction,
            int tenLoiDungMayId)
        {
            const string sql = @"
                SELECT id, TenLoi, MoTaLoi, DanhSachCongDoan_MaCongDoan
                FROM TenLoiDungMay
                WHERE id = @Id;";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", tenLoiDungMayId);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new InvalidOperationException("Không tìm thấy lỗi dừng máy đã chọn.");

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

        private static string FormatTime(TimeSpan value)
        {
            return value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        private static TimeSpan? TryParseTimeOrNull(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is TimeSpan)
                return (TimeSpan)value;

            if (value is DateTime)
                return ((DateTime)value).TimeOfDay;

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            TimeSpan result;
            return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out result)
                ? (TimeSpan?)result
                : null;
        }
    }
}
