// ============================================================
// DB_KiemKe.cs
// Bảng liên quan: TTKiemKeThang, TTThanhPham, DanhSachBin, DanhSachMaSP, DanhSachNCC
// Chức năng: Kiểm kê tháng, upsert thành phẩm theo MaBin, tìm kiếm NCC
// ============================================================

using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DB_KiemKe
    {
        // ── TTKiemKeThang ────────────────────────────────────────────────────

        public static DataTable Load_TTKiemKeThang(string namThang = null, string nguoiKK = "Người 1")
        {
            if (string.IsNullOrWhiteSpace(namThang))
                namThang = DateTime.Now.ToString("yyyy-MM");

            const string sql = @"
                SELECT 
                    tp.DanhSachSP_ID,
                    kk.id,
                    kk.MaBin,
                    sp.Ten,
                    kk.ChieuDai,
                    kk.KhoiLuong,
                    kk.NguoiKK,
                    kk.GhiChu
                FROM TTKiemKeThang kk
                LEFT JOIN TTThanhPham tp ON tp.id = kk.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                WHERE kk.ThoiGianKiemKe = @namThang  AND kk.NguoiKK = @nguoiKK COLLATE NOCASE
                ORDER BY kk.DateInsert DESC, kk.id DESC;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@namThang", namThang);
            cmd.Parameters.AddWithValue("@nguoiKK", nguoiKK);

            var dt = new DataTable();
            using var da = new SQLiteDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public static bool Update_TTKiemKeThang(int id, decimal? chieuDai, decimal? khoiLuong, string ghiChu)
        {
            const string sql = @"
                UPDATE TTKiemKeThang
                SET 
                    ChieuDai  = @ChieuDai,
                    KhoiLuong = @KhoiLuong,
                    GhiChu    = @GhiChu
                WHERE id = @id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@ChieuDai", (object)chieuDai ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KhoiLuong", (object)khoiLuong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu.Trim());
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool Delete_TTKiemKeThang(int id)
        {
            const string sql = @"DELETE FROM TTKiemKeThang WHERE id = @id;";
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static long Insert_TTKiemKeThang(KiemKe model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(model.MaBin))
                throw new ArgumentException("MaBin không được để trống.", nameof(model.MaBin));

            string thoiGianKiemKe = string.IsNullOrWhiteSpace(model.ThoiGianKiemKe)
                ? DateTime.Now.ToString("yyyy-MM") : model.ThoiGianKiemKe.Trim();
            string dateInsert = string.IsNullOrWhiteSpace(model.DateInsert)
                ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : model.DateInsert.Trim();

            const string sql = @"
                INSERT INTO TTKiemKeThang
                (TTThanhPham_ID, MaBin, ChieuDai, KhoiLuong,
                 GhiChu, ThoiGianKiemKe, ApprovedDate, DateInsert, NguoiKK)
                VALUES
                (@TTThanhPham_ID, @MaBin, @ChieuDai, @KhoiLuong,
                 @GhiChu, @ThoiGianKiemKe, @ApprovedDate, @DateInsert, @NguoiKK);";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", (object)model.TTThanhPham_ID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaBin", model.MaBin.Trim());
                cmd.Parameters.AddWithValue("@ChieuDai", (object)model.ChieuDai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KhoiLuong", (object)model.KhoiLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());
                cmd.Parameters.AddWithValue("@ThoiGianKiemKe", thoiGianKiemKe);
                cmd.Parameters.AddWithValue("@ApprovedDate", string.IsNullOrWhiteSpace(model.ApprovedDate) ? (object)DBNull.Value : model.ApprovedDate.Trim());
                cmd.Parameters.AddWithValue("@DateInsert", dateInsert);
                cmd.Parameters.AddWithValue("@NguoiKK", model.NguoiKK);

                cmd.ExecuteNonQuery();
                long newId = conn.LastInsertRowId;
                tx.Commit();
                return newId;
            }
            catch { tx.Rollback(); throw; }
        }

        // ── TTThanhPham (từ Kiểm kê) ─────────────────────────────────────────

        public static long InsertTTThanhPham_FromKiemKe(KiemKe model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(model.MaBin))
                throw new ArgumentException("MaBin không được để trống.", nameof(model.MaBin));

            const string sql = @"
                INSERT INTO TTThanhPham
                (DanhSachSP_ID, MaBin, KhoiLuongTruoc, KhoiLuongSau,
                 ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu, DateInsert)
                VALUES
                (@DanhSachSP_ID, @MaBin, @KhoiLuongTruoc, @KhoiLuongSau,
                 @ChieuDaiTruoc, @ChieuDaiSau, 0, 0, @GhiChu, @DateInsert);
                SELECT last_insert_rowid();";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@DanhSachSP_ID", (object?)model.DanhSachSP_ID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaBin", model.MaBin.Trim());
                cmd.Parameters.AddWithValue("@KhoiLuongTruoc", (object?)model.KhoiLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KhoiLuongSau", (object?)model.KhoiLuong ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChieuDaiTruoc", (object?)model.ChieuDai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChieuDaiSau", (object?)model.ChieuDai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());
                cmd.Parameters.AddWithValue("@DateInsert", string.IsNullOrWhiteSpace(model.DateInsert)
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : model.DateInsert.Trim());

                long newId = Convert.ToInt64(cmd.ExecuteScalar());
                tx.Commit();
                return newId;
            }
            catch { tx.Rollback(); throw; }
        }

        public static int UpsertTTThanhPhamByMaBin(List<KiemKe> items)
        {
            if (items == null || items.Count == 0) return 0;

            const string sqlSelect = @"SELECT id FROM TTThanhPham WHERE MaBin = @MaBin LIMIT 1;";
            const string sqlInsert = @"
                INSERT INTO TTThanhPham (DanhSachSP_ID, MaBin, KhoiLuongTruoc, KhoiLuongSau,
                    ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu, DateInsert)
                VALUES (@DanhSachSP_ID, @MaBin, @KL, @KL, @CD, @CD, 0, 0, @GhiChu, @DateInsert);";
            const string sqlUpdate = @"
                UPDATE TTThanhPham
                SET DanhSachSP_ID  = @DanhSachSP_ID,
                    KhoiLuongTruoc = @KL,
                    KhoiLuongSau   = @KL,
                    ChieuDaiTruoc  = @CD,
                    ChieuDaiSau    = @CD,
                    GhiChu         = @GhiChu
                WHERE MaBin = @MaBin;";

            int count = 0;
            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var m in items)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.MaBin)) continue;
                    string maBin = m.MaBin.Trim();
                    string dateInsert = string.IsNullOrWhiteSpace(m.DateInsert)
                        ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : m.DateInsert.Trim();

                    long existingId = 0;
                    using (var sel = new SQLiteCommand(sqlSelect, conn, tx))
                    {
                        sel.Parameters.AddWithValue("@MaBin", maBin);
                        var r = sel.ExecuteScalar();
                        if (r != null && r != DBNull.Value) existingId = Convert.ToInt64(r);
                    }

                    string sql = existingId == 0 ? sqlInsert : sqlUpdate;
                    using var cmd = new SQLiteCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("@DanhSachSP_ID", (object?)m.DanhSachSP_ID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaBin", maBin);
                    cmd.Parameters.AddWithValue("@KL", (object?)m.KhoiLuong ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD", (object?)m.ChieuDai ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(m.GhiChu) ? (object)DBNull.Value : m.GhiChu.Trim());
                    cmd.Parameters.AddWithValue("@DateInsert", dateInsert);
                    cmd.ExecuteNonQuery();
                    count++;
                }
                tx.Commit();
                return count;
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Báo cáo kiểm kê thực tế ──────────────────────────────────────────

        /// <summary>
        /// Bảng: ThongTinDatHang, DanhSachDatHang, DanhSachMaSP, LichSuXuatNhap
        /// Tồn thực tế = SUM(LichSuXuatNhap.SoLuong), chỉ lấy > 0
        /// </summary>
        public static async Task<DataTable> LayBaoCaoKiemKeThucTeKho(string tuKhoa = "")
        {
            const string sql = @"
                SELECT
                    MIN(t.id)                            AS id,
                    t.TenVatTu                           AS TenVatTu,
                    IFNULL(sp.Ma, '')                    AS MaVatTu,
                    IFNULL(sp.Ten, '')                   AS TenDanhMuc,
                    IFNULL(sp.DonVi, '')                 AS DonVi,
                    SUM(IFNULL(t.SoLuongMua, 0))         AS TongSoLuongMua,
                    SUM(IFNULL(ls.TonKho, 0))            AS TonThucTe
                FROM ThongTinDatHang t
                INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN (
                    SELECT ThongTinDatHang_ID, SUM(SoLuong) AS TonKho
                    FROM LichSuXuatNhap GROUP BY ThongTinDatHang_ID
                ) ls ON ls.ThongTinDatHang_ID = t.id
                WHERE t.CanEdit = 1
                  AND (@tuKhoa IS NULL OR TRIM(@tuKhoa) = ''
                       OR t.TenVatTu LIKE '%' || @tuKhoa || '%'
                       OR sp.Ma LIKE '%' || @tuKhoa || '%'
                       OR sp.Ten LIKE '%' || @tuKhoa || '%')
                GROUP BY t.TenVatTu, sp.Ma, sp.Ten, sp.DonVi
                HAVING SUM(IFNULL(ls.TonKho, 0)) > 0
                ORDER BY t.TenVatTu, sp.Ma;";

            var dt = new DataTable();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tuKhoa", tuKhoa ?? "");
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });
            return dt;
        }

        // ── DanhSachBin ──────────────────────────────────────────────────────

        /// <summary>Bảng: DanhSachBin</summary>
        public static async Task<Dictionary<string, decimal>> LayDanhSachBin_KhoiLuong()
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"SELECT TenBin, KhoiLuongBin FROM DanhSachBin";

            using var conn = await DB_Base.OpenConnectionAsync();
            using var cmd = new SQLiteCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                string ten = rd["TenBin"]?.ToString()?.Trim();
                decimal kl = rd["KhoiLuongBin"] != DBNull.Value ? Convert.ToDecimal(rd["KhoiLuongBin"]) : 0m;
                if (!string.IsNullOrWhiteSpace(ten)) result[ten] = kl;
            }
            return result;
        }

        // ── DanhSachNCC ──────────────────────────────────────────────────────

        /// <summary>Bảng: DanhSachNCC</summary>
        public static async Task<List<NhaCungCapItem>> TimKiemNhaCungCap(string keyword)
        {
            const string sql = @"
                SELECT TenNCC, id FROM DanhSachNCC
                WHERE TenNCC_KhongDau LIKE @kw COLLATE NOCASE
                   OR TenNCC LIKE @kw COLLATE NOCASE
                   OR Ma LIKE @kw COLLATE NOCASE
                ORDER BY TenNCC LIMIT 30";

            var result = new List<NhaCungCapItem>();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    result.Add(new NhaCungCapItem { Id = Convert.ToInt32(rd["id"]), Ten = rd["TenNCC"].ToString() });
            });
            return result;
        }

        // ── TTThanhPham - theo MaBin ─────────────────────────────────────────

        /// <summary>
        /// Bảng: TTThanhPham, DanhSachMaSP, ThongTinCaLamViec, CaiDatCDBoc, CD_BocMach, TTNVL
        /// </summary>
        public static async Task<DataRow> LayThongTinTheoMaBin(string maBin)
        {
            const string sql = @"
                SELECT
                    ttp.DanhSachSP_ID, ttp.id AS TTThanhPham_ID,
                    sp.Ten, sp.Ma,
                    ttp.ChieuDaiSau, ttp.KhoiLuongSau, ttp.GhiChu,
                    ca.Ngay AS NgaySX, ca.Ca AS CaSX, ca.NguoiLam AS TenCN,
                    bm.Mau, nvl.QC
                FROM TTThanhPham ttp
                JOIN DanhSachMaSP sp           ON sp.id  = ttp.DanhSachSP_ID
                LEFT JOIN ThongTinCaLamViec ca ON ca.TTThanhPham_id = ttp.id
                LEFT JOIN CaiDatCDBoc cdb      ON cdb.TTThanhPham_ID = ttp.id
                LEFT JOIN CD_BocMach bm        ON bm.CaiDatCDBoc_ID = cdb.id
                LEFT JOIN TTNVL nvl            ON nvl.TTThanhPham_ID = ttp.id
                WHERE ttp.MaBin = @maBin
                ORDER BY ttp.id DESC LIMIT 1;";

            var dt = new DataTable();
            using var conn = await DB_Base.OpenConnectionAsync();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@maBin", maBin?.Trim());
            await Task.Run(() => { using var da = new SQLiteDataAdapter(cmd); da.Fill(dt); });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // ── Hạ Bin ───────────────────────────────────────────────────────────

        /// <summary>Bảng: TTThanhPham</summary>
        public static void Update_KhoiLuongSau_ChieuDaiSau(string maBin, decimal khoiLuongSau, decimal chieuDaiSau, string ghiChu)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @khoiLuongSau,
                    ChieuDaiSau  = @chieuDaiSau,
                    GhiChu       = @GhiChu
                WHERE MaBin = @maBin;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@khoiLuongSau", khoiLuongSau);
            cmd.Parameters.AddWithValue("@chieuDaiSau", chieuDaiSau);
            cmd.Parameters.AddWithValue("@GhiChu", ghiChu);
            cmd.Parameters.AddWithValue("@maBin", maBin);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new Exception($"Không tìm thấy MaBin: {maBin}");
        }
    }
}