using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static class CatDay_DB
    {
        private const int SuggestionLimit = 50;

        /// <summary>
        /// Search-as-you-type cho cbxTimKiem.
        /// Chỉ trả về LOT / Tên SP / Khách hàng có ít nhất một TTCuonDay CÒN TỒN.
        /// Tồn được tính theo đúng 2 nhóm:
        /// - SoDau/SoCuoi đều NULL: tồn theo số lượng.
        /// - SoDau/SoCuoi đều có giá trị: tồn theo chiều dài.
        /// </summary>
        public static Task<DataTable> TimKiemGiaTriAsync(
            CatDay_SearchType searchType,
            string keyword,
            CancellationToken ct)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (searchType == CatDay_SearchType.ChieuDai)
                    return TaoBangSuggestionRong();

                string keywordForQuery = searchType == CatDay_SearchType.KhachHang
                    ? ChuyenKhongDau(keyword)
                    : keyword;

                string sql = XayDungSqlSuggestion(searchType);

                DataTable dt = new DataTable();
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@kw", TaoLikeContains(keywordForQuery));
                cmd.Parameters.AddWithValue("@limit", SuggestionLimit);

                ct.ThrowIfCancellationRequested();

                using var adapter = new SQLiteDataAdapter(cmd);
                adapter.Fill(dt);

                ct.ThrowIfCancellationRequested();
                return dt;
            }, ct);
        }

        /// <summary>
        /// Tìm theo điều kiện UI, nhưng chỉ trả về TTCuonDay còn tồn.
        /// Ngày tìm kiếm luôn dựa trên TTCuonDay.Ngay.
        /// </summary>
        public static CatDay_SearchResult TimKiem(CatDay_SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.LayToanBo = false;
            return ThucThiTimKiem(criteria);
        }

        /// <summary>
        /// Lấy toàn bộ TTCuonDay còn tồn theo quy tắc 2 nhóm.
        /// </summary>
        public static CatDay_SearchResult LayToanBo()
        {
            return ThucThiTimKiem(new CatDay_SearchCriteria
            {
                LayToanBo = true
            });
        }

        private static CatDay_SearchResult ThucThiTimKiem(CatDay_SearchCriteria criteria)
        {
            string sql = XayDungSql(criteria);
            var result = new CatDay_SearchResult();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            GanThamSo(cmd, criteria);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long ttCuonDayId = Convert.ToInt64(reader["TTCuonDay_ID"], CultureInfo.InvariantCulture);
                string lot = DbText(reader["Lot"]);

                long? soDauGoc = DbInt64Nullable(reader["SoDau"]);
                long? soCuoiGoc = DbInt64Nullable(reader["SoCuoi"]);
                long? soCuonGoc = DbInt64Nullable(reader["SoCuon"]);
                long chieuDaiMotDonVi = Convert.ToInt64(reader["ChieuDai_1cuon"], CultureInfo.InvariantCulture);
                long tongChieuDaiCat = Convert.ToInt64(reader["TongChieuDaiCat"], CultureInfo.InvariantCulture);
                long tongSoLuong = Convert.ToInt64(reader["TongSoLuong"], CultureInfo.InvariantCulture);
                bool coChieuDaiCatAm = Convert.ToInt32(reader["CoChieuDaiCatAm"], CultureInfo.InvariantCulture) != 0;
                bool coSoLuongAm = Convert.ToInt32(reader["CoSoLuongAm"], CultureInfo.InvariantCulture) != 0;

                // Chỉ một trong SoDau/SoCuoi NULL là dữ liệu không thuộc 2 nhóm hợp lệ.
                // Theo yêu cầu: không hiển thị, không tự suy diễn sang nhóm còn lại.
                if (soDauGoc.HasValue != soCuoiGoc.HasValue)
                    continue;

                string loai = reader["TTLo_ID"] == DBNull.Value ? "Cuộn" : "Lô";
                string khachHang = DbText(reader["KhachHang"]);

                // NHÓM SỐ LƯỢNG: SoDau và SoCuoi đều NULL.
                if (!soDauGoc.HasValue && !soCuoiGoc.HasValue)
                {
                    if (coSoLuongAm)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            null,
                            "Có LichSuCatDay.SoLuong âm; đã loại toàn bộ TTCuonDay khỏi kết quả.");
                        continue;
                    }

                    if (!soCuonGoc.HasValue || soCuonGoc.Value <= 0)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonGoc,
                            "TTCuonDay.SoCuon không hợp lệ cho nhóm quản lý theo số lượng.");
                        continue;
                    }

                    long soCuonCon = soCuonGoc.Value - tongSoLuong;
                    if (soCuonCon < 0)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonCon,
                            "Số cuộn còn lại âm: TTCuonDay.SoCuon - SUM(LichSuCatDay.SoLuong) < 0.");
                        continue;
                    }

                    // Chỉ hiển thị khi còn tồn.
                    if (soCuonCon == 0)
                        continue;

                    // Tìm theo chiều dài ở nhóm số lượng dùng ChieuDai_1cuon.
                    if (criteria.SearchType == CatDay_SearchType.ChieuDai
                        && criteria.ChieuDaiToiThieu.HasValue
                        && chieuDaiMotDonVi < criteria.ChieuDaiToiThieu.Value)
                    {
                        continue;
                    }

                    if (soCuonCon > int.MaxValue)
                    {
                        ThemDataIssue(
                            result,
                            ttCuonDayId,
                            lot,
                            soCuonCon,
                            "Số cuộn còn lại vượt phạm vi dữ liệu mà giao diện có thể hiển thị.");
                        continue;
                    }

                    result.Rows.Add(new CatDay_Row
                    {
                        TTCuonDay_ID = ttCuonDayId,
                        Lot = lot,
                        TenSP = DbText(reader["TenSP"]),
                        KhachHang = khachHang,
                        Loai = loai,
                        NhomTon = CatDay_InventoryGroup.SoLuong,
                        SoCuon = (int)soCuonCon,
                        SoDau = null,
                        SoCuoi = null,
                        ChieuDaiConLai = chieuDaiMotDonVi,
                        TongChieuDai = soCuonCon * chieuDaiMotDonVi
                    });

                    continue;
                }

                // NHÓM CHIỀU DÀI: SoDau và SoCuoi đều có giá trị.
                if (coChieuDaiCatAm)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        null,
                        "Có LichSuCatDay.ChieuDaiCat âm; đã loại toàn bộ TTCuonDay khỏi kết quả.");
                    continue;
                }

                // Đã chốt: nhóm chiều dài phải có đúng 1 cuộn.
                if (!soCuonGoc.HasValue || soCuonGoc.Value != 1)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        soCuonGoc,
                        "Nhóm quản lý theo chiều dài nhưng TTCuonDay.SoCuon khác 1.");
                    continue;
                }

                long soDau = soDauGoc.Value;
                long soCuoi = soCuoiGoc.Value;

                // SoDau == SoCuoi được coi là hết tồn, không cảnh báo.
                if (soDau == soCuoi)
                    continue;

                long chieuDaiGoc = Math.Abs(soCuoi - soDau);
                long chieuDaiCon = chieuDaiGoc - tongChieuDaiCat;

                if (chieuDaiCon < 0)
                {
                    ThemDataIssue(
                        result,
                        ttCuonDayId,
                        lot,
                        chieuDaiCon,
                        "Chiều dài còn lại âm: ABS(SoCuoi - SoDau) - SUM(ChieuDaiCat) < 0.");
                    continue;
                }

                // Chỉ hiển thị khi còn tồn.
                if (chieuDaiCon == 0)
                    continue;

                // Tìm theo chiều dài ở nhóm chiều dài dùng chiều dài CÒN LẠI.
                if (criteria.SearchType == CatDay_SearchType.ChieuDai
                    && criteria.ChieuDaiToiThieu.HasValue
                    && chieuDaiCon < criteria.ChieuDaiToiThieu.Value)
                {
                    continue;
                }

                int heSo = soDau < soCuoi ? 1 : -1;
                long soCuoiHienThi = soCuoi - (heSo * tongChieuDaiCat);

                result.Rows.Add(new CatDay_Row
                {
                    TTCuonDay_ID = ttCuonDayId,
                    Lot = lot,
                    TenSP = DbText(reader["TenSP"]),
                    KhachHang = khachHang,
                    Loai = loai,
                    NhomTon = CatDay_InventoryGroup.ChieuDai,
                    SoCuon = 1,
                    SoDau = soDau,
                    SoCuoi = soCuoiHienThi,
                    ChieuDaiConLai = chieuDaiCon,
                    TongChieuDai = chieuDaiCon
                });
            }

            return result;
        }

        private static string XayDungSql(CatDay_SearchCriteria criteria)
        {
            // Lọc trước các điều kiện không phụ thuộc lịch sử để giảm số TTCuonDay
            // cần aggregate. Riêng tìm theo chiều dài phải aggregate trước mới biết tồn.
            var sql = new StringBuilder(@"
                WITH base AS (
                    SELECT
                        cd.id AS TTCuonDay_ID,
                        COALESCE(tp.MaBin, '') AS Lot,
                        nk.TenSP AS TenSP,
                        cd.SoCuon AS SoCuon,
                        cd.SoDau AS SoDau,
                        cd.SoCuoi AS SoCuoi,
                        cd.ChieuDai_1cuon AS ChieuDai_1cuon,
                        cd.Ngay AS Ngay,
                        cd.KhachHang AS KhachHang,
                        cd.KhachHang_KhongDau AS KhachHang_KhongDau,
                        cd.TTLo_ID AS TTLo_ID
                    FROM TTCuonDay cd
                    JOIN TTNhapKhoTP nk
                        ON nk.id = cd.ThongTinNhapKho_ID
                    LEFT JOIN TTThanhPham tp
                        ON tp.id = nk.TTThanhPham_ID
                    WHERE 1 = 1
            ");

            if (!criteria.LayToanBo && criteria.SearchType.HasValue)
            {
                switch (criteria.SearchType.Value)
                {
                    case CatDay_SearchType.Lot:
                        sql.AppendLine("  AND TRIM(COALESCE(tp.MaBin, '')) = TRIM(@searchValue) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.TenSanPham:
                        sql.AppendLine("  AND TRIM(COALESCE(nk.TenSP, '')) = TRIM(@searchValue) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.KhachHang:
                        sql.AppendLine("  AND TRIM(COALESCE(cd.KhachHang_KhongDau, '')) = TRIM(@searchValueKhongDau) COLLATE NOCASE");
                        break;

                    case CatDay_SearchType.ChieuDai:
                        // Phải tính tồn xong mới so được chiều dài.
                        break;
                }
            }

            // Đã chốt: tìm ngày dựa trên TTCuonDay.Ngay.
            if (criteria.NgayBatDau.HasValue)
                sql.AppendLine("  AND date(cd.Ngay) >= date(@ngayBatDau)");

            if (criteria.NgayKetThuc.HasValue)
                sql.AppendLine("  AND date(cd.Ngay) <= date(@ngayKetThuc)");

            sql.Append(@"
                ),
                du_lieu AS (
                    SELECT
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.Ngay,
                        b.KhachHang,
                        b.KhachHang_KhongDau,
                        b.TTLo_ID,
                        COALESCE(SUM(CASE WHEN lscd.ChieuDaiCat IS NULL THEN 0 ELSE lscd.ChieuDaiCat END), 0) AS TongChieuDaiCat,
                        COALESCE(SUM(CASE WHEN lscd.SoLuong IS NULL THEN 0 ELSE lscd.SoLuong END), 0) AS TongSoLuong,
                        MAX(CASE WHEN lscd.ChieuDaiCat < 0 THEN 1 ELSE 0 END) AS CoChieuDaiCatAm,
                        MAX(CASE WHEN lscd.SoLuong < 0 THEN 1 ELSE 0 END) AS CoSoLuongAm
                    FROM base b
                    LEFT JOIN LichSuCatDay lscd
                        ON lscd.TTCuonDay_ID = b.TTCuonDay_ID
                    GROUP BY
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.Ngay,
                        b.KhachHang,
                        b.KhachHang_KhongDau,
                        b.TTLo_ID
                )
                SELECT
                    TTCuonDay_ID,
                    Lot,
                    TenSP,
                    SoCuon,
                    SoDau,
                    SoCuoi,
                    ChieuDai_1cuon,
                    Ngay,
                    KhachHang,
                    KhachHang_KhongDau,
                    TTLo_ID,
                    TongChieuDaiCat,
                    TongSoLuong,
                    CoChieuDaiCatAm,
                    CoSoLuongAm
                FROM du_lieu
                ORDER BY Ngay DESC, TTCuonDay_ID DESC;
            ");

            return sql.ToString();
        }

        private static string XayDungSqlSuggestion(CatDay_SearchType searchType)
        {
            string dieuKienKeyword;
            string cotGiaTri;

            switch (searchType)
            {
                case CatDay_SearchType.Lot:
                    dieuKienKeyword = @"
                        AND tp.MaBin IS NOT NULL
                        AND TRIM(tp.MaBin) <> ''
                        AND tp.MaBin LIKE @kw ESCAPE '\'";
                    cotGiaTri = "Lot";
                    break;

                case CatDay_SearchType.TenSanPham:
                    dieuKienKeyword = @"
                        AND nk.TenSP IS NOT NULL
                        AND TRIM(nk.TenSP) <> ''
                        AND nk.TenSP LIKE @kw ESCAPE '\'";
                    cotGiaTri = "TenSP";
                    break;

                case CatDay_SearchType.KhachHang:
                    dieuKienKeyword = @"
                        AND cd.KhachHang IS NOT NULL
                        AND TRIM(cd.KhachHang) <> ''
                        AND COALESCE(cd.KhachHang_KhongDau, '') LIKE @kw ESCAPE '\'";
                    cotGiaTri = "KhachHang";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(searchType));
            }

            var sql = new StringBuilder(@"
                WITH base AS (
                    SELECT
                        cd.id AS TTCuonDay_ID,
                        COALESCE(tp.MaBin, '') AS Lot,
                        nk.TenSP AS TenSP,
                        cd.SoCuon AS SoCuon,
                        cd.SoDau AS SoDau,
                        cd.SoCuoi AS SoCuoi,
                        cd.ChieuDai_1cuon AS ChieuDai_1cuon,
                        cd.KhachHang AS KhachHang
                    FROM TTCuonDay cd
                    JOIN TTNhapKhoTP nk
                        ON nk.id = cd.ThongTinNhapKho_ID
                    LEFT JOIN TTThanhPham tp
                        ON tp.id = nk.TTThanhPham_ID
                    WHERE 1 = 1
            ");

            sql.AppendLine(dieuKienKeyword);
            sql.Append(@"
                ),
                du_lieu AS (
                    SELECT
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.KhachHang,
                        COALESCE(SUM(CASE WHEN lscd.ChieuDaiCat IS NULL THEN 0 ELSE lscd.ChieuDaiCat END), 0) AS TongChieuDaiCat,
                        COALESCE(SUM(CASE WHEN lscd.SoLuong IS NULL THEN 0 ELSE lscd.SoLuong END), 0) AS TongSoLuong,
                        MAX(CASE WHEN lscd.ChieuDaiCat < 0 THEN 1 ELSE 0 END) AS CoChieuDaiCatAm,
                        MAX(CASE WHEN lscd.SoLuong < 0 THEN 1 ELSE 0 END) AS CoSoLuongAm
                    FROM base b
                    LEFT JOIN LichSuCatDay lscd
                        ON lscd.TTCuonDay_ID = b.TTCuonDay_ID
                    GROUP BY
                        b.TTCuonDay_ID,
                        b.Lot,
                        b.TenSP,
                        b.SoCuon,
                        b.SoDau,
                        b.SoCuoi,
                        b.ChieuDai_1cuon,
                        b.KhachHang
                ),
                con_ton AS (
                    SELECT *
                    FROM du_lieu
                    WHERE
                        (
                            SoDau IS NULL
                            AND SoCuoi IS NULL
                            AND SoCuon IS NOT NULL
                            AND SoCuon > 0
                            AND CoSoLuongAm = 0
                            AND (CAST(SoCuon AS INTEGER) - TongSoLuong) > 0
                        )
                        OR
                        (
                            SoDau IS NOT NULL
                            AND SoCuoi IS NOT NULL
                            AND SoCuon = 1
                            AND SoDau <> SoCuoi
                            AND CoChieuDaiCatAm = 0
                            AND (ABS(CAST(SoCuoi AS INTEGER) - CAST(SoDau AS INTEGER)) - TongChieuDaiCat) > 0
                        )
                )
                SELECT DISTINCT TRIM(");

            sql.Append(cotGiaTri);
            sql.Append(@") AS GiaTri
                FROM con_ton
                WHERE ");
            sql.Append(cotGiaTri);
            sql.Append(@" IS NOT NULL
                  AND TRIM(");
            sql.Append(cotGiaTri);
            sql.Append(@") <> ''
                ORDER BY GiaTri COLLATE NOCASE
                LIMIT @limit;");

            return sql.ToString();
        }

        private static void GanThamSo(SQLiteCommand cmd, CatDay_SearchCriteria criteria)
        {
            if (!criteria.LayToanBo && criteria.SearchType.HasValue)
            {
                switch (criteria.SearchType.Value)
                {
                    case CatDay_SearchType.Lot:
                    case CatDay_SearchType.TenSanPham:
                        cmd.Parameters.AddWithValue("@searchValue", criteria.SearchValue?.Trim() ?? string.Empty);
                        break;

                    case CatDay_SearchType.KhachHang:
                        cmd.Parameters.AddWithValue(
                            "@searchValueKhongDau",
                            ChuyenKhongDau(criteria.SearchValue?.Trim() ?? string.Empty));
                        break;

                    case CatDay_SearchType.ChieuDai:
                        // Chiều dài được so sau khi đã tính tồn trong C#.
                        break;
                }
            }

            if (criteria.NgayBatDau.HasValue)
                cmd.Parameters.AddWithValue(
                    "@ngayBatDau",
                    criteria.NgayBatDau.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (criteria.NgayKetThuc.HasValue)
                cmd.Parameters.AddWithValue(
                    "@ngayKetThuc",
                    criteria.NgayKetThuc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static void ThemDataIssue(
            CatDay_SearchResult result,
            long ttCuonDayId,
            string lot,
            long? rawRemaining,
            string noiDung)
        {
            result.DataIssues.Add(new CatDay_DataIssue
            {
                TTCuonDay_ID = ttCuonDayId,
                Lot = lot,
                RawRemaining = rawRemaining,
                NoiDung = noiDung ?? string.Empty
            });
        }

        private static DataTable TaoBangSuggestionRong()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("GiaTri", typeof(string));
            return dt;
        }

        private static long? DbInt64Nullable(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static string DbText(object value)
        {
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Tạo pattern contains nhưng coi %, _ và \ là ký tự thường của keyword.
        /// </summary>
        private static string TaoLikeContains(string keyword)
        {
            keyword ??= string.Empty;
            return "%" + keyword
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_") + "%";
        }

        private static string ChuyenKhongDau(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                if (c == 'đ')
                    sb.Append('d');
                else if (c == 'Đ')
                    sb.Append('D');
                else
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
