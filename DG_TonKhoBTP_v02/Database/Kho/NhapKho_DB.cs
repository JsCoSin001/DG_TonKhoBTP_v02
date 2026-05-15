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
    public static class NhapKho_DB
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

        /// <summary>
        /// Lấy danh sách kích thước lô từ TTLo để đổ vào ComboBox nrChieuCaoLo.
        /// </summary>
        public static DataTable LayDanhSachKichThuocLo()
        {
            const string sql = @"
                SELECT KichThuoc
                FROM TTLo
                WHERE TRIM(IFNULL(KichThuoc, '')) <> ''
                ORDER BY CAST(KichThuoc AS REAL), KichThuoc;";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỨC NĂNG 1 – NHẬP KHO (INSERT TTNhapKho + TTCuonDay), trả về id vừa tạo
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// INSERT một bản ghi vào TTNhapKho, sau đó INSERT từng dòng cuộn/lô
        /// vào TTCuonDay (ThongTinNhapKho_ID = id vừa tạo), và cập nhật TTThanhPham.
        /// Toàn bộ thực hiện trong 1 transaction – ném ngoại lệ nếu thất bại.
        /// </summary>
        /// <param name="model">Thông tin header nhập kho.</param>
        /// <param name="dsCuon">
        /// Danh sách cuộn / lô chi tiết (từ Frm_DLCuon).
        /// Không được null; có thể rỗng nếu người dùng chưa nhập chi tiết.
        /// </param>
        /// <returns>id (rowid) của bản ghi TTNhapKho vừa tạo.</returns>
        public static long NhapKho(
            DG_TonKhoBTP_v02.Models.NhapKho_Model model,
            List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> dsCuon)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (dsCuon == null) dsCuon = new List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay>();

            bool isLo = model.Loai == "Lô";

            // ── SQL INSERT TTNhapKho ────────────────────────────────────────────
            const string sqlInsertNhapKho = @"
                INSERT INTO TTNhapKho
                    (Ngay, SoBB, TTThanhPham_ID, TenSP, SoMet,
                     LoaiDon, KhachHang, GhiChu,
                     Loai, ChieuCaoLo,
                     NguoiLam, TenDuAn)
                VALUES
                    (@Ngay, @SoBB, @TTThanhPham_ID, @TenSP, @SoMet,
                     @LoaiDon, @KhachHang, @GhiChu,
                     @Loai, @ChieuCaoLo,
                     @NguoiLam, @TenDuAn);
                SELECT last_insert_rowid();";

            // ── SQL INSERT TTCuonDay ────────────────────────────────────────────
            const string sqlInsertCuon = @"
                INSERT INTO TTCuonDay
                    (SoCuon,TongChieuDai, SoDau, SoCuoi, GhiChu, ThongTinNhapKho_ID)
                VALUES
                    (@SoCuon,@TongChieuDai, @SoDau, @SoCuoi, @GhiChu, @ThongTinNhapKho_ID);";

            // ── SQL cập nhật TTThanhPham ────────────────────────────────────────
            const string sqlUpdateThanhPham = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = 0,
                    ChieuDaiSau  = 0,
                    NhapKho      = 1
                WHERE id = @id;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                // ── Bước 1: INSERT TTNhapKho ────────────────────────────────────
                long newId;
                using (var cmd = new SQLiteCommand(sqlInsertNhapKho, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Ngay",
                        string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay);
                    cmd.Parameters.AddWithValue("@SoBB", model.SoBB);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", model.TTThanhPham_ID);
                    cmd.Parameters.AddWithValue("@TenSP",
                        string.IsNullOrWhiteSpace(model.TenSP) ? (object)DBNull.Value : model.TenSP);
                    cmd.Parameters.AddWithValue("@SoMet", model.SoMet);
                    cmd.Parameters.AddWithValue("@LoaiDon",
                        string.IsNullOrWhiteSpace(model.LoaiDon) ? (object)DBNull.Value : model.LoaiDon);
                    cmd.Parameters.AddWithValue("@KhachHang",
                        string.IsNullOrWhiteSpace(model.KhachHang) ? (object)DBNull.Value : model.KhachHang);
                    cmd.Parameters.AddWithValue("@GhiChu",
                        string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu);
                    cmd.Parameters.AddWithValue("@Loai", model.Loai);
                    cmd.Parameters.AddWithValue("@ChieuCaoLo",
                        isLo ? (object)model.ChieuCaoLo : DBNull.Value);
                    cmd.Parameters.AddWithValue("@NguoiLam",
                        string.IsNullOrWhiteSpace(model.NguoiLam) ? (object)DBNull.Value : model.NguoiLam.Trim());
                    cmd.Parameters.AddWithValue("@TenDuAn",
                        string.IsNullOrWhiteSpace(model.TenDuAn) ? (object)DBNull.Value : model.TenDuAn.Trim());

                    newId = (long)cmd.ExecuteScalar();
                }

                // ── Bước 2: INSERT từng dòng vào TTCuonDay ──────────────────────
                if (dsCuon.Count > 0)
                {
                    using var cmdCuon = new SQLiteCommand(sqlInsertCuon, conn, tran);
                    cmdCuon.Parameters.Add("@SoCuon", DbType.Int32);
                    cmdCuon.Parameters.Add("@TongChieuDai", DbType.Int32);
                    cmdCuon.Parameters.Add("@SoDau", DbType.Int32);
                    cmdCuon.Parameters.Add("@SoCuoi", DbType.Int32);
                    cmdCuon.Parameters.Add("@GhiChu", DbType.String);
                    cmdCuon.Parameters.Add("@ThongTinNhapKho_ID", DbType.Int64);

                    foreach (var cuon in dsCuon)
                    {
                        cmdCuon.Parameters["@SoCuon"].Value = cuon.SoCuon;
                        cmdCuon.Parameters["@TongChieuDai"].Value = cuon.TongChieuDai;
                        cmdCuon.Parameters["@SoDau"].Value = cuon.SoDau;
                        cmdCuon.Parameters["@SoCuoi"].Value = cuon.SoCuoi;
                        cmdCuon.Parameters["@GhiChu"].Value =
                            string.IsNullOrWhiteSpace(cuon.Ghichu) ? (object)DBNull.Value : cuon.Ghichu;
                        cmdCuon.Parameters["@ThongTinNhapKho_ID"].Value = newId;
                        cmdCuon.ExecuteNonQuery();
                    }
                }

                // ── Bước 3: Cập nhật TTThanhPham ────────────────────────────────
                using (var cmd3 = new SQLiteCommand(sqlUpdateThanhPham, conn, tran))
                {
                    cmd3.Parameters.AddWithValue("@id", model.TTThanhPham_ID);
                    if (cmd3.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTThanhPham id={model.TTThanhPham_ID} sau khi nhập kho.");
                }

                tran.Commit();

                // Ghi lại id vào model để caller có thể dùng ngay
                model.Id = newId;
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
        public static void CapNhatNhapKho(
    long idNhapKho,
    long ttThanhPhamIdCu,
    double soMetCu,
    DG_TonKhoBTP_v02.Models.NhapKho_Model model,
    List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> dsCuon,
    bool capNhatTTCuonDay)
        {
            if (idNhapKho <= 0)
                throw new ArgumentException("idNhapKho không hợp lệ.", nameof(idNhapKho));

            if (ttThanhPhamIdCu <= 0)
                throw new ArgumentException("ttThanhPhamIdCu không hợp lệ.", nameof(ttThanhPhamIdCu));

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (model.TTThanhPham_ID <= 0)
                throw new ArgumentException("TTThanhPham_ID mới không hợp lệ.", nameof(model));

            if (capNhatTTCuonDay && (dsCuon == null || dsCuon.Count == 0))
                throw new InvalidOperationException("Không có dữ liệu TTCuonDay mới để cập nhật.");

            bool isLo = model.Loai == "Lô";

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
            NguoiLam       = @NguoiLam,
            TenDuAn        = @TenDuAn
        WHERE id = @id;";

            const string sqlDeleteCuonDay = @"
        DELETE FROM TTCuonDay
        WHERE ThongTinNhapKho_ID = @ThongTinNhapKho_ID;";

            const string sqlInsertCuonDay = @"
        INSERT INTO TTCuonDay
            (SoCuon, TongChieuDai, SoDau, SoCuoi, GhiChu, ThongTinNhapKho_ID)
        VALUES
            (@SoCuon, @TongChieuDai, @SoDau, @SoCuoi, @GhiChu, @ThongTinNhapKho_ID);";

            const string sqlCapNhatMoi = @"
        UPDATE TTThanhPham
        SET KhoiLuongSau = 0,
            ChieuDaiSau  = 0,
            NhapKho      = 1
        WHERE id = @idMoi;";

            using var conn = DB_Base.OpenConnection();
            using var tran = conn.BeginTransaction();

            try
            {
                // Bước 1: Rollback TTThanhPham cũ.
                using (var cmd = new SQLiteCommand(sqlRollbackCu, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@SoMetCu", soMetCu);
                    cmd.Parameters.AddWithValue("@idCu", ttThanhPhamIdCu);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không rollback được TTThanhPham id={ttThanhPhamIdCu}.");
                }

                // Bước 2: Update header TTNhapKho theo schema mới.
                using (var cmd = new SQLiteCommand(sqlUpdateNhapKho, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", idNhapKho);

                    cmd.Parameters.AddWithValue("@Ngay",
                        string.IsNullOrWhiteSpace(model.Ngay) ? (object)DBNull.Value : model.Ngay);

                    cmd.Parameters.AddWithValue("@SoBB", model.SoBB);
                    cmd.Parameters.AddWithValue("@TTThanhPham_ID", model.TTThanhPham_ID);

                    cmd.Parameters.AddWithValue("@TenSP",
                        string.IsNullOrWhiteSpace(model.TenSP) ? (object)DBNull.Value : model.TenSP);

                    cmd.Parameters.AddWithValue("@SoMet", model.SoMet);

                    cmd.Parameters.AddWithValue("@LoaiDon",
                        string.IsNullOrWhiteSpace(model.LoaiDon) ? (object)DBNull.Value : model.LoaiDon);

                    cmd.Parameters.AddWithValue("@KhachHang",
                        string.IsNullOrWhiteSpace(model.KhachHang) ? (object)DBNull.Value : model.KhachHang);

                    cmd.Parameters.AddWithValue("@GhiChu",
                        string.IsNullOrWhiteSpace(model.GhiChu) ? (object)DBNull.Value : model.GhiChu);

                    cmd.Parameters.AddWithValue("@Loai",
                        string.IsNullOrWhiteSpace(model.Loai) ? (object)DBNull.Value : model.Loai);

                    cmd.Parameters.AddWithValue("@ChieuCaoLo",
                        isLo ? (object)model.ChieuCaoLo : DBNull.Value);

                    cmd.Parameters.AddWithValue("@NguoiLam",
                        string.IsNullOrWhiteSpace(model.NguoiLam) ? (object)DBNull.Value : model.NguoiLam.Trim());

                    cmd.Parameters.AddWithValue("@TenDuAn",
                        string.IsNullOrWhiteSpace(model.TenDuAn) ? (object)DBNull.Value : model.TenDuAn.Trim());

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTNhapKho id={idNhapKho}.");
                }

                // Bước 3: Chỉ update TTCuonDay nếu người dùng đã sửa trong Frm_DLCuon.
                if (capNhatTTCuonDay)
                {
                    using (var cmd = new SQLiteCommand(sqlDeleteCuonDay, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@ThongTinNhapKho_ID", idNhapKho);
                        cmd.ExecuteNonQuery();
                    }

                    using var cmdInsertCuon = new SQLiteCommand(sqlInsertCuonDay, conn, tran);

                    cmdInsertCuon.Parameters.Add("@SoCuon", DbType.Int32);
                    cmdInsertCuon.Parameters.Add("@TongChieuDai", DbType.Int32);
                    cmdInsertCuon.Parameters.Add("@SoDau", DbType.Int32);
                    cmdInsertCuon.Parameters.Add("@SoCuoi", DbType.Int32);
                    cmdInsertCuon.Parameters.Add("@GhiChu", DbType.String);
                    cmdInsertCuon.Parameters.Add("@ThongTinNhapKho_ID", DbType.Int64);

                    foreach (var cuon in dsCuon)
                    {
                        cmdInsertCuon.Parameters["@SoCuon"].Value = cuon.SoCuon;
                        cmdInsertCuon.Parameters["@TongChieuDai"].Value = cuon.TongChieuDai;
                        cmdInsertCuon.Parameters["@SoDau"].Value = cuon.SoDau;
                        cmdInsertCuon.Parameters["@SoCuoi"].Value = cuon.SoCuoi;

                        cmdInsertCuon.Parameters["@GhiChu"].Value =
                            string.IsNullOrWhiteSpace(cuon.Ghichu) ? (object)DBNull.Value : cuon.Ghichu;

                        cmdInsertCuon.Parameters["@ThongTinNhapKho_ID"].Value = idNhapKho;

                        cmdInsertCuon.ExecuteNonQuery();
                    }
                }

                // Bước 4: Cập nhật TTThanhPham mới.
                using (var cmd = new SQLiteCommand(sqlCapNhatMoi, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@idMoi", model.TTThanhPham_ID);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException(
                            $"Không cập nhật được TTThanhPham mới id={model.TTThanhPham_ID}.");
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

            string keywordDateDMY = keyword;
            string keywordDateISO = keyword;

            int? keywordSoBB = null;
            if (int.TryParse(keyword, out int soBB))
                keywordSoBB = soBB;

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
                keywordDateDMY = parsedDate.ToString("dd/MM/yyyy");
                keywordDateISO = parsedDate.ToString("yyyy-MM-dd");
            }

            const string sql = @"
            WITH found AS
            (
                SELECT
                    nk.id                   AS id_NhapKho,
                    nk.TTThanhPham_ID       AS TTThanhPham_ID,

                    CASE
                        WHEN nk.Ngay LIKE '____-__-__'
                        THEN strftime('%d/%m/%Y', nk.Ngay)
                        ELSE nk.Ngay
                    END                     AS ngay,

                    nk.SoBB                 AS soBB,
                    nk.NguoiLam             AS nguoiLam,
                    nk.TenSP                AS tenSP,
                    IFNULL(tp.MaBin, '')    AS maBin2,
                    nk.SoMet                AS soMet,
                    nk.LoaiDon              AS loaiDon,
                    nk.KhachHang            AS khachHang,
                    nk.Loai                 AS loai,
                    nk.ChieuCaoLo           AS chieuCaoLo,
                    nk.GhiChu               AS ghiChu,
                    nk.TenDuAn              AS tenDuAn,

                    bs.TenChiTiet           AS tenChiTiet,
                    bs.TieuChuan            AS tieuChuan,
                    CAST(IFNULL(bs.T, 0) AS REAL) AS heSoT,

                    lo.KhoiLuong            AS klLoKhoiLuong,
                    lo.KhoiLuongCaNanPhu    AS klLoKhoiLuongCaNanPhu

                FROM TTNhapKho nk
                LEFT JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                LEFT JOIN TTBoSung bs    ON bs.DanhSachMaSP_ID = tp.DanhSachSP_ID
                LEFT JOIN TTLo lo        ON CAST(lo.KichThuoc AS TEXT) = CAST(nk.ChieuCaoLo AS TEXT)
                WHERE
                       TRIM(IFNULL(nk.TenSP, '')) = @keyword COLLATE NOCASE
                    OR TRIM(IFNULL(nk.Ngay, '')) = @keyword COLLATE NOCASE
                    OR TRIM(IFNULL(nk.Ngay, '')) = @keywordDateDMY COLLATE NOCASE
                    OR TRIM(IFNULL(nk.Ngay, '')) = @keywordDateISO COLLATE NOCASE
                    OR (@keywordSoBB IS NOT NULL AND nk.SoBB = @keywordSoBB)
                    OR TRIM(IFNULL(tp.MaBin, '')) = @keyword COLLATE NOCASE
                ORDER BY nk.id DESC
                LIMIT 200
            )
            SELECT
                f.*,
                cd.SoCuon       AS ct_SoCuon,
                cd.TongChieuDai AS ct_TongChieuDai,
                cd.SoDau        AS ct_SoDau,
                cd.SoCuoi       AS ct_SoCuoi,
                cd.GhiChu       AS ct_GhiChu
            FROM found f
            LEFT JOIN TTCuonDay cd
                    ON cd.ThongTinNhapKho_ID = f.id_NhapKho
            ORDER BY f.id_NhapKho DESC, cd.rowid;";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@keyword", keyword);
            cmd.Parameters.AddWithValue("@keywordDateDMY", keywordDateDMY);
            cmd.Parameters.AddWithValue("@keywordDateISO", keywordDateISO);
            cmd.Parameters.AddWithValue("@keywordSoBB", keywordSoBB.HasValue ? (object)keywordSoBB.Value : DBNull.Value);

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

        public static List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> LayThongTinCuonDay(long idNhapKho)
        {
            List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay> result =
                new List<DG_TonKhoBTP_v02.Models.ThongTinCuonDay>();

            if (idNhapKho <= 0)
                return result;

            const string sql = @"
        SELECT
            SoCuon,
            TongChieuDai,
            SoDau,
            SoCuoi,
            GhiChu
        FROM TTCuonDay
        WHERE ThongTinNhapKho_ID = @ThongTinNhapKho_ID
        ORDER BY id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@ThongTinNhapKho_ID", idNhapKho);

            using SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new DG_TonKhoBTP_v02.Models.ThongTinCuonDay
                {
                    SoCuon = reader["SoCuon"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuon"]),
                    TongChieuDai = reader["TongChieuDai"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TongChieuDai"]),
                    SoDau = reader["SoDau"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoDau"]),
                    SoCuoi = reader["SoCuoi"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoCuoi"]),
                    Ghichu = reader["GhiChu"] == DBNull.Value ? string.Empty : reader["GhiChu"].ToString()
                });
            }

            return result;
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

        // ════════════════════════════════════════════════════════════════════════
        // LẤY THÔNG TIN TTBoSung VÀ TTLo ĐỂ ĐIỀN VÀO GRID
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Trả về thông tin từ TTBoSung (tenChiTiet, tieuChuan, heSoT)
        /// và TTLo (klKhoiLuong, klKhoiLuongCaNanPhu) theo TTThanhPham_ID và chieuCaoLo.
        /// </summary>
        public static ThongTinBoSungVaLo LayThongTinBoSungVaLo(long ttThanhPhamId, double chieuCaoLo)
        {
            const string sql = @"
                SELECT
                    bs.TenChiTiet               AS tenChiTiet,
                    bs.TieuChuan                AS tieuChuan,
                    CAST(IFNULL(bs.T, 0) AS REAL) AS heSoT,
                    lo.KhoiLuong                AS klKhoiLuong,
                    lo.KhoiLuongCaNanPhu        AS klKhoiLuongCaNanPhu
                FROM TTThanhPham tp
                LEFT JOIN TTBoSung bs ON bs.DanhSachMaSP_ID = tp.DanhSachSP_ID
                LEFT JOIN TTLo lo     ON CAST(lo.KichThuoc AS TEXT) = CAST(@chieuCaoLo AS TEXT)
                WHERE tp.id = @ttThanhPhamId
                LIMIT 1;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ttThanhPhamId", ttThanhPhamId);
            cmd.Parameters.AddWithValue("@chieuCaoLo", chieuCaoLo);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return new ThongTinBoSungVaLo();

            return new ThongTinBoSungVaLo
            {
                TenChiTiet = reader["tenChiTiet"] == DBNull.Value ? string.Empty : reader["tenChiTiet"].ToString(),
                TieuChuan = reader["tieuChuan"] == DBNull.Value ? string.Empty : reader["tieuChuan"].ToString(),
                HeSoT = reader["heSoT"] == DBNull.Value ? 0 : Convert.ToDouble(reader["heSoT"]),
                KlKhoiLuong = reader["klKhoiLuong"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["klKhoiLuong"]),
                KlKhoiLuongCaNanPhu = reader["klKhoiLuongCaNanPhu"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["klKhoiLuongCaNanPhu"]),
            };
        }
    }

    /// <summary>DTO chứa kết quả từ TTBoSung và TTLo.</summary>
    public class ThongTinBoSungVaLo
    {
        public string TenChiTiet { get; set; } = string.Empty;
        public string TieuChuan { get; set; } = string.Empty;
        public double HeSoT { get; set; }
        public double? KlKhoiLuong { get; set; }
        public double? KlKhoiLuongCaNanPhu { get; set; }
    }
}