// ============================================================
// DB_XuatNhapKho.cs
// Bảng liên quan: LichSuXuatNhap, DanhSachDatHang, ThongTinDatHang,
//                 DanhSachKho, DanhSachMaSP, DanhSachNCC
// Chức năng: Xuất nhập kho, báo cáo xuất nhập, tồn kho
// ============================================================

using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DB_XuatNhapKho
    {
        // ── Tồn kho ──────────────────────────────────────────────────────────

        /// <summary>
        /// Bảng: LichSuXuatNhap, ThongTinDatHang, DanhSachMaSP, DanhSachKho
        /// Tính tồn kho theo kỳ.
        /// </summary>
        public static DataTable TinhTonKho(DateTime? ngayBatDau, DateTime? ngayKetThuc, int? idKho = null)
        {
            bool coLocNgay = ngayBatDau.HasValue && ngayKetThuc.HasValue;
            string khoFilter = (idKho.HasValue && idKho.Value > 0) ? "AND lx.DanhSachKho_ID = @IdKho" : "";
            string tonDauKyNgayFilter = coLocNgay ? "WHERE lx.Ngay < @NgayBatDau" : "WHERE 1=0";
            string phatSinhNgayFilter = coLocNgay ? "AND lx.Ngay >= @NgayBatDau AND lx.Ngay <= @NgayKetThuc" : "";

            string sql = $@"
                WITH
                TonDauKy AS (
                    SELECT lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID, SUM(lx.SoLuong) AS TonDau
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    {tonDauKyNgayFilter}
                      AND lx.DanhSachKho_ID IS NOT NULL AND lx.ThongTinDatHang_ID IS NOT NULL
                      {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                PhatSinhKy AS (
                    SELECT lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID,
                        SUM(CASE WHEN lx.SoLuong > 0 THEN  lx.SoLuong ELSE 0 END) AS TongNhap,
                        SUM(CASE WHEN lx.SoLuong < 0 THEN -lx.SoLuong ELSE 0 END) AS TongXuat
                    FROM LichSuXuatNhap lx
                    INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lx.ThongTinDatHang_ID
                    WHERE lx.DanhSachKho_ID IS NOT NULL AND lx.ThongTinDatHang_ID IS NOT NULL
                      {phatSinhNgayFilter} {khoFilter}
                    GROUP BY lx.DanhSachKho_ID, ttdh.DanhSachMaSP_ID
                ),
                AllKeys AS (
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM TonDauKy
                    UNION
                    SELECT DanhSachKho_ID, DanhSachMaSP_ID FROM PhatSinhKy
                )
                SELECT
                    sp.Ten  AS TenVatTu,  sp.Ma AS MaVatTu,  sp.DonVi AS DonVi,
                    kho.TenKho AS TenKho,
                    COALESCE(tdk.TonDau,   0) AS TonDauKy,
                    COALESCE(psk.TongNhap, 0) AS TongNhap,
                    COALESCE(psk.TongXuat, 0) AS TongXuat,
                    COALESCE(tdk.TonDau, 0) + COALESCE(psk.TongNhap, 0) - COALESCE(psk.TongXuat, 0) AS TonCuoiKy
                FROM AllKeys ak
                INNER JOIN DanhSachMaSP sp  ON sp.id  = ak.DanhSachMaSP_ID
                INNER JOIN DanhSachKho  kho ON kho.id = ak.DanhSachKho_ID
                LEFT  JOIN TonDauKy    tdk  ON tdk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID AND tdk.DanhSachKho_ID = ak.DanhSachKho_ID
                LEFT  JOIN PhatSinhKy  psk  ON psk.DanhSachMaSP_ID = ak.DanhSachMaSP_ID AND psk.DanhSachKho_ID = ak.DanhSachKho_ID
                ORDER BY sp.Ma;";

            var dt = new DataTable();
            dt.Columns.Add("Tên Vật Tư", typeof(string));
            dt.Columns.Add("Mã Vật Tư", typeof(string));
            dt.Columns.Add("Đơn vị", typeof(string));
            dt.Columns.Add("Tên Kho", typeof(string));
            dt.Columns.Add("Tồn Đầu Kỳ", typeof(decimal));
            dt.Columns.Add("Tổng Nhập", typeof(decimal));
            dt.Columns.Add("Tổng Xuất", typeof(decimal));
            dt.Columns.Add("Tồn Cuối Kỳ", typeof(decimal));

            using var conn = DB_Base.OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (coLocNgay)
            {
                cmd.Parameters.AddWithValue("@NgayBatDau", ngayBatDau!.Value.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc!.Value.ToString("yyyy-MM-dd 23:59:59"));
            }
            if (idKho.HasValue && idKho.Value > 0)
                cmd.Parameters.AddWithValue("@IdKho", idKho.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dt.Rows.Add(
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                    reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                    reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                    reader.IsDBNull(7) ? 0m : reader.GetDecimal(7));
            }
            return dt;
        }

        // ── Load lịch sử xuất nhập ────────────────────────────────────────────

        /// <summary>Bảng: LichSuXuatNhap, DanhSachDatHang, DanhSachKho</summary>
        public static DataTable LoadLichSuXuatNhap_LoaiDon1(bool i, string maDon = null, long? khoId = null)
        {
            string sql = @"
                SELECT d.id AS DanhSachDatHang_ID, d.LoaiDon, k.TenKho,
                    sp.Ma AS MaHang, t.TenVatTu, sp.DonVi,
                    COALESCE(SUM(CASE WHEN l.SoLuong > 0 THEN l.SoLuong ELSE 0 END), 0) AS Nhap,
                    COALESCE(SUM(CASE WHEN l.SoLuong < 0 THEN ABS(l.SoLuong) ELSE 0 END), 0) AS Xuat,
                    COALESCE(SUM(l.SoLuong), 0) AS Ton
                FROM DanhSachDatHang d
                INNER JOIN ThongTinDatHang t ON t.DanhSachDatHang_ID = d.id
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN LichSuXuatNhap l ON l.ThongTinDatHang_ID = t.id
                LEFT JOIN DanhSachKho k ON k.id = l.DanhSachKho_ID
                WHERE d.LoaiDon = 1";

            if (!string.IsNullOrWhiteSpace(maDon)) sql += " AND d.MaDon = @MaDon ";
            if (khoId.HasValue) sql += " AND k.id = @KhoId ";

            sql += @"
                GROUP BY d.id, d.MaDon, d.LoaiDon, t.id, sp.Ma, t.TenVatTu, sp.DonVi, k.id, k.KiHieu, k.TenKho, t.SoLuongMua
                HAVING COALESCE(SUM(l.SoLuong), 0) != 0
                ORDER BY d.DateInsert DESC, d.MaDon, t.id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(maDon)) cmd.Parameters.AddWithValue("@MaDon", maDon.Trim());
            if (khoId.HasValue) cmd.Parameters.AddWithValue("@KhoId", khoId.Value);
            using var da = new SQLiteDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        // ── Xóa / Update ────────────────────────────────────────────────────

        /// <summary>Bảng: DanhSachDatHang (cascade FK sang ThongTinDatHang, LichSuXuatNhap)</summary>
        public static void DeleteDanhSachDatHang(int id)
        {
            const string sql = "DELETE FROM DanhSachDatHang WHERE id = @Id;";
            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0) throw new Exception($"Không tìm thấy DanhSachDatHang ID = {id} để xóa.");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>Bảng: LichSuXuatNhap</summary>
        public static void DeleteLichSuXuatNhap(int id)
        {
            const string sql = "DELETE FROM LichSuXuatNhap WHERE id = @Id;";
            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0) throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {id} để xóa.");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateLichSuXuatNhap(LichSuXuatNhapUpdateModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            const string sql = @"
                UPDATE LichSuXuatNhap
                SET SoLuong = @SoLuong, NguoiGiao_Nhan = @NguoiGiaoNhan, Kho = @Kho,
                    LyDo = @LyDo, Ngay = @Ngay, TenPhieu = @TenPhieu, GhiChu = @GhiChu
                WHERE id = @Id;";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@SoLuong", model.SoLuong);
                cmd.Parameters.AddWithValue("@NguoiGiaoNhan", string.IsNullOrWhiteSpace(model.NguoiGiaoNhan) ? (object)DBNull.Value : model.NguoiGiaoNhan.Trim());
                cmd.Parameters.AddWithValue("@Kho", string.IsNullOrWhiteSpace(model.Kho) ? (object)DBNull.Value : model.Kho.Trim());
                cmd.Parameters.AddWithValue("@LyDo", string.IsNullOrWhiteSpace(model.LyDo) ? (object)DBNull.Value : model.LyDo.Trim());
                cmd.Parameters.AddWithValue("@Ngay", string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay.Trim());
                cmd.Parameters.AddWithValue("@TenPhieu", string.IsNullOrWhiteSpace(model.TenPhieu) ? (object)DBNull.Value : model.TenPhieu.Trim());
                cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu.Trim());
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0) throw new Exception($"Không tìm thấy LichSuXuatNhap ID = {model.Id}.");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateCanEdit(DataGridView dgr, string clCheck)
        {
            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                const string sql = "UPDATE LichSuXuatNhap SET CanEdit = @CanEdit WHERE id = @Id";
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.Add("@CanEdit", DbType.Int32);
                cmd.Parameters.Add("@Id", DbType.Int32);

                foreach (DataGridViewRow row in dgr.Rows)
                {
                    if (!dgr.Columns.Contains("colChon")) break;
                    var idVal = row.Cells["lsxn_id"].Value;
                    if (idVal == null || idVal == DBNull.Value) continue;
                    bool isChecked = row.Cells[clCheck].Value != null && Convert.ToBoolean(row.Cells[clCheck].Value);
                    cmd.Parameters["@CanEdit"].Value = isChecked ? 0 : 1;
                    cmd.Parameters["@Id"].Value = Convert.ToInt32(idVal);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch (Exception ex) { tx.Rollback(); throw new Exception($"UpdateCanEdit lỗi: {ex.Message}", ex); }
        }

        // ── Tìm kiếm mã đơn ─────────────────────────────────────────────────

        public static async Task<List<string>> TimKiemMaDon(string keyword, bool isEdit)
        {
            string sql = isEdit
                ? @"SELECT DISTINCT l.TenPhieu as MaDon FROM LichSuXuatNhap as l
                    WHERE l.canedit = 1 AND l.TenPhieu LIKE @kw COLLATE NOCASE LIMIT 30"
                : @"SELECT MaDon FROM DanhSachDatHang WHERE MaDon LIKE @kw ORDER BY DateInsert DESC LIMIT 30";

            var result = new List<string>();
            using var conn = await DB_Base.OpenConnectionAsync();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) result.Add(rd["MaDon"]?.ToString());
            return result;
        }

        public static async Task<List<string>> TimKiemMaDonConHang(string keyword, bool isEdit)
        {
            string sql = isEdit
                ? @"SELECT DISTINCT l.TenPhieu as MaDon FROM LichSuXuatNhap as l
                    WHERE l.canedit = 1 AND l.TenPhieu LIKE @kw COLLATE NOCASE LIMIT 30"
                : @"SELECT DISTINCT d.MaDon FROM DanhSachDatHang d
                    INNER JOIN ThongTinDatHang t ON t.DanhSachDatHang_ID = d.id
                    INNER JOIN LichSuXuatNhap l ON l.ThongTinDatHang_ID = t.id
                    WHERE d.MaDon LIKE @kw GROUP BY t.id HAVING SUM(l.SoLuong) > 0
                    ORDER BY d.DateInsert DESC LIMIT 30";

            var result = new List<string>();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) result.Add(rd["MaDon"].ToString());
            });
            return result;
        }

        public static async Task<List<string>> TimKiemTheoTenVatTuConHang(string keyword)
        {
            string keyWord_KhongDau = Helper.Helper.BoDauTiengViet(keyword.Trim());
            const string sql = @"
                SELECT CASE WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu ELSE d.Ten END AS TenVatTu
                FROM ThongTinDatHang t LEFT JOIN DanhSachMaSP d ON d.id = t.DanhSachMaSP_ID
                INNER JOIN LichSuXuatNhap l ON l.ThongTinDatHang_ID = t.id
                WHERE (t.DanhSachMaSP_ID IS NULL AND t.TenVatTu_KhongDau LIKE @kw COLLATE NOCASE AND t.TenVatTu IS NOT NULL)
                   OR (t.DanhSachMaSP_ID IS NOT NULL AND d.Ten_KhongDau LIKE @kw COLLATE NOCASE AND d.Ten IS NOT NULL)
                GROUP BY CASE WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu ELSE d.Ten END
                HAVING SUM(l.SoLuong) > 0 ORDER BY TenVatTu LIMIT 50;";

            var result = new List<string>();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyWord_KhongDau}%");
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) result.Add(rd["TenVatTu"].ToString());
            });
            return result;
        }

        // ── Chi tiết đơn đặt hàng ────────────────────────────────────────────

        public static async Task<DataTable> LayChiTietDonDatHang(string maDon, bool isEdit, bool isNhap = true)
        {
            string sql = @"
                SELECT t.id AS id, d.MaDon, t.TenVatTu AS ten, sp.Ma AS ma, sp.DonVi AS donVi,
                    (t.SoLuongMua - IFNULL(ls.TongSoLuong, 0)) AS yeuCau
                FROM ThongTinDatHang t
                INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN (SELECT ThongTinDatHang_ID, SUM(SoLuong) AS TongSoLuong FROM LichSuXuatNhap GROUP BY ThongTinDatHang_ID) ls
                    ON ls.ThongTinDatHang_ID = t.id
                WHERE d.MaDon = @maDon
                  AND (t.SoLuongMua - IFNULL(ls.TongSoLuong, 0)) > 0
                ORDER BY t.id;";

            if (isEdit)
            {
                sql = @"
                    SELECT l.id, l.TenPhieu AS MaDon, t.TenVatTu AS ten, sp.Ma AS ma, sp.DonVi AS donVi,
                        ABS(l.soluong) as thucNhan, l.donGia, l.SoLuong * l.donGia AS thanhTien,
                        l.ngay, l.GhiChu, ncc.id AS idNcc, ncc.TenNCC AS NhaCungCap
                    FROM LichSuXuatNhap l
                    LEFT JOIN ThongTinDatHang t ON t.id = l.ThongTinDatHang_ID
                    INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                    LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                    LEFT JOIN DanhSachNCC ncc ON ncc.id = l.DanhSachNCC_ID
                    WHERE l.TenPhieu = @maDon AND l.CanEdit = 1
                      AND ((l.SoLuong > 0 AND @isNhap = 1) OR (l.SoLuong < 0 AND @isNhap = 0))";
            }

            var dt = new DataTable();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@maDon", maDon);
                cmd.Parameters.AddWithValue("@isNhap", isNhap ? 1 : 0);
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });
            return dt;
        }

        public static async Task<DataTable> LayChiTietDonDatHangXuatKho(string maDon, bool isEdit)
        {
            string sql = @"
                SELECT t.id AS id,
                    CASE WHEN t.DanhSachMaSP_ID IS NULL THEN t.TenVatTu ELSE sp.Ten END AS ten,
                    CASE WHEN t.DanhSachMaSP_ID IS NULL THEN NULL ELSE sp.Ma END AS ma,
                    d.MaDon AS maDon,
                    CASE WHEN t.DanhSachMaSP_ID IS NULL THEN NULL ELSE sp.DonVi END AS donVi,
                    IFNULL(ls.TonKho, 0) AS yeuCau
                FROM ThongTinDatHang t
                INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN (SELECT ThongTinDatHang_ID, SUM(SoLuong) AS TonKho FROM LichSuXuatNhap GROUP BY ThongTinDatHang_ID) ls
                    ON ls.ThongTinDatHang_ID = t.id
                WHERE ((t.DanhSachMaSP_ID IS NULL AND t.TenVatTu_KhongDau = @tenVatTu COLLATE NOCASE)
                    OR (t.DanhSachMaSP_ID IS NOT NULL AND sp.Ten_KhongDau = @tenVatTu COLLATE NOCASE))
                  AND IFNULL(ls.TonKho, 0) > 0 ORDER BY t.id;";

            if (isEdit)
            {
                sql = @"
                    SELECT l.id, l.TenPhieu AS MaDon, t.TenVatTu AS ten, sp.Ma AS ma, sp.DonVi AS donVi,
                        ABS(l.soluong) as thucNhan, l.donGia, l.SoLuong * l.donGia AS thanhTien,
                        l.ngay, l.GhiChu, ncc.id AS idNcc, ncc.TenNCC AS NhaCungCap
                    FROM LichSuXuatNhap l
                    LEFT JOIN ThongTinDatHang t ON t.id = l.ThongTinDatHang_ID
                    INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                    LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                    LEFT JOIN DanhSachNCC ncc ON ncc.id = l.DanhSachNCC_ID
                    WHERE l.TenPhieu = @maDon AND l.CanEdit = 1
                      AND ((l.SoLuong > 0 AND @isNhap = 1) OR (l.SoLuong < 0 AND @isNhap = 0))";
            }

            var dt = new DataTable();
            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@maDon", maDon);
                cmd.Parameters.AddWithValue("@isNhap", 0);
                cmd.Parameters.AddWithValue("@tenVatTu", maDon);
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });
            return dt;
        }

        public static async Task<DataTable> LayChiTietDonTheoTenVatTuXuatKho(string tenVatTu)
        {
            const string sql = @"
                SELECT t.id AS id, sp.Ten AS ten, sp.Ma AS ma, d.MaDon AS maDon, sp.DonVi AS donVi, IFNULL(ls.TonKho, 0) AS yeuCau
                FROM ThongTinDatHang t
                INNER JOIN DanhSachDatHang d ON d.id = t.DanhSachDatHang_ID
                LEFT JOIN DanhSachMaSP sp ON sp.id = t.DanhSachMaSP_ID
                LEFT JOIN (SELECT ThongTinDatHang_ID, SUM(SoLuong) AS TonKho FROM LichSuXuatNhap GROUP BY ThongTinDatHang_ID) ls
                    ON ls.ThongTinDatHang_ID = t.id
                WHERE sp.Ten_KhongDau = @tenVatTu COLLATE NOCASE AND IFNULL(ls.TonKho, 0) > 0
                ORDER BY t.id";

            var dt = new DataTable();
            await Task.Run(() =>
            {
                tenVatTu = Helper.Helper.BoDauTiengViet(tenVatTu.Trim());
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@tenVatTu", tenVatTu);
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            });
            return dt;
        }

        // ── Lưu xuất nhập kho ────────────────────────────────────────────────

        public static async Task<string> LuuLichSuXuatNhap(DataGridView dgv, string nguoiGiaoNhan, string lyDoChung,
            string ngay, decimal nhacc, int kho, string nguoiLam, bool isNhapKho = true)
        {
            if (dgv == null || dgv.Rows.Count == 0) return null;

            DateTime parsedNgay = DateTime.TryParse(ngay, out DateTime tempNgay) ? tempNgay : DateTime.Now;

            const string sqlInsert = @"
                INSERT INTO LichSuXuatNhap
                (ThongTinDatHang_ID, Ngay, DanhSachNCC_ID, NguoiGiao_Nhan, LyDo, SoLuong, DanhSachKho_ID, nguoiLam, GhiChu, DonGia, TenPhieu)
                VALUES (@ThongTinDatHang_ID, @Ngay, @nhacc, @NguoiGiao_Nhan, @LyDo, @SoLuong, @Kho, @nguoiLam, @GhiChu, @DonGia, @TenPhieu);";

            string tenPhieu = null;
            bool hasInsert = false;

            await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var tx = conn.BeginTransaction();
                using var cmd = new SQLiteCommand(sqlInsert, conn, tx);

                cmd.Parameters.Add("@ThongTinDatHang_ID", DbType.Int64);
                cmd.Parameters.Add("@Ngay", DbType.String);
                cmd.Parameters.Add("@nhacc", DbType.Decimal);
                cmd.Parameters.Add("@NguoiGiao_Nhan", DbType.String);
                cmd.Parameters.Add("@LyDo", DbType.String);
                cmd.Parameters.Add("@SoLuong", DbType.Decimal);
                cmd.Parameters.Add("@Kho", DbType.Int64);
                cmd.Parameters.Add("@nguoiLam", DbType.String);
                cmd.Parameters.Add("@GhiChu", DbType.String);
                cmd.Parameters.Add("@DonGia", DbType.String);
                cmd.Parameters.Add("@TenPhieu", DbType.String);

                try
                {
                    int soThuTu = DB_Base.GetSoLuongXuatNhapThangHienTai(isNhapKho) + 1;
                    string prefix = isNhapKho ? "KNK" : "KXK";
                    tenPhieu = $"{prefix}{parsedNgay:yy/MM}-{soThuTu:D4}";

                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue;
                        object idObj = row.Cells["id"]?.Value;
                        if (idObj == null || idObj == DBNull.Value) continue;
                        if (!long.TryParse(idObj.ToString(), out long ttdhId)) continue;

                        var raw = row.Cells["thucNhan"]?.Value?.ToString()?.Trim()?.Replace(',', '.');
                        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soLuong)) continue;
                        if (soLuong == 0) continue;

                        string ghiChuRieng = dgv.Columns.Contains("ghiChu") ? row.Cells["ghiChu"]?.Value?.ToString()?.Trim() : null;
                        string donGia = dgv.Columns.Contains("DonGia") ? row.Cells["DonGia"]?.Value?.ToString()?.Trim() : null;
                        string ghiChu = !string.IsNullOrWhiteSpace(ghiChuRieng) ? ghiChuRieng : ghiChuRieng?.Trim();

                        cmd.Parameters["@ThongTinDatHang_ID"].Value = ttdhId;
                        cmd.Parameters["@Ngay"].Value = parsedNgay.ToString("yyyy-MM-dd");
                        cmd.Parameters["@nhacc"].Value = nhacc == 0 ? (object)DBNull.Value : nhacc;
                        cmd.Parameters["@NguoiGiao_Nhan"].Value = nguoiGiaoNhan?.Trim() ?? "";
                        cmd.Parameters["@LyDo"].Value = lyDoChung;
                        cmd.Parameters["@GhiChu"].Value = string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu;
                        cmd.Parameters["@SoLuong"].Value = isNhapKho ? soLuong : -Math.Abs(soLuong);
                        cmd.Parameters["@DonGia"].Value = donGia;
                        cmd.Parameters["@TenPhieu"].Value = tenPhieu;
                        cmd.Parameters["@Kho"].Value = kho;
                        cmd.Parameters["@nguoiLam"].Value = nguoiLam;

                        cmd.ExecuteNonQuery();
                        hasInsert = true;
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            });

            return hasInsert ? tenPhieu : null;
        }

        public static async Task<string> LuuDonKhacAsync(DonKhacInfo info, List<DonKhacItem> items)
        {
            if (info == null || items == null || items.Count == 0) return null;

            var validItems = items.Where(x => x != null && !string.IsNullOrWhiteSpace(x.TenVatTu) && x.SoLuong != 0).ToList();
            if (validItems.Count == 0) return null;

            DateTime ngay = info.Ngay == default ? DateTime.Now : info.Ngay;
            int soThuTu = DB_Base.GetSoLuongXuatNhapThangHienTai(info.IsNhapKho) + 1;
            string prefix = info.IsNhapKho ? "KNK" : "KXK";
            string tenPhieu = $"{prefix}{ngay:yy/MM}-{soThuTu:D4}";
            string maDon = tenPhieu;

            const string sqlInsertDSDH = @"
                INSERT INTO DanhSachDatHang (MaDon, LoaiDon, DateInsert, NguoiDat)
                VALUES (@MaDon, @LoaiDon, @DateInsert, @NguoiDat);
                SELECT last_insert_rowid();";

            const string sqlInsertTTDH = @"
                INSERT INTO ThongTinDatHang
                (DanhSachMaSP_ID, DanhSachDatHang_ID, TenVatTu, TenVatTu_KhongDau, SoLuongMua, MucDichMua, NgayGiao, Date_Insert, DonGia)
                VALUES (@DanhSachMaSP_ID, @DanhSachDatHang_ID, @TenVatTu, @TenVatTu_KhongDau, @SoLuongMua, @MucDichMua, @NgayGiao, @Date_Insert, @DonGia);
                SELECT last_insert_rowid();";

            const string sqlInsertLSXN = @"
                INSERT INTO LichSuXuatNhap
                (ThongTinDatHang_ID, Ngay, NguoiGiao_Nhan, LyDo, SoLuong, DanhSachKho_ID, NguoiLam, DanhSachNCC_ID, GhiChu, DonGia, TenPhieu)
                VALUES (@ThongTinDatHang_ID, @Ngay, @NguoiGiao_Nhan, @LyDo, @SoLuong, @DanhSachKho_ID, @NguoiLam, @NhaCC, @GhiChu, @DonGia, @TenPhieu);";

            return await Task.Run(() =>
            {
                using var conn = DB_Base.OpenConnection();
                using var tx = conn.BeginTransaction();
                try
                {
                    long danhSachDatHangId = 0;

                    if (!info.IsNhapKho)
                    {
                        foreach (var item in validItems)
                            InsertLichSuXuatNhapInternal(conn, tx, sqlInsertLSXN, info, item, item.DanhSachMaSpId ?? 0, ngay, tenPhieu);
                    }
                    else
                    {
                        using (var cmdDon = new SQLiteCommand(sqlInsertDSDH, conn, tx))
                        {
                            cmdDon.Parameters.AddWithValue("@MaDon", maDon);
                            cmdDon.Parameters.AddWithValue("@LoaiDon", 1);
                            cmdDon.Parameters.AddWithValue("@DateInsert", ngay.ToString("yyyy-MM-dd"));
                            cmdDon.Parameters.AddWithValue("@NguoiDat", info.NguoiDat?.Trim() ?? "");
                            danhSachDatHangId = Convert.ToInt64(cmdDon.ExecuteScalar());
                        }

                        foreach (var item in validItems)
                        {
                            long ttdhId;
                            using (var cmdTTDH = new SQLiteCommand(sqlInsertTTDH, conn, tx))
                            {
                                cmdTTDH.Parameters.AddWithValue("@DanhSachMaSP_ID", item.DanhSachMaSpId.HasValue ? (object)item.DanhSachMaSpId.Value : DBNull.Value);
                                cmdTTDH.Parameters.AddWithValue("@DanhSachDatHang_ID", danhSachDatHangId);
                                cmdTTDH.Parameters.AddWithValue("@TenVatTu", item.TenVatTu.Trim());
                                cmdTTDH.Parameters.AddWithValue("@TenVatTu_KhongDau", item.TenVatTuKhongDau.Trim());
                                cmdTTDH.Parameters.AddWithValue("@SoLuongMua", item.SoLuong);
                                cmdTTDH.Parameters.AddWithValue("@MucDichMua", string.IsNullOrWhiteSpace(item.MucDichMua) ? DBNull.Value : (object)item.MucDichMua.Trim());
                                cmdTTDH.Parameters.AddWithValue("@NgayGiao", ngay.ToString("yyyy-MM-dd"));
                                cmdTTDH.Parameters.AddWithValue("@Date_Insert", ngay.ToString("yyyy-MM-dd"));
                                cmdTTDH.Parameters.AddWithValue("@DonGia", item.DonGia.HasValue ? (object)item.DonGia.Value : DBNull.Value);
                                ttdhId = Convert.ToInt64(cmdTTDH.ExecuteScalar());
                            }
                            InsertLichSuXuatNhapInternal(conn, tx, sqlInsertLSXN, info, item, ttdhId, ngay, tenPhieu);
                        }
                    }
                    tx.Commit();
                    return maDon;
                }
                catch { tx.Rollback(); throw; }
            });
        }

        private static void InsertLichSuXuatNhapInternal(SQLiteConnection conn, SQLiteTransaction tx,
            string sql, DonKhacInfo info, DonKhacItem item, long ttdhId, DateTime ngay, string tenPhieu)
        {
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ThongTinDatHang_ID", ttdhId);
            cmd.Parameters.AddWithValue("@Ngay", ngay.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@NguoiGiao_Nhan", info.NguoiGiaoNhan?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@LyDo", string.IsNullOrWhiteSpace(info.LyDoChung) ? DBNull.Value : (object)info.LyDoChung.Trim());
            cmd.Parameters.AddWithValue("@SoLuong", info.IsNhapKho ? item.SoLuong : -Math.Abs(item.SoLuong));
            cmd.Parameters.AddWithValue("@DanhSachKho_ID", info.KhoId < 1 ? (object)DBNull.Value : info.KhoId);
            cmd.Parameters.AddWithValue("@NguoiLam", info.NguoiLam?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@NhaCC", info.Nhacc == 0 ? (object)DBNull.Value : info.Nhacc);
            cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(item.GhiChu) ? DBNull.Value : (object)item.GhiChu.Trim());
            cmd.Parameters.AddWithValue("@DonGia", item.DonGia.HasValue ? (object)item.DonGia.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TenPhieu", tenPhieu);
            cmd.ExecuteNonQuery();
        }

        // ── Báo cáo ──────────────────────────────────────────────────────────

        public static DataTable GetBaoCao(string ngayBatDau, string ngayKetThuc, int kho, int tinhTrang)
        {
            var dt = new DataTable();
            var sql = new StringBuilder(@"
                SELECT dsdh.id AS DonHang_ID, dsdh.MaDon, dsdh.LoaiDon, dsdh.DateInsert AS NgayTaoDon, dsdh.NguoiDat,
                    ttdh.id AS ThongTinDatHang_ID, ttdh.TenVatTu, ttdh.SoLuongMua AS SL_YeuCau, ttdh.DonGia, ttdh.MucDichMua, ttdh.NgayGiao,
                    ttdh.Date_Insert AS NgayNhapChiTiet, lsxn.id AS LichSu_ID, lsxn.SoLuong, lsxn.NguoiGiao_Nhan, kho.TenKho,
                    lsxn.LyDo, lsxn.Ngay AS NgayXuatNhap, lsxn.TenPhieu, lsxn.GhiChu, lsxn.CanEdit AS Edit
                FROM DanhSachDatHang dsdh
                INNER JOIN ThongTinDatHang ttdh ON ttdh.DanhSachDatHang_ID = dsdh.id
                LEFT JOIN LichSuXuatNhap lsxn ON lsxn.ThongTinDatHang_ID = ttdh.id
                INNER JOIN DanhSachKho Kho ON Kho.id = lsxn.DanhSachKho_ID
                WHERE 1=1");

            var parameters = new List<SQLiteParameter>();
            if (!string.IsNullOrEmpty(ngayBatDau) && !string.IsNullOrEmpty(ngayKetThuc))
            {
                sql.AppendLine("AND dsdh.DateInsert >= @NgayBatDau AND dsdh.DateInsert <= @NgayKetThuc");
                parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
            }
            if (kho != 0) { sql.AppendLine("AND lsxn.Kho = @Kho"); parameters.Add(new SQLiteParameter("@Kho", kho)); }
            switch (tinhTrang)
            {
                case 1: sql.AppendLine("AND ttdh.id NOT IN (SELECT DISTINCT ThongTinDatHang_ID FROM LichSuXuatNhap WHERE ThongTinDatHang_ID IS NOT NULL)"); break;
                case 2: sql.AppendLine("AND ttdh.SoLuongMua != COALESCE((SELECT SUM(sl.SoLuong) FROM LichSuXuatNhap sl WHERE sl.ThongTinDatHang_ID = ttdh.id), 0)"); break;
                case 3: sql.AppendLine("AND ttdh.SoLuongMua = COALESCE((SELECT SUM(sl.SoLuong) FROM LichSuXuatNhap sl WHERE sl.ThongTinDatHang_ID = ttdh.id), 0)"); break;
            }
            sql.AppendLine("ORDER BY dsdh.DateInsert DESC, dsdh.id DESC");

            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { throw new Exception($"GetBaoCao lỗi: {ex.Message}", ex); }
            return dt;
        }

        public static DataTable GetBaoCaoDatHang(string ngayBatDau, string ngayKetThuc, string nguoiThucHien)
        {
            var dt = new DataTable();
            var sql = new StringBuilder(@"
                SELECT dsdh.id AS DanhSachDatHang_ID, dsdh.nguoiDat, dsdh.MaDon, dsdh.LoaiDon,
                    dsdh.DateInsert AS NgayTaoDon, dsdh.NguoiDat, ttdh.id AS ThongTinDatHang_ID,
                    ttdh.TenVatTu, ttdh.SoLuongMua, ttdh.DonGia, ttdh.MucDichMua, ttdh.NgayGiao,
                    ttdh.Date_Insert AS NgayNhapChiTiet,
                    CASE WHEN EXISTS (SELECT 1 FROM LichSuXuatNhap lsxn INNER JOIN ThongTinDatHang t ON t.id = lsxn.ThongTinDatHang_ID WHERE t.DanhSachDatHang_ID = dsdh.id) THEN 0 ELSE 1 END AS Edit
                FROM DanhSachDatHang dsdh INNER JOIN ThongTinDatHang ttdh ON ttdh.DanhSachDatHang_ID = dsdh.id
                WHERE 1=1");
            var parameters = new List<SQLiteParameter>();
            if (!string.IsNullOrWhiteSpace(ngayBatDau) && !string.IsNullOrWhiteSpace(ngayKetThuc))
            {
                sql.AppendLine("AND dsdh.DateInsert >= @NgayBatDau AND dsdh.DateInsert <= @NgayKetThuc");
                parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
            }
            sql.AppendLine("ORDER BY dsdh.DateInsert DESC, dsdh.id DESC, ttdh.id;");
            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { throw new Exception($"GetBaoCaoDatHang lỗi: {ex.Message}", ex); }
            return dt;
        }

        public static DataTable GetBaoCaoDatHang_v2(string nguoiThucHien, DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            var sql = new StringBuilder(@"
                SELECT dh.id AS dh_id, dh.MaDon AS dh_MaDon, dh.NguoiDat AS dh_NguoiDat,
                    tt.TenVatTu AS tt_TenVatTu, tt.SoLuongMua AS tt_SoLuongMua, tt.MucDichMua AS tt_MucDichMua,
                    strftime('%d/%m/%Y', tt.NgayGiao) AS NgayGiao, strftime('%d/%m/%Y', tt.Date_Insert) AS Date_Insert, tt.DonGia AS tt_DonGia
                FROM ThongTinDatHang tt
                INNER JOIN DanhSachDatHang dh ON tt.DanhSachDatHang_ID = dh.id
                LEFT JOIN DanhSachMaSP sp ON tt.DanhSachMaSP_ID = sp.id
                WHERE 1 = 1");
            var parameters = new List<SQLiteParameter>();
            if (!string.IsNullOrWhiteSpace(nguoiThucHien)) { sql.AppendLine(" AND dh.NguoiDat = @NguoiDat "); parameters.Add(new SQLiteParameter("@NguoiDat", nguoiThucHien.Trim())); }
            if (ngayBatDau.HasValue) { sql.AppendLine(" AND DATE(dh.DateInsert) >= DATE(@NgayBatDau) "); parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd"))); }
            if (ngayKetThuc.HasValue) { sql.AppendLine(" AND DATE(dh.DateInsert) <= DATE(@NgayKetThuc) "); parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd"))); }
            sql.AppendLine(" ORDER BY dh.DateInsert DESC, dh.id DESC, tt.id DESC ");
            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { throw new Exception($"GetBaoCaoDatHang lỗi: {ex.Message}", ex); }
            return dt;
        }

        public static DataTable GetBaoCaoLichSuXuatNhap(string ngayBatDau, string ngayKetThuc, int kho,
            int tinhTrang, string nguoiThucHien, bool soLuongDuong)
        {
            var dt = new DataTable();
            var sql = new StringBuilder(@"
                SELECT lsxn.id AS LichSu_ID, dsdh.MaDon, dsdh.DateInsert AS NgayTaoDon,
                    ttdh.id AS ThongTinDatHang_ID, ttdh.TenVatTu, ttdh.SoLuongMua, ttdh.DonGia, ttdh.MucDichMua,
                    lsxn.NguoiLam, lsxn.SoLuong, lsxn.NguoiGiao_Nhan, kho.TenKho, lsxn.LyDo,
                    lsxn.Ngay AS NgayXuatNhap, lsxn.TenPhieu, lsxn.GhiChu, lsxn.CanEdit AS Edit, lsxn.DonGia AS DonGiaPhieu
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh ON ttdh.id = lsxn.ThongTinDatHang_ID
                INNER JOIN DanhSachDatHang dsdh ON dsdh.id = ttdh.DanhSachDatHang_ID
                INNER JOIN DanhSachKho kho ON kho.id = lsxn.DanhSachKho_ID
                WHERE ");
            sql.AppendLine(soLuongDuong ? "lsxn.SoLuong > 0" : "lsxn.SoLuong < 0");

            var parameters = new List<SQLiteParameter>();
            if (!string.IsNullOrWhiteSpace(ngayBatDau) && !string.IsNullOrWhiteSpace(ngayKetThuc))
            {
                sql.AppendLine("AND lsxn.Ngay >= @NgayBatDau AND lsxn.Ngay <= @NgayKetThuc");
                parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau));
                parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc));
            }
            if (!string.IsNullOrWhiteSpace(nguoiThucHien)) { sql.AppendLine("AND lsxn.NguoiLam = @NguoiLam"); parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien)); }
            if (kho != 0) { sql.AppendLine("AND kho.id = @Kho"); parameters.Add(new SQLiteParameter("@Kho", kho)); }
            if (tinhTrang == 1) sql.AppendLine("AND lsxn.CanEdit = 1");
            else if (tinhTrang == 2) sql.AppendLine("AND lsxn.CanEdit = 0");
            sql.AppendLine("ORDER BY lsxn.Ngay DESC, lsxn.id DESC;");

            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { throw new Exception($"GetBaoCaoLichSuXuatNhap lỗi: {ex.Message}", ex); }
            return dt;
        }

        public static DataTable GetBaoCaoLichSuXuatNhap_v2(int kho, string nguoiThucHien, bool isNhap,
            DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            var dt = new DataTable();
            var sql = new StringBuilder(@"
                SELECT lsxn.id AS lsxn_id, strftime('%d/%m/%Y', lsxn.Ngay) AS NgayXuatNhap, lsxn.TenPhieu, lsxn.LyDo,
                    dssp.Ma, CASE WHEN ttdh.DanhSachMaSP_ID IS NULL THEN ttdh.TenVatTu ELSE dssp.Ten END AS TenVatTu,
                    lsxn.SoLuong, lsxn.DonGia, dsk.TenKho, ncc.TenNcc AS NCC, lsxn.GhiChu, dsdh.MaDon, dsdh.NguoiDat,
                    lsxn.NguoiGiao_Nhan AS NguoiGiaoNhan, strftime('%d/%m/%Y', dsdh.DateInsert) AS NgayDatPR,
                    lsxn.NguoiLam, ttdh.SoLuongMua, ttdh.MucDichMua,
                    strftime('%d/%m/%Y', ttdh.NgayGiao) AS NgayGiao, strftime('%d/%m/%Y', ttdh.Date_Insert) AS Date_Insert,
                    lsxn.canEdit, ttdh.DonGia AS DonGia
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh ON lsxn.ThongTinDatHang_ID = ttdh.id
                INNER JOIN DanhSachDatHang dsdh ON ttdh.DanhSachDatHang_ID = dsdh.id
                LEFT JOIN DanhSachMaSP dssp ON ttdh.DanhSachMaSP_ID = dssp.id
                LEFT JOIN DanhSachKho dsk ON lsxn.DanhSachKho_ID = dsk.id
                LEFT JOIN DanhSachNcc ncc ON lsxn.DanhSachNcc_ID = ncc.id
                WHERE 1 = 1");
            var parameters = new List<SQLiteParameter>();
            if (kho > 0) { sql.AppendLine(" AND lsxn.DanhSachKho_ID = @DanhSachKho_ID "); parameters.Add(new SQLiteParameter("@DanhSachKho_ID", kho)); }
            if (!string.IsNullOrWhiteSpace(nguoiThucHien)) { sql.AppendLine(" AND lsxn.NguoiLam = @NguoiLam "); parameters.Add(new SQLiteParameter("@NguoiLam", nguoiThucHien.Trim())); }
            if (ngayBatDau.HasValue) { sql.AppendLine(" AND DATE(lsxn.Ngay) >= DATE(@NgayBatDau) "); parameters.Add(new SQLiteParameter("@NgayBatDau", ngayBatDau.Value.ToString("yyyy-MM-dd"))); }
            if (ngayKetThuc.HasValue) { sql.AppendLine(" AND DATE(lsxn.Ngay) <= DATE(@NgayKetThuc) "); parameters.Add(new SQLiteParameter("@NgayKetThuc", ngayKetThuc.Value.ToString("yyyy-MM-dd"))); }
            sql.AppendLine(isNhap ? " AND lsxn.SoLuong > 0 " : " AND lsxn.SoLuong < 0 ");
            sql.AppendLine(" ORDER BY lsxn.Ngay DESC, lsxn.id DESC; ");
            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());
                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex) { throw new Exception($"GetBaoCaoLichSuXuatNhap lỗi: {ex.Message}", ex); }
            return dt;
        }

        // ── ThongTinDatHang Update ────────────────────────────────────────────

        public static void UpdateThongTinDatHang(ThongTinDatHangUpdateModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            const string sql = @"
                UPDATE ThongTinDatHang
                SET TenVatTu = @TenVatTu, SoLuongMua = @SoLuongMua, DonGia = @DonGia,
                    MucDichMua = @MucDichMua, NgayGiao = @NgayGiao
                WHERE id = @Id;";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@TenVatTu", model.TenVatTu);
                cmd.Parameters.AddWithValue("@SoLuongMua", model.SoLuongMua);
                cmd.Parameters.AddWithValue("@DonGia", model.DonGia);
                cmd.Parameters.AddWithValue("@MucDichMua", string.IsNullOrWhiteSpace(model.MucDichMua) ? (object)DBNull.Value : model.MucDichMua.Trim());
                cmd.Parameters.AddWithValue("@NgayGiao", string.IsNullOrWhiteSpace(model.NgayGiao) ? (object)DBNull.Value : model.NgayGiao.Trim());
                cmd.Parameters.AddWithValue("@Id", model.Id);
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0) throw new Exception($"Không tìm thấy ThongTinDatHang ID = {model.Id}.");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Tiện ích ────────────────────────────────────────────────────────

        public static int GetSoLuongDonThangHienTai()
        {
            var now = DateTime.Now;
            string start = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
            string end = new DateTime(now.Year, now.Month, 1).AddMonths(1).ToString("yyyy-MM-dd");
            const string sql = "SELECT COUNT(*) FROM DanhSachDatHang WHERE NgayThem >= @start AND NgayThem < @end";
            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);
            var result = cmd.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }
    }
}