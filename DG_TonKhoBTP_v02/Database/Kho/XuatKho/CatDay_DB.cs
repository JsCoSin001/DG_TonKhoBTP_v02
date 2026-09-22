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
        /// - LOT: tìm contains theo TTThanhPham.MaBin.
        /// - Tên sản phẩm: tìm contains theo TTNhapKhoTP.TenSP.
        /// - Khách hàng: tìm contains theo TTCuonDay.KhachHang_KhongDau,
        ///   nhưng trả về KhachHang có dấu để hiển thị.
        /// Kết quả luôn DISTINCT và chỉ lấy giá trị thực sự có TTCuonDay.
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

                string sql;
                string keywordForQuery = keyword;

                switch (searchType)
                {
                    case CatDay_SearchType.Lot:
                        sql = @"
                            SELECT DISTINCT TRIM(tp.MaBin) AS GiaTri
                            FROM TTCuonDay cd
                            JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                            JOIN TTThanhPham tp ON tp.id = nk.TTThanhPham_ID
                            WHERE tp.MaBin IS NOT NULL
                              AND TRIM(tp.MaBin) <> ''
                              AND tp.MaBin LIKE @kw ESCAPE '\'
                            ORDER BY GiaTri COLLATE NOCASE
                            LIMIT @limit;";
                        break;

                    case CatDay_SearchType.TenSanPham:
                        sql = @"
                            SELECT DISTINCT TRIM(nk.TenSP) AS GiaTri
                            FROM TTCuonDay cd
                            JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                            WHERE nk.TenSP IS NOT NULL
                              AND TRIM(nk.TenSP) <> ''
                              AND nk.TenSP LIKE @kw ESCAPE '\'
                            ORDER BY GiaTri COLLATE NOCASE
                            LIMIT @limit;";
                        break;

                    case CatDay_SearchType.KhachHang:
                        keywordForQuery = ChuyenKhongDau(keyword);
                        sql = @"
                            SELECT DISTINCT TRIM(cd.KhachHang) AS GiaTri
                            FROM TTCuonDay cd
                            JOIN TTNhapKhoTP nk ON nk.id = cd.ThongTinNhapKho_ID
                            WHERE cd.KhachHang IS NOT NULL
                              AND TRIM(cd.KhachHang) <> ''
                              AND COALESCE(cd.KhachHang_KhongDau, '') LIKE @kw ESCAPE '\'
                            ORDER BY GiaTri COLLATE NOCASE
                            LIMIT @limit;";
                        break;

                    case CatDay_SearchType.ChieuDai:
                    default:
                        return TaoBangSuggestionRong();
                }

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
        /// Tìm theo criteria đã được UC xác nhận.
        /// RawRemaining âm luôn bị loại khỏi Rows nhưng được trả về DataIssues để UI cảnh báo.
        /// RawRemaining = 0 vẫn hợp lệ đối với tìm LOT/Tên SP/Khách hàng/chỉ theo ngày.
        /// </summary>
        public static CatDay_SearchResult TimKiem(CatDay_SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.LayToanBo = false;
            return ThucThiTimKiem(criteria);
        }

        /// <summary>
        /// Lấy toàn bộ TTCuonDay còn chiều dài khả dụng.
        /// Chỉ hiển thị RawRemaining > 0.
        /// RawRemaining âm không hiển thị nhưng vẫn trả về DataIssues để cảnh báo.
        /// RawRemaining = 0 không hiển thị ở chế độ Lấy toàn bộ.
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
                long rawRemaining = Convert.ToInt64(reader["RawRemaining"], CultureInfo.InvariantCulture);

                if (rawRemaining < 0)
                {
                    result.DataIssues.Add(new CatDay_DataIssue
                    {
                        TTCuonDay_ID = ttCuonDayId,
                        Lot = lot,
                        RawRemaining = rawRemaining
                    });
                    continue;
                }

                if (criteria.LayToanBo && rawRemaining <= 0)
                    continue;

                if (criteria.SearchType == CatDay_SearchType.ChieuDai
                    && criteria.ChieuDaiToiThieu.HasValue
                    && rawRemaining < criteria.ChieuDaiToiThieu.Value)
                {
                    continue;
                }

                int soCuon = Convert.ToInt32(reader["SoCuon"], CultureInfo.InvariantCulture);

                result.Rows.Add(new CatDay_Row
                {
                    TTCuonDay_ID = ttCuonDayId,
                    Lot = lot,
                    TenSP = DbText(reader["TenSP"]),
                    SoCuon = soCuon,
                    SoDau = Convert.ToInt32(reader["SoDau"], CultureInfo.InvariantCulture),
                    SoCuoi = Convert.ToInt32(reader["SoCuoi"], CultureInfo.InvariantCulture),
                    ChieuDaiConLai = rawRemaining,
                    TongChieuDai = (long)soCuon * rawRemaining
                });
            }

            return result;
        }

        private static string XayDungSql(CatDay_SearchCriteria criteria)
        {
            // Tối ưu theo 2 tầng:
            // 1) base: lọc trước các điều kiện không phụ thuộc SUM lịch sử
            //    (LOT / Tên SP / Khách hàng / Ngày).
            // 2) du_lieu: chỉ aggregate LichSuCatDay cho tập TTCuonDay đã được thu hẹp.
            // Điều kiện phụ thuộc RawRemaining được áp dụng sau bước aggregate.
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
                        cd.KhachHang_KhongDau AS KhachHang_KhongDau
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
                        // Chưa thể lọc theo chiều dài còn lại ở tầng base vì cần SUM lịch sử cắt.
                        break;
                }
            }

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
                        b.Ngay,
                        b.KhachHang,
                        b.KhachHang_KhongDau,
                        CAST(b.ChieuDai_1cuon AS INTEGER)
                            - COALESCE(SUM(COALESCE(lscd.ChieuDaiCat, 0)), 0) AS RawRemaining
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
                        b.KhachHang_KhongDau
                )
                SELECT
                    TTCuonDay_ID,
                    Lot,
                    TenSP,
                    SoCuon,
                    SoDau,
                    SoCuoi,
                    Ngay,
                    RawRemaining
                FROM du_lieu
                WHERE 1 = 1
            ");

            if (criteria.LayToanBo)
            {
                // Lấy toàn bộ: chỉ hiển thị > 0, nhưng vẫn cần lấy số âm để UI cảnh báo.
                // Vì vậy chỉ bỏ đúng trường hợp = 0 tại SQL; số âm sẽ được tách vào DataIssues.
                sql.AppendLine("  AND RawRemaining <> 0");
            }
            else if (criteria.SearchType == CatDay_SearchType.ChieuDai)
            {
                // Tìm chiều dài: kết quả hợp lệ phải >= mức yêu cầu.
                // Vẫn lấy số âm để thông báo dữ liệu bất thường cho đúng tập dữ liệu đang tìm.
                sql.AppendLine("  AND (RawRemaining >= @minLength OR RawRemaining < 0)");
            }

            sql.AppendLine("ORDER BY Ngay DESC, TTCuonDay_ID DESC;");
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
                        cmd.Parameters.AddWithValue("@minLength", criteria.ChieuDaiToiThieu.GetValueOrDefault(0));
                        break;
                }
            }

            if (criteria.NgayBatDau.HasValue)
                cmd.Parameters.AddWithValue("@ngayBatDau", criteria.NgayBatDau.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (criteria.NgayKetThuc.HasValue)
                cmd.Parameters.AddWithValue("@ngayKetThuc", criteria.NgayKetThuc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static DataTable TaoBangSuggestionRong()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("GiaTri", typeof(string));
            return dt;
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
