using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    /// <summary>
    /// Truy xuất báo cáo dừng máy độc lập: ThongTinDungMay và DSLoiDungMay.
    /// Các API cũ theo TTThanhPham_ID/DanhSachLoiDungMay đã được loại bỏ có chủ đích.
    /// </summary>
    internal static class LoiDungMay_DB
    {
        private const string DinhDangNgay = "yyyy-MM-dd";

        public static List<DanhSachCongDoan_Model> GetDanhSachCongDoanCoMay()
        {
            const string sql = @"
                SELECT cd.id, cd.MaCongDoan, cd.TenCongDoan
                FROM DanhSachCongDoan cd
                WHERE EXISTS (SELECT 1 FROM DanhSachMay m
                              WHERE m.DanhSachCongDoan_MaCongDoan = cd.MaCongDoan)
                ORDER BY cd.id ASC;";
            var ds = new List<DanhSachCongDoan_Model>();
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            using (SQLiteDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                    ds.Add(new DanhSachCongDoan_Model
                    {
                        Id = Convert.ToInt32(r["id"]),
                        MaCongDoan = Convert.ToInt32(r["MaCongDoan"]),
                        TenCongDoan = Convert.ToString(r["TenCongDoan"]) ?? string.Empty
                    });
            }
            return ds;
        }

        public static List<DanhSachMay_Model> GetDanhSachMayTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT id, DanhSachCongDoan_MaCongDoan, TenMay
                FROM DanhSachMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";
            var ds = new List<DanhSachMay_Model>();
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);
                using (SQLiteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        ds.Add(new DanhSachMay_Model
                        {
                            Id = Convert.ToInt32(r["id"]),
                            MaCongDoan = Convert.ToInt32(r["DanhSachCongDoan_MaCongDoan"]),
                            TenMay = Convert.ToString(r["TenMay"]) ?? string.Empty
                        });
            }
            return ds;
        }

        public static List<TenLoiDungMay_Model> GetDanhSachTenLoiTheoMaCongDoan(int maCongDoan)
        {
            const string sql = @"
                SELECT id, TenLoi, MoTaLoi, DanhSachCongDoan_MaCongDoan
                FROM TenLoiDungMay
                WHERE DanhSachCongDoan_MaCongDoan = @MaCongDoan
                ORDER BY id ASC;";
            var ds = new List<TenLoiDungMay_Model>();
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);
                using (SQLiteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        ds.Add(new TenLoiDungMay_Model
                        {
                            Id = Convert.ToInt32(r["id"]),
                            TenLoi = Convert.ToString(r["TenLoi"]) ?? string.Empty,
                            MoTaLoi = r["MoTaLoi"] == DBNull.Value ? string.Empty : Convert.ToString(r["MoTaLoi"]) ?? string.Empty,
                            MaCongDoan = Convert.ToInt32(r["DanhSachCongDoan_MaCongDoan"])
                        });
            }
            return ds;
        }

        /// <summary>Trả tối đa 2 bản ghi để nhận diện dữ liệu bị trùng khóa nghiệp vụ.</summary>
        public static List<ThongTinDungMay_Model> TimBaoCao(DateTime ngay, int ca, int congDoanId, int mayId)
        {
            using (SQLiteConnection conn = DB_Base.OpenConnection())
                return TimBaoCao(conn, null, ngay, ca, congDoanId, mayId);
        }

        private static List<ThongTinDungMay_Model> TimBaoCao(
            SQLiteConnection conn, SQLiteTransaction tx, DateTime ngay, int ca, int congDoanId, int mayId)
        {
            const string sql = @"
                SELECT id, DanhSachCongDoan_ID, Ngay, Ca, DanhSachMay, NguoiLam
                FROM ThongTinDungMay
                WHERE Ngay = @Ngay AND Ca = @Ca
                  AND DanhSachCongDoan_ID = @CongDoan AND DanhSachMay = @May
                ORDER BY id ASC LIMIT 2;";
            var result = new List<ThongTinDungMay_Model>();
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Ngay", ngay.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@Ca", ca);
                cmd.Parameters.AddWithValue("@CongDoan", congDoanId);
                cmd.Parameters.AddWithValue("@May", mayId);
                using (SQLiteDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        result.Add(new ThongTinDungMay_Model
                        {
                            Id = Convert.ToInt64(r["id"]),
                            DanhSachCongDoanId = Convert.ToInt32(r["DanhSachCongDoan_ID"]),
                            Ngay = DateTime.ParseExact(Convert.ToString(r["Ngay"]), DinhDangNgay, CultureInfo.InvariantCulture),
                            Ca = Convert.ToInt32(r["Ca"]),
                            DanhSachMayId = Convert.ToInt32(r["DanhSachMay"]),
                            NguoiLam = r["NguoiLam"] == DBNull.Value ? string.Empty : Convert.ToString(r["NguoiLam"]) ?? string.Empty
                        });
                }
            }
            return result;
        }

        public static List<DSLoiDungMay_Model> GetLoiTheoBaoCao(long baoCaoId)
        {
            const string sql = @"
                SELECT id, ThongTinDungMay_ID, ThoiGianDung, TenLoiDungMay_ID, GhiChu
                FROM DSLoiDungMay WHERE ThongTinDungMay_ID = @BaoCaoId ORDER BY id ASC;";
            var result = new List<DSLoiDungMay_Model>();
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BaoCaoId", baoCaoId);
                using (SQLiteDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        result.Add(new DSLoiDungMay_Model
                        {
                            Id = Convert.ToInt64(r["id"]),
                            ThongTinDungMayId = Convert.ToInt64(r["ThongTinDungMay_ID"]),
                            ThoiGianDung = r["ThoiGianDung"] == DBNull.Value ? 0 : Convert.ToInt32(r["ThoiGianDung"]),
                            TenLoiDungMayId = r["TenLoiDungMay_ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["TenLoiDungMay_ID"]),
                            GhiChu = r["GhiChu"] == DBNull.Value ? string.Empty : Convert.ToString(r["GhiChu"]) ?? string.Empty
                        });
            }
            return result;
        }

        /// <summary>
        /// Lưu cha và tất cả lỗi trong một transaction. Edit thay thế toàn bộ lỗi cũ.
        /// Id > 0: cập nhật bản ghi hiện hữu; Id = 0: tạo báo cáo mới.
        /// </summary>
        public static long LuuBaoCao(ThongTinDungMay_Model baoCao, IList<DSLoiDungMay_Model> loi)
        {
            if (baoCao == null) throw new ArgumentNullException(nameof(baoCao));
            if (loi == null || loi.Count == 0)
                throw new InvalidOperationException("Báo cáo phải có ít nhất một dòng lỗi.");
            if (baoCao.DanhSachCongDoanId <= 0 || baoCao.DanhSachMayId <= 0 || baoCao.Ca < 1 || baoCao.Ca > 3)
                throw new InvalidOperationException("Vui lòng chọn đầy đủ Ca, Công đoạn và Máy.");
            if (baoCao.Ngay.Date < DateTime.Today.AddMonths(-1).Date || baoCao.Ngay.Date > DateTime.Today)
                throw new InvalidOperationException("Ngày báo cáo phải trong khoảng một tháng trở lại tính đến hôm nay.");
            if (string.IsNullOrWhiteSpace(baoCao.NguoiLam))
                throw new InvalidOperationException("Vui lòng nhập Người làm.");

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int maCongDoan = KiemTraCongDoanMay(conn, tx, baoCao.DanhSachCongDoanId, baoCao.DanhSachMayId);
                    var daGap = new HashSet<int>();
                    foreach (DSLoiDungMay_Model item in loi)
                    {
                        if (item == null || item.TenLoiDungMayId <= 0 || item.ThoiGianDung <= 0)
                            throw new InvalidOperationException("Danh sách lỗi có dòng chưa hợp lệ.");
                        if (!daGap.Add(item.TenLoiDungMayId))
                            throw new InvalidOperationException("Loại lỗi dừng máy bị trùng. Vui lòng chỉnh sửa các dòng trùng.");
                        string tenLoi = KiemTraTenLoi(conn, tx, item.TenLoiDungMayId, maCongDoan);
                        if (string.Equals(tenLoi.Trim(), "Làm việc khác", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrWhiteSpace(item.GhiChu))
                            throw new InvalidOperationException("Lỗi 'Làm việc khác' phải có ghi chú.");
                    }

                    List<ThongTinDungMay_Model> matches = TimBaoCao(conn, tx, baoCao.Ngay, baoCao.Ca,
                        baoCao.DanhSachCongDoanId, baoCao.DanhSachMayId);
                    if (matches.Count > 1)
                        throw new InvalidOperationException("Có nhiều báo cáo trùng Ca, Ngày, Công đoạn và Máy. Vui lòng xử lý dữ liệu trùng trước.");
                    if (baoCao.Id <= 0 && matches.Count > 0)
                        throw new InvalidOperationException("Báo cáo đã tồn tại. Vui lòng tải lại trước khi cập nhật.");
                    if (baoCao.Id > 0 && (matches.Count != 1 || matches[0].Id != baoCao.Id))
                        throw new InvalidOperationException("Báo cáo đã bị thay đổi hoặc xóa. Vui lòng tải lại.");

                    long id = baoCao.Id;
                    if (id <= 0)
                    {
                        const string insert = @"
                            INSERT INTO ThongTinDungMay(DanhSachCongDoan_ID, Ngay, Ca, DanhSachMay, NguoiLam)
                            VALUES(@CongDoan, @Ngay, @Ca, @May, @NguoiLam);";
                        using (SQLiteCommand cmd = new SQLiteCommand(insert, conn, tx))
                        {
                            GanThamSoCha(cmd, baoCao);
                            cmd.ExecuteNonQuery();
                        }
                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, tx))
                            id = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    else
                    {
                        const string update = @"
                            UPDATE ThongTinDungMay SET NguoiLam=@NguoiLam
                            WHERE id=@Id AND DanhSachCongDoan_ID=@CongDoan
                              AND Ngay=@Ngay AND Ca=@Ca AND DanhSachMay=@May;";
                        using (SQLiteCommand cmd = new SQLiteCommand(update, conn, tx))
                        {
                            GanThamSoCha(cmd, baoCao);
                            cmd.Parameters.AddWithValue("@Id", id);
                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("Không thể cập nhật báo cáo đang chọn.");
                        }
                        using (SQLiteCommand cmd = new SQLiteCommand(
                            "DELETE FROM DSLoiDungMay WHERE ThongTinDungMay_ID = @Id;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    const string insertLoi = @"
                        INSERT INTO DSLoiDungMay(ThongTinDungMay_ID, ThoiGianDung, TenLoiDungMay_ID, GhiChu)
                        VALUES(@BaoCaoId, @Phut, @TenLoiId, @GhiChu);";
                    foreach (DSLoiDungMay_Model item in loi)
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(insertLoi, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@BaoCaoId", id);
                            cmd.Parameters.AddWithValue("@Phut", item.ThoiGianDung);
                            cmd.Parameters.AddWithValue("@TenLoiId", item.TenLoiDungMayId);
                            cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(item.GhiChu)
                                ? (object)DBNull.Value : item.GhiChu.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    return id;
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
        }

        public static void XoaBaoCao(long id)
        {
            if (id <= 0) throw new InvalidOperationException("Không tìm thấy báo cáo cần xóa.");
            using (SQLiteConnection conn = DB_Base.OpenConnection())
            using (SQLiteTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "DELETE FROM DSLoiDungMay WHERE ThongTinDungMay_ID = @Id;", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "DELETE FROM ThongTinDungMay WHERE id = @Id;", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Báo cáo không còn tồn tại. Vui lòng tải lại.");
                    }
                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
        }

        private static void GanThamSoCha(SQLiteCommand cmd, ThongTinDungMay_Model baoCao)
        {
            cmd.Parameters.AddWithValue("@CongDoan", baoCao.DanhSachCongDoanId);
            cmd.Parameters.AddWithValue("@Ngay", baoCao.Ngay.Date.ToString(DinhDangNgay, CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@Ca", baoCao.Ca);
            cmd.Parameters.AddWithValue("@May", baoCao.DanhSachMayId);
            cmd.Parameters.AddWithValue("@NguoiLam", baoCao.NguoiLam.Trim());
        }

        private static int KiemTraCongDoanMay(SQLiteConnection conn, SQLiteTransaction tx, int congDoanId, int mayId)
        {
            const string sql = @"
                SELECT cd.MaCongDoan FROM DanhSachCongDoan cd
                INNER JOIN DanhSachMay m ON m.DanhSachCongDoan_MaCongDoan=cd.MaCongDoan
                WHERE cd.id=@CongDoan AND m.id=@May LIMIT 1;";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@CongDoan", congDoanId);
                cmd.Parameters.AddWithValue("@May", mayId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("Máy không thuộc công đoạn đã chọn.");
                return Convert.ToInt32(value);
            }
        }

        private static string KiemTraTenLoi(SQLiteConnection conn, SQLiteTransaction tx, int tenLoiId, int maCongDoan)
        {
            const string sql = @"
                SELECT TenLoi FROM TenLoiDungMay
                WHERE id=@Id AND DanhSachCongDoan_MaCongDoan=@MaCongDoan LIMIT 1;";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", tenLoiId);
                cmd.Parameters.AddWithValue("@MaCongDoan", maCongDoan);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("Loại lỗi không còn hợp lệ. Vui lòng chọn lại loại lỗi hiển thị 'Lỗi'.");
                return Convert.ToString(value) ?? string.Empty;
            }
        }
    }
}
