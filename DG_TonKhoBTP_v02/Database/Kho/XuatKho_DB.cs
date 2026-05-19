using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    public static class XuatKho_DB
    {
        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM LOT / BIN CÒN TỒN KHO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm các bin khớp với keyword và chỉ trả về những bin còn tồn kho thực tế.
        /// Trả về: MaBin, Ten, TTThanhPham_ID, DanhSachMaSP_ID, Loai (của TTNhapKho).
        /// Không lọc TTThanhPham.NhapKho; chỉ lọc TTNhapKho.Kieu = 1 theo nghiệp vụ tồn khả dụng.
        /// </summary>
        public static Task<DataTable> TimKiemLotAsync(string keyword, CancellationToken ct)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                WITH ton AS (
                    SELECT  nk.TTThanhPham_ID AS TTThanhPham_ID,
                            tp.MaBin,
                            sp.Ten,
                            sp.id AS DanhSachMaSP_ID,
                            nk.Loai,
                            cd.id AS TTCuonDay_ID,
                            cd.SoDau AS SoDauNhap,
                            CASE
                                WHEN nk.Loai = 'Lô'
                                    THEN COALESCE(MIN(xk.SoDau), cd.SoCuoi)
                                ELSE cd.SoCuoi
                            END AS SoCuoiTon,
                            CASE
                                WHEN nk.Loai = 'Cuộn'
                                    THEN COALESCE(cd.SoCuon, 0) - COALESCE(SUM(COALESCE(xk.SoCuon, 0)), 0)
                                WHEN nk.Loai = 'Lô'
                                    THEN 1
                                ELSE 0
                            END AS SoCuonTon
                    FROM    TTNhapKho    nk
                    JOIN    TTThanhPham  tp ON tp.id = nk.TTThanhPham_ID
                    JOIN    DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                    JOIN    TTCuonDay    cd ON cd.ThongTinNhapKho_ID = nk.id
                    LEFT JOIN TTXuatKho  xk ON xk.TTCuonDay_ID = cd.id
                    WHERE   nk.Kieu = 1
                      AND  (tp.MaBin           LIKE @kw
                            OR sp.Ten_KhongDau LIKE @kw
                            OR sp.Ten          LIKE @kw)
                    GROUP BY nk.id, cd.id
                )
                SELECT DISTINCT
                        TTThanhPham_ID,
                        MaBin,
                        Ten,
                        DanhSachMaSP_ID,
                        Loai
                FROM    ton
                WHERE  (Loai = 'Cuộn' AND SoCuonTon > 0)
                   OR  (Loai = 'Lô'   AND SoCuoiTon > SoDauNhap)
                ORDER BY MaBin
                LIMIT 50";

                DataTable dt = new DataTable();

                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);

                cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                return dt;

            }, ct);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LẤY DỮ LIỆU TỒN KHO CUỘN / LÔ THEO THÀNH PHẨM
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy tồn kho thực tế theo TTThanhPham_ID.
        /// Trả về thêm TTCuonDay_ID để dùng làm khoá khi lưu TTXuatKho.
        ///
        /// Quy tắc:
        /// - Cuộn: SoCuonTon = SoCuon nhập - tổng SoCuon đã xuất.
        /// - Lô:   SoCuoiTon = nếu chưa xuất thì SoCuoi nhập,
        ///                 nếu đã xuất thì MIN(SoDau xuất), vì xuất giảm dần từ cuối về đầu.
        /// Chỉ trả về dòng còn tồn và TTNhapKho.Kieu = 1.
        /// </summary>
        public static DataTable LayDuLieuTonKhoCuonDay(long ttThanhPhamId)
        {
            return LayDuLieuTonKhoCuonDayNoiBo(ttThanhPhamId, null);
        }

        /// <summary>
        /// Lấy tồn khả dụng khi sửa một phiếu xuất cũ.
        /// Phiếu đang sửa được loại trừ khỏi phần tổng xuất, tương đương:
        /// tồn khả dụng khi sửa = tồn hiện tại + lượng đã xuất của chính phiếu đang sửa.
        /// </summary>
        public static DataTable LayDuLieuTonKhoCuonDayChoSua(long ttThanhPhamId, long xuatKhoIdDangSua)
        {
            return LayDuLieuTonKhoCuonDayNoiBo(ttThanhPhamId, xuatKhoIdDangSua);
        }

        private static DataTable LayDuLieuTonKhoCuonDayNoiBo(long ttThanhPhamId, long? xuatKhoIdDangSua)
        {
            DataTable dt = new DataTable();

            if (ttThanhPhamId <= 0)
                return dt;

            const string sql = @"
                WITH ton AS (
                    SELECT  nk.id AS TTNhapKho_ID,
                            cd.id AS TTCuonDay_ID,
                            nk.Loai,
                            cd.SoDau AS SoDauNhap,
                            cd.SoCuoi AS SoCuoiNhap,
                            cd.GhiChu,
                            CASE
                                WHEN nk.Loai = 'Lô'
                                    THEN 1
                                WHEN nk.Loai = 'Cuộn'
                                    THEN COALESCE(cd.SoCuon, 0) - COALESCE(SUM(COALESCE(xk.SoCuon, 0)), 0)
                                ELSE 0
                            END AS SoCuonTon,
                            CASE
                                WHEN nk.Loai = 'Lô'
                                    THEN COALESCE(MIN(xk.SoDau), cd.SoCuoi)
                                ELSE cd.SoCuoi
                            END AS SoCuoiTon
                    FROM    TTNhapKho   nk
                    JOIN    TTCuonDay   cd ON cd.ThongTinNhapKho_ID = nk.id
                    LEFT JOIN TTXuatKho xk ON xk.TTCuonDay_ID = cd.id
                                           AND (@xuatKhoIdDangSua IS NULL OR xk.id <> @xuatKhoIdDangSua)
                    WHERE   nk.TTThanhPham_ID = @id
                      AND   nk.Kieu = 1
                    GROUP BY nk.id, cd.id
                )
                SELECT  TTNhapKho_ID,
                        TTCuonDay_ID,
                        CASE
                            WHEN Loai = 'Cuộn'
                                THEN SoCuonTon * (SoCuoiNhap - SoDauNhap)
                            WHEN Loai = 'Lô'
                                THEN SoCuoiTon - SoDauNhap
                            ELSE 0
                        END AS TongChieuDai,
                        SoCuonTon AS SoCuon,
                        SoDauNhap AS SoDau,
                        SoCuoiTon AS soCuoi,
                        GhiChu,
                        Loai
                FROM    ton
                WHERE  (Loai = 'Cuộn' AND SoCuonTon > 0)
                   OR  (Loai = 'Lô'   AND SoCuoiTon > SoDauNhap)
                ORDER BY TTNhapKho_ID, TTCuonDay_ID";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", ttThanhPhamId);
            cmd.Parameters.AddWithValue("@xuatKhoIdDangSua", (object?)xuatKhoIdDangSua ?? DBNull.Value);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Hàm cũ được giữ lại để tránh lỗi nếu còn nơi khác đang gọi.
        /// Nghiệp vụ xuất kho mới nên dùng LayDuLieuTonKhoCuonDay.
        /// </summary>
        [Obsolete("Dùng LayDuLieuTonKhoCuonDay để lấy tồn kho thực tế thay vì dữ liệu nhập gốc.")]
        public static DataTable LayDuLieuCuonDay(long ttThanhPhamId)
        {
            return LayDuLieuTonKhoCuonDay(ttThanhPhamId);
        }

        // ════════════════════════════════════════════════════════════════════════
        // THÊM MỚI BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Chèn một bản ghi vào TTXuatKho.
        /// Trả về id mới được chèn (last insert rowid).
        /// TongChieuDai = SoCuon * (soCuoi - SoDau) — tính ở tầng DB để nhất quán.
        /// </summary>
        public static long ThemXuatKho(
            long ttCuonDayId,
            int? soCuon,
            int? soDau,
            int? soCuoi,
            string ghiChu,
            string ngayXuat,
            string nguoiLam)
        {
            int tongChieuDai = 0;
            if (soCuon.HasValue && soDau.HasValue && soCuoi.HasValue)
                tongChieuDai = soCuon.Value * (soCuoi.Value - soDau.Value);

            const string sql = @"
                INSERT INTO TTXuatKho
                    (TTCuonDay_ID, SoCuon, TongChieuDai, SoDau, soCuoi, GhiChu, NgayXuat, NguoiLam)
                VALUES
                    (@ttCuonDayId, @soCuon, @tongChieuDai, @soDau, @soCuoi, @ghiChu, @ngayXuat, @nguoiLam);
                SELECT last_insert_rowid();";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@ttCuonDayId", ttCuonDayId);
            cmd.Parameters.AddWithValue("@soCuon", (object?)soCuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tongChieuDai", tongChieuDai);
            cmd.Parameters.AddWithValue("@soDau", (object?)soDau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soCuoi", (object?)soCuoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ghiChu", (object?)ghiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ngayXuat", ngayXuat);
            cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam);

            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        // ════════════════════════════════════════════════════════════════════════
        // CẬP NHẬT BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        public static void SuaXuatKho(
            long id,
            int? soCuon,
            int? soDau,
            int? soCuoi,
            string ghiChu,
            string ngayXuat,
            string nguoiLam)
        {
            if (!LaPhieuXuatLoMoiNhat(id))
            {
                throw new InvalidOperationException(
                    "Phiếu xuất loại Lô này không phải lần xuất mới nhất nên không được sửa trực tiếp. " +
                    "Vui lòng sửa/xoá các lần xuất sau trước, hoặc tạo nghiệp vụ điều chỉnh.");
            }

            int tongChieuDai = 0;
            if (soCuon.HasValue && soDau.HasValue && soCuoi.HasValue)
                tongChieuDai = soCuon.Value * (soCuoi.Value - soDau.Value);

            const string sql = @"
                UPDATE TTXuatKho
                SET    SoCuon       = @soCuon,
                       TongChieuDai = @tongChieuDai,
                       SoDau        = @soDau,
                       soCuoi       = @soCuoi,
                       GhiChu       = @ghiChu,
                       NgayXuat     = @ngayXuat,
                       NguoiLam     = @nguoiLam
                WHERE  id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@soCuon", (object?)soCuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tongChieuDai", tongChieuDai);
            cmd.Parameters.AddWithValue("@soDau", (object?)soDau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soCuoi", (object?)soCuoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ghiChu", (object?)ghiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ngayXuat", ngayXuat);
            cmd.Parameters.AddWithValue("@nguoiLam", nguoiLam);

            cmd.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════════════════════
        // XOÁ BẢN GHI XUẤT KHO
        // ════════════════════════════════════════════════════════════════════════

        public static void XoaXuatKho(long id)
        {
            const string sql = "DELETE FROM TTXuatKho WHERE id = @id";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Với Loại = 'Lô', chỉ cho sửa phiếu xuất mới nhất của cùng TTCuonDay_ID
        /// để tránh phá chuỗi xuất giảm dần từ cuối về đầu.
        /// Với Loại khác 'Lô', luôn trả về true.
        /// </summary>
        public static bool LaPhieuXuatLoMoiNhat(long xuatKhoId)
        {
            const string sql = @"
                SELECT CASE
                    WHEN nk.Loai <> 'Lô' THEN 1
                    WHEN NOT EXISTS (
                        SELECT 1
                        FROM   TTXuatKho xkSau
                        WHERE  xkSau.TTCuonDay_ID = xk.TTCuonDay_ID
                          AND  xkSau.id > xk.id
                    ) THEN 1
                    ELSE 0
                END AS IsMoiNhat
                FROM    TTXuatKho xk
                JOIN    TTCuonDay cd ON cd.id = xk.TTCuonDay_ID
                JOIN    TTNhapKho nk ON nk.id = cd.ThongTinNhapKho_ID
                WHERE   xk.id = @id
                LIMIT 1";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", xuatKhoId);

            object result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
        }

        public static DataTable LayChiTietXuatKho(long xuatKhoId)
        {
            const string sql = @"
            WITH chi_tiet AS (
                SELECT  xk.id AS XuatKho_ID,
                        nk.id AS TTNhapKho_ID,
                        cd.id AS TTCuonDay_ID,
                        nk.Loai,
                        cd.SoCuon AS SoCuonNhap,
                        cd.TongChieuDai AS TongChieuDaiNhap,
                        cd.SoDau AS SoDauNhap,
                        cd.SoCuoi AS SoCuoiNhap,
                        cd.GhiChu AS GhiChu_CD,
                        xk.SoCuon AS SoCuon_XK,
                        xk.SoDau AS SoDau_XK,
                        xk.SoCuoi AS soCuoi_XK,
                        xk.GhiChu AS GhiChu_XK,
                        xk.NgayXuat,
                        xk.NguoiLam,
                        COALESCE(SUM(COALESCE(xkKhac.SoCuon, 0)), 0) AS TongSoCuonXuatKhac,
                        MIN(xkKhac.SoDau) AS MinSoDauXuatKhac,
                        CASE
                            WHEN nk.Loai <> 'Lô' THEN 1
                            WHEN NOT EXISTS (
                                SELECT 1
                                FROM   TTXuatKho xkSau
                                WHERE  xkSau.TTCuonDay_ID = xk.TTCuonDay_ID
                                  AND  xkSau.id > xk.id
                            ) THEN 1
                            ELSE 0
                        END AS IsLoMoiNhat
                FROM    TTXuatKho    xk
                JOIN    TTCuonDay    cd ON cd.id  = xk.TTCuonDay_ID
                JOIN    TTNhapKho    nk ON nk.id  = cd.ThongTinNhapKho_ID
                LEFT JOIN TTXuatKho xkKhac ON xkKhac.TTCuonDay_ID = cd.id
                                          AND xkKhac.id <> xk.id
                WHERE   xk.id = @id
                GROUP BY xk.id
            )
            SELECT  TTNhapKho_ID,
                    TTCuonDay_ID,
                    CASE
                        WHEN Loai = 'Cuộn'
                            THEN (COALESCE(SoCuonNhap, 0) - TongSoCuonXuatKhac) * (SoCuoiNhap - SoDauNhap)
                        WHEN Loai = 'Lô'
                            THEN COALESCE(MinSoDauXuatKhac, SoCuoiNhap) - SoDauNhap
                        ELSE TongChieuDaiNhap
                    END AS TongChieuDai_NK,
                    CASE
                        WHEN Loai = 'Lô' THEN 1
                        WHEN Loai = 'Cuộn' THEN COALESCE(SoCuonNhap, 0) - TongSoCuonXuatKhac
                        ELSE SoCuonNhap
                    END AS SoCuon_CD,
                    SoDauNhap AS SoDau_CD,
                    CASE
                        WHEN Loai = 'Lô' THEN COALESCE(MinSoDauXuatKhac, SoCuoiNhap)
                        ELSE SoCuoiNhap
                    END AS soCuoi_CD,
                    GhiChu_CD,
                    SoCuon_XK,
                    SoDau_XK,
                    soCuoi_XK,
                    GhiChu_XK,
                    NgayXuat,
                    NguoiLam,
                    Loai,
                    IsLoMoiNhat
            FROM    chi_tiet
            LIMIT 1";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", xuatKhoId);

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }


        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM LỊCH SỬ XUẤT KHO (cho dataGridView1 / preview)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm kiếm theo: Tên sản phẩm, NgayXuat, MaBin.
        /// Trả về đủ thông tin để hiển thị lên dataGridView1.
        /// </summary>
        public static DataTable TimKiemXuatKho(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            const string sql = @"
                SELECT  xk.id               AS id_preview,
                        xk.TongChieuDai     AS tongCD_preview,
                        sp.Ten              AS ten_preview,
                        tp.MaBin            AS lot_preview,
                        cd.SoCuon           AS soCuon_preview,
                        cd.SoDau            AS soDau_preview,
                        cd.soCuoi           AS soCuoi_preview,
                        cd.GhiChu           AS ghiChu_preview,
                        xk.SoCuon           AS SoCuon_user_preview,
                        xk.SoDau            AS soDau_user_preview,
                        xk.soCuoi           AS soCuoi_user_preview,
                        xk.GhiChu           AS ghiChu_user_preview,
                        xk.NgayXuat,
                        xk.NguoiLam,
                        xk.TTCuonDay_ID
                FROM    TTXuatKho       xk
                JOIN    TTCuonDay       cd ON cd.id           = xk.TTCuonDay_ID
                JOIN    TTNhapKho       nk ON nk.id           = cd.ThongTinNhapKho_ID
                JOIN    TTThanhPham     tp ON tp.id           = nk.TTThanhPham_ID
                JOIN    DanhSachMaSP    sp ON sp.id           = tp.DanhSachSP_ID
                WHERE  (sp.Ten          LIKE @kw
                        OR sp.Ten_KhongDau LIKE @kw
                        OR xk.NgayXuat  LIKE @kw
                        OR tp.MaBin     LIKE @kw)
                ORDER BY xk.id DESC
                LIMIT 200";

            DataTable dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }
    }
}
