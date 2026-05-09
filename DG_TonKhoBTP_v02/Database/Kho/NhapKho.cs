using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Database.ChatLuong
{
    public static class NhapKho
    {
        public static Task<DataTable> TimKiemMaBinAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                    SELECT  tp.id          AS TTThanhPham_ID,
                            tp.MaBin,
                            sp.Ten,
                            tp.ChieuDaiSau,
                            tp.GhiChu
                    FROM    TTThanhPham   tp
                    JOIN    DanhSachMaSP  sp ON sp.id = tp.DanhSachSP_ID
                    WHERE   tp.MaBin LIKE @keyword AND tp.active = 1 AND tp.CongDoan = 5 AND tp.ChieuDaiSau > 5 
                    ORDER BY tp.MaBin
                    LIMIT   50";

                var dt = new DataTable();

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                return dt;

            }, ct);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 1 – LƯU MỘT DÒNG MỚI (INSERT), trả về id vừa tạo
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// INSERT một bản ghi vào TTNhapKho và cập nhật TTThanhPham.
        /// Trả về id (rowid) vừa được tạo.
        /// Ném ngoại lệ nếu thất bại.
        /// </summary>
        public static long LuuMotDong(
            string ngay,
            int soBB,
            long ttThanhPhamId,
            string tenSP,
            double soMet,
            string loaiDon,
            string khachHang,
            string ghiChu,
            string loai,
            double chieuCaoLo,
            double tongChieuDai,
            int soDau,
            int soCuoi,
            string thongTinCuon,
            string nguoiLam)
        {
            const string sqlInsert = @"
                INSERT INTO TTNhapKho
                    (Ngay, SoBB, TTThanhPham_ID, TenSP, SoMet,
                     LoaiDon, KhachHang, GhiChu,
                     Loai, ChieuCaoLo, TongChieuDai, SoDau, SoCuoi, ThongTinCuon,
                     NguoiLam)
                VALUES
                    (@Ngay, @SoBB, @TTThanhPham_ID, @TenSP, @SoMet,
                     @LoaiDon, @KhachHang, @GhiChu,
                     @Loai, @ChieuCaoLo, @TongChieuDai, @SoDau, @SoCuoi, @ThongTinCuon,
                     @NguoiLam);
                SELECT last_insert_rowid();";

            const string sqlUpdateThanhPham = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = 0,
                    ChieuDaiSau  = 0,
                    NhapKho      = 1
                WHERE id = @id;";

            bool isLo = loai == "Lô";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                long newId;

                using (var cmd = new SQLiteCommand(sqlInsert, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Ngay", string.IsNullOrWhiteSpace(ngay) ? (object)DBNull.Value : ngay);
                    cmd.Parameters.AddWithValue("@SoBB", soBB);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamId);
                    cmd.Parameters.AddWithValue("@TenSP", string.IsNullOrWhiteSpace(tenSP) ? (object)DBNull.Value : tenSP);
                    cmd.Parameters.AddWithValue("@SoMet", soMet);
                    cmd.Parameters.AddWithValue("@LoaiDon", string.IsNullOrWhiteSpace(loaiDon) ? (object)DBNull.Value : loaiDon);
                    cmd.Parameters.AddWithValue("@KhachHang", string.IsNullOrWhiteSpace(khachHang) ? (object)DBNull.Value : khachHang);
                    cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu);
                    cmd.Parameters.AddWithValue("@Loai", loai);
                    cmd.Parameters.AddWithValue("@ChieuCaoLo", isLo ? (object)chieuCaoLo : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TongChieuDai", isLo ? (object)tongChieuDai : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SoDau", isLo ? (object)soDau : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SoCuoi", isLo ? (object)soCuoi : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ThongTinCuon", isLo ? (object)DBNull.Value : (string.IsNullOrWhiteSpace(thongTinCuon) ? (object)DBNull.Value : thongTinCuon));
                    cmd.Parameters.AddWithValue("@NguoiLam", string.IsNullOrWhiteSpace(nguoiLam) ? (object)DBNull.Value : nguoiLam.Trim());

                    newId = (long)cmd.ExecuteScalar();
                }

                using (var cmd2 = new SQLiteCommand(sqlUpdateThanhPham, conn, tran))
                {
                    cmd2.Parameters.AddWithValue("@id", ttThanhPhamId);
                    int updated = cmd2.ExecuteNonQuery();

                    if (updated == 0)
                        throw new InvalidOperationException("Không cập nhật được TTThanhPham sau khi nhập kho.");
                }

                tran.Commit();
                return newId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 2 – CẬP NHẬT MỘT DÒNG ĐÃ CÓ (UPDATE theo id_NhapKho)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// UPDATE bản ghi TTNhapKho theo id. Luôn thực hiện đủ 3 bước trong 1 transaction:
        ///   1. Rollback TTThanhPham cũ: ChieuDaiSau = soMetCu, NhapKho = 0
        ///      (ttThanhPhamIdCu và soMetCu được truyền vào từ caller – lấy từ grid, không SELECT lại DB)
        ///   2. UPDATE TTNhapKho với dữ liệu mới
        ///   3. Cập nhật TTThanhPham mới: ChieuDaiSau = 0, NhapKho = 1
        /// Ném ngoại lệ nếu bất kỳ bước nào thất bại (tự động rollback transaction).
        /// </summary>
        public static void CapNhatMotDong(
            long idNhapKho,
            long ttThanhPhamIdCu,   // lấy từ grid cell "TTThanhPham_ID"
            double soMetCu,         // lấy từ grid cell "soMet"
            string ngay,
            int soBB,
            long ttThanhPhamIdMoi,
            string tenSP,
            double soMet,
            string loaiDon,
            string khachHang,
            string ghiChu,
            string loai,
            double chieuCaoLo,
            double tongChieuDai,
            int soDau,
            int soCuoi,
            string thongTinCuon,
            string nguoiLam)
        {
            bool isLo = loai == "Lô";

            const string sqlRollbackCu = @"
                UPDATE TTThanhPham
                SET ChieuDaiSau = @SoMetCu,
                    NhapKho     = 0
                WHERE id = @idCu;";

            const string sqlUpdateNhapKho = @"
                UPDATE TTNhapKho SET
                    Ngay           = @Ngay,
                    SoBB           = @SoBB,
                    TTThanhPham_ID = @TTThanhPham_ID,
                    TenSP          = @TenSP,
                    SoMet          = @SoMet,
                    LoaiDon        = @LoaiDon,
                    KhachHang      = @KhachHang,
                    GhiChu         = @GhiChu,
                    Loai           = @Loai,
                    ChieuCaoLo     = @ChieuCaoLo,
                    TongChieuDai   = @TongChieuDai,
                    SoDau          = @SoDau,
                    SoCuoi         = @SoCuoi,
                    ThongTinCuon   = @ThongTinCuon,
                    NguoiLam       = @NguoiLam
                WHERE id = @id;";

            const string sqlCapNhatMoi = @"
                UPDATE TTThanhPham
                SET ChieuDaiSau = 0,
                    NhapKho     = 1
                WHERE id = @idMoi;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                // ── Bước 1: Rollback TTThanhPham cũ ────────────────────────────────
                // ttThanhPhamIdCu và soMetCu đã được caller đọc từ grid → không cần SELECT lại DB
                using (var cmd = new SQLiteCommand(sqlRollbackCu, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@SoMetCu", soMetCu);
                    cmd.Parameters.AddWithValue("@idCu", ttThanhPhamIdCu);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không rollback được TTThanhPham id={ttThanhPhamIdCu}.");
                }

                // ── Bước 2: UPDATE TTNhapKho ────────────────────────────────────────
                using (var cmd = new SQLiteCommand(sqlUpdateNhapKho, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);
                    cmd.Parameters.AddWithValue("@Ngay", string.IsNullOrWhiteSpace(ngay) ? (object)DBNull.Value : ngay);
                    cmd.Parameters.AddWithValue("@SoBB", soBB);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", ttThanhPhamIdMoi);
                    cmd.Parameters.AddWithValue("@TenSP", string.IsNullOrWhiteSpace(tenSP) ? (object)DBNull.Value : tenSP);
                    cmd.Parameters.AddWithValue("@SoMet", soMet);
                    cmd.Parameters.AddWithValue("@LoaiDon", string.IsNullOrWhiteSpace(loaiDon) ? (object)DBNull.Value : loaiDon);
                    cmd.Parameters.AddWithValue("@KhachHang", string.IsNullOrWhiteSpace(khachHang) ? (object)DBNull.Value : khachHang);
                    cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? (object)DBNull.Value : ghiChu);
                    cmd.Parameters.AddWithValue("@Loai", loai);
                    cmd.Parameters.AddWithValue("@ChieuCaoLo", isLo ? (object)chieuCaoLo : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TongChieuDai", isLo ? (object)tongChieuDai : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SoDau", isLo ? (object)soDau : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SoCuoi", isLo ? (object)soCuoi : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ThongTinCuon", isLo ? (object)DBNull.Value : (string.IsNullOrWhiteSpace(thongTinCuon) ? (object)DBNull.Value : thongTinCuon));
                    cmd.Parameters.AddWithValue("@NguoiLam", string.IsNullOrWhiteSpace(nguoiLam) ? (object)DBNull.Value : nguoiLam.Trim());

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTNhapKho id={idNhapKho}.");
                }

                // ── Bước 3: Cập nhật TTThanhPham mới ───────────────────────────────
                using (var cmd = new SQLiteCommand(sqlCapNhatMoi, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@idMoi", ttThanhPhamIdMoi);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTThanhPham mới id={ttThanhPhamIdMoi}.");
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // GIỮ NGUYÊN – LuuDanhSachNhapKho (batch insert từ grid – dùng nếu cần)
        // ════════════════════════════════════════════════════════════════════════

        public static int LuuDanhSachNhapKho(DataGridView grid, string nguoiLam)
        {
            if (grid == null || grid.Rows.Count == 0)
                return 0;

            const string sqlInsert = @"
            INSERT INTO TTNhapKho
                (Ngay, SoBB, TTThanhPham_ID, TenSP, SoMet,
                 LoaiDon, KhachHang, GhiChu,
                 Loai, ChieuCaoLo, TongChieuDai, SoDau, SoCuoi, ThongTinCuon,
                 NguoiLam)
            VALUES
                (@Ngay, @SoBB, @TTThanhPham_ID, @TenSP, @SoMet,
                 @LoaiDon, @KhachHang, @GhiChu,
                 @Loai, @ChieuCaoLo, @TongChieuDai, @SoDau, @SoCuoi, @ThongTinCuon,
                 @NguoiLam);";

            int rowsInserted = 0;
            List<long> dsTTThanhPhamId = new List<long>();

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                using var cmdInsert = new SQLiteCommand(sqlInsert, conn, tran);

                cmdInsert.Parameters.Add("@Ngay", DbType.String);
                cmdInsert.Parameters.Add("@SoBB", DbType.Int32);
                cmdInsert.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
                cmdInsert.Parameters.Add("@TenSP", DbType.String);
                cmdInsert.Parameters.Add("@SoMet", DbType.Double);
                cmdInsert.Parameters.Add("@LoaiDon", DbType.String);
                cmdInsert.Parameters.Add("@KhachHang", DbType.String);
                cmdInsert.Parameters.Add("@GhiChu", DbType.String);
                cmdInsert.Parameters.Add("@Loai", DbType.String);
                cmdInsert.Parameters.Add("@ChieuCaoLo", DbType.Double);
                cmdInsert.Parameters.Add("@TongChieuDai", DbType.Double);
                cmdInsert.Parameters.Add("@SoDau", DbType.Int32);
                cmdInsert.Parameters.Add("@SoCuoi", DbType.Int32);
                cmdInsert.Parameters.Add("@ThongTinCuon", DbType.String);
                cmdInsert.Parameters.Add("@NguoiLam", DbType.String);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    long? ttThanhPhamId = ParseLong(row, "TTThanhPham_ID");
                    if (!ttThanhPhamId.HasValue)
                        throw new InvalidOperationException("Không tìm thấy TTThanhPham_ID để cập nhật nhập kho.");

                    string loai = ParseStr(row, "loai");
                    bool isLo = loai == "Lô";

                    cmdInsert.Parameters["@Ngay"].Value = ParseStr(row, "ngay") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@SoBB"].Value = ParseInt(row, "soBB") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@TTThanhPham_ID"].Value = ttThanhPhamId.Value;
                    cmdInsert.Parameters["@TenSP"].Value = ParseStr(row, "tenSP") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@SoMet"].Value = ParseDbl(row, "soMet") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@LoaiDon"].Value = ParseStr(row, "loaiDon") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@KhachHang"].Value = ParseStr(row, "khachHang") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@GhiChu"].Value = ParseStr(row, "ghiChu") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@Loai"].Value = loai ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@ChieuCaoLo"].Value = isLo ? ParseDbl(row, "chieuCaoLo") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@TongChieuDai"].Value = isLo ? ParseDbl(row, "tongChieuDai") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@SoDau"].Value = isLo ? ParseInt(row, "soDau") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@SoCuoi"].Value = isLo ? ParseInt(row, "soCuoi") ?? (object)DBNull.Value : DBNull.Value;
                    cmdInsert.Parameters["@ThongTinCuon"].Value = isLo ? DBNull.Value : ParseStr(row, "cuon") ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@NguoiLam"].Value = string.IsNullOrWhiteSpace(nguoiLam) ? (object)DBNull.Value : nguoiLam.Trim();

                    cmdInsert.ExecuteNonQuery();
                    dsTTThanhPhamId.Add(ttThanhPhamId.Value);
                    rowsInserted++;
                }

                List<long> dsIdKhongTrung = dsTTThanhPhamId.Distinct().ToList();

                if (dsIdKhongTrung.Count > 0)
                {
                    List<string> paramNames = new List<string>();
                    for (int i = 0; i < dsIdKhongTrung.Count; i++)
                        paramNames.Add("@id" + i);

                    string sqlUpdateThanhPham = $@"
                        UPDATE TTThanhPham
                        SET KhoiLuongSau = 0,
                            ChieuDaiSau = 0,
                            NhapKho = 1
                        WHERE id IN ({string.Join(",", paramNames)});";

                    using var cmdUpdateThanhPham = new SQLiteCommand(sqlUpdateThanhPham, conn, tran);
                    for (int i = 0; i < dsIdKhongTrung.Count; i++)
                        cmdUpdateThanhPham.Parameters.AddWithValue("@id" + i, dsIdKhongTrung[i]);

                    int updated = cmdUpdateThanhPham.ExecuteNonQuery();
                    if (updated == 0)
                        throw new InvalidOperationException("Không cập nhật được dữ liệu TTThanhPham sau khi nhập kho.");
                }

                tran.Commit();
                return rowsInserted;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 3 – TÌM KIẾM
        // ════════════════════════════════════════════════════════════════════════

        public static DataTable TimKiemNhapKho(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            string keywordDate = keyword;

            string[] dateFormats =
            {
                "dd/MM/yyyy", "d/M/yyyy",
                "yyyy-MM-dd",
                "dd-MM-yyyy", "d-M-yyyy"
            };

            if (DateTime.TryParseExact(
                    keyword,
                    dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                keywordDate = parsedDate.ToString("dd/MM/yyyy");
            }

            const string sql = @"
                SELECT
                    nk.id                   AS id_NhapKho,
                    nk.TTThanhPham_ID       AS TTThanhPham_ID,
                    nk.Ngay                 AS ngay,
                    nk.SoBB                 AS soBB,
                    nk.TenSP                AS tenSP,
                    IFNULL(tp.MaBin, '')    AS maBin2,
                    nk.SoMet                AS soMet,
                    nk.LoaiDon              AS loaiDon,
                    nk.KhachHang            AS khachHang,
                    nk.SoDau                AS soDau,
                    nk.SoCuoi               AS soCuoi,
                    nk.Loai                 AS loai,
                    nk.ChieuCaoLo           AS chieuCaoLo,
                    nk.TongChieuDai         AS tongChieuDai,
                    nk.ThongTinCuon         AS cuon,
                    nk.GhiChu               AS ghiChu
                FROM TTNhapKho nk
                LEFT JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                WHERE
                       nk.TenSP LIKE @keyword
                    OR nk.Ngay LIKE @keyword
                    OR nk.Ngay LIKE @keywordDate
                    OR CAST(nk.SoBB AS TEXT) LIKE @keyword
                    OR IFNULL(tp.MaBin, '') LIKE @keyword
                ORDER BY nk.id DESC
                LIMIT 200;";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");
            cmd.Parameters.AddWithValue("@keywordDate", "%" + keywordDate + "%");

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        public static void XoaMotDong(long idNhapKho, long ttThanhPhamId, double soMet)
        {
            const string sqlRollback = @"
            UPDATE TTThanhPham
            SET ChieuDaiSau = @SoMet,
                NhapKho     = 0
            WHERE id = @id;";

            const string sqlDelete = @"
            DELETE FROM TTNhapKho
            WHERE id = @id;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                // Bước 1: Rollback TTThanhPham
                using (var cmd = new SQLiteCommand(sqlRollback, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@SoMet", soMet);
                    cmd.Parameters.AddWithValue("@id", ttThanhPhamId);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không rollback được TTThanhPham id={ttThanhPhamId}.");
                }

                // Bước 2: Xoá TTNhapKho
                using (var cmd = new SQLiteCommand(sqlDelete, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không xoá được TTNhapKho id={idNhapKho}.");
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private static string ParseStr(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value?.ToString();
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        }

        private static int? ParseInt(DataGridViewRow row, string col) =>
            int.TryParse(row.Cells[col].Value?.ToString(), out int v) ? v : (int?)null;

        private static long? ParseLong(DataGridViewRow row, string col) =>
            long.TryParse(row.Cells[col].Value?.ToString(), out long v) ? v : (long?)null;

        private static double? ParseDbl(DataGridViewRow row, string col) =>
            double.TryParse(
                row.Cells[col].Value?.ToString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture,
                out double v) ? v : (double?)null;
    }
}