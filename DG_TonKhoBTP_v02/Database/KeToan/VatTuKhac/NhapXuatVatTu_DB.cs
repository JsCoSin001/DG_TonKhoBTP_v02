using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database
{
    internal static class NhapXuatVatTu_DB
    {
        private const int SearchLimit = 30;

        public static DataTable TimVatTuDichVu(
            KieuNhapXuat_Model model,
            string keyword,
            int? danhSachKhoId,
            string nguoiLam,
            CancellationToken cancellationToken)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptySearchTable();

            if (IsDichVu(model))
                return TimDichVu(keyword, cancellationToken);

            if (IsNhapKhoTheoDon(model))
                return TimVatTuNhapKhoTheoDon(keyword, cancellationToken);

            if (IsNhapKhoKhac(model))
                return TimVatTuNhapKhoKhac(keyword, cancellationToken);

            if (IsXuat(model))
                return TimVatTuXuat(keyword, danhSachKhoId, nguoiLam, cancellationToken);

            return CreateEmptySearchTable();
        }

        public static DataTable TimKhoHang(
            string keyword,
            CancellationToken cancellationToken)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptyKhoHangSearchTable();

            string sql = @"
                SELECT
                    id,
                    TenKho,
                    TenKho_KhongDau
                FROM DanhSachKho
                WHERE
                    TenKho LIKE @kw
                    OR IFNULL(TenKho_KhongDau, '') LIKE @kwNoDau
                ORDER BY TenKho
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        public static DataTable TimNhaCungCap(
            string keyword,
            CancellationToken cancellationToken)
        {
            keyword = keyword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
                return CreateEmptyNhaCungCapSearchTable();

            string sql = @"
                SELECT
                    id,
                    TenNCC,
                    TenNCC_KhongDau
                FROM DanhSachNCC
                WHERE
                    TenNCC LIKE @kw
                    OR IFNULL(TenNCC_KhongDau, '') LIKE @kwNoDau
                ORDER BY TenNCC
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }


        public static int GetSoLuongXuatNhapThangHienTai(bool isNhapKho = false, DateTime? currentDate = null)
        {
            DateTime now = currentDate ?? DateTime.Now;

            string start = new DateTime(now.Year, now.Month, 1)
                .ToString("yyyy-MM-dd");

            string end = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .ToString("yyyy-MM-dd");

            string sql = @"
                SELECT 
                    IFNULL(MAX(CAST(SUBSTR(TenPhieu, INSTR(TenPhieu, '-') + 1) AS INTEGER)), 0)
                FROM LichSuXuatNhap
                WHERE Ngay >= @start 
                AND Ngay < @end
            ";

            sql += isNhapKho ? " AND TenPhieu LIKE 'KNK%'" : " AND TenPhieu LIKE 'KXK%'";

            using var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            object result = cmd.ExecuteScalar();

            return result == null ? 0 : Convert.ToInt32(result);
        }

        private static DataTable TimVatTuNhapKhoTheoDon(
            string keyword,
            CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    dms.id AS id,
                    ttdh.id AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    dsdh.MaDon AS MaDon,
                    (ttdh.SoLuongMua - COALESCE(nhap.TongNhap, 0)) AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    ttdh.DonGia AS DonGia
                FROM ThongTinDatHang ttdh
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                INNER JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN (
                    SELECT
                        ThongTinDatHang_ID,
                        SUM(SoLuong) AS TongNhap
                    FROM LichSuXuatNhap
                    WHERE SoLuong > 0
                    GROUP BY ThongTinDatHang_ID
                ) nhap
                    ON nhap.ThongTinDatHang_ID = ttdh.id
                WHERE dms.Active = 1
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                  AND (ttdh.SoLuongMua - COALESCE(nhap.TongNhap, 0)) > 0
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static DataTable TimVatTuNhapKhoKhac(
            string keyword,
            CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    dms.id AS id,
                    NULL AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    '' AS MaDon,
                    0 AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    NULL AS DonGia
                FROM DanhSachMaSP dms
                WHERE dms.Active = 1
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static DataTable TimVatTuXuat(
            string keyword,
            int? danhSachKhoId,
            string nguoiLam,
            CancellationToken cancellationToken)
        {
            if (!danhSachKhoId.HasValue)
                return CreateEmptySearchTable();

            if (string.IsNullOrWhiteSpace(nguoiLam))
                return CreateEmptySearchTable();

            string sql = @"
                SELECT
                    dms.id AS id,
                    NULL AS ThongTinDatHang_ID,
                    dms.id AS DanhSachMaSP_ID,
                    dms.Ten AS ten,
                    dms.Ma AS ma,
                    dms.DonVi AS donvi,
                    '' AS MaDon,
                    SUM(lsxn.SoLuong) AS SoLuongYeuCau,
                    SUM(lsxn.SoLuong) AS SoLuongTon,
                    NULL AS DonGia
                FROM LichSuXuatNhap lsxn
                INNER JOIN ThongTinDatHang ttdh
                    ON ttdh.id = lsxn.ThongTinDatHang_ID
                INNER JOIN DanhSachMaSP dms
                    ON dms.id = ttdh.DanhSachMaSP_ID
                WHERE dms.Active = 1
                  AND lsxn.DanhSachKho_ID = @danhSachKhoId
                  AND IFNULL(lsxn.NguoiLam, '') = @nguoiLam
                  AND (
                        dms.Ten LIKE @kw
                        OR IFNULL(dms.Ten_KhongDau, '') LIKE @kwNoDau
                  )
                GROUP BY
                    dms.id,
                    dms.Ten,
                    dms.Ma,
                    dms.DonVi
                HAVING SUM(lsxn.SoLuong) > 0
                ORDER BY dms.Ten
                LIMIT @limit;
            ";

            var parameters = CreateKeywordParameters(keyword);
            parameters["@danhSachKhoId"] = danhSachKhoId.Value;
            parameters["@nguoiLam"] = nguoiLam;

            return ExecuteSearchQuery(sql, parameters, cancellationToken);
        }

        private static DataTable TimDichVu(
            string keyword,
            CancellationToken cancellationToken)
        {
            string sql = @"
                SELECT
                    NULL AS id,
                    ttdh.id AS ThongTinDatHang_ID,
                    NULL AS DanhSachMaSP_ID,
                    ttdh.TenVatTu AS ten,
                    '' AS ma,
                    '' AS donvi,
                    dsdh.MaDon AS MaDon,
                    0 AS SoLuongYeuCau,
                    0 AS SoLuongTon,
                    ttdh.DonGia AS DonGia
                FROM ThongTinDatHang ttdh
                INNER JOIN DanhSachDatHang dsdh
                    ON dsdh.id = ttdh.DanhSachDatHang_ID
                LEFT JOIN LichSuXuatNhap lsxn
                    ON lsxn.ThongTinDatHang_ID = ttdh.id
                WHERE lsxn.id IS NULL
                  AND (
                        ttdh.DanhSachMaSP_ID IS NULL
                        OR ttdh.DanhSachMaSP_ID = 0
                  )
                  AND (
                        ttdh.TenVatTu LIKE @kw
                        OR IFNULL(ttdh.TenVatTu_KhongDau, '') LIKE @kwNoDau
                  )
                ORDER BY ttdh.TenVatTu
                LIMIT @limit;
            ";

            return ExecuteSearchQuery(sql, CreateKeywordParameters(keyword), cancellationToken);
        }

        private static Dictionary<string, object> CreateKeywordParameters(string keyword)
        {
            string keywordKhongDau = CoreHelper.BoDauTiengViet(keyword) ?? keyword;

            return new Dictionary<string, object>
            {
                ["@kw"] = "%" + keyword + "%",
                ["@kwNoDau"] = "%" + keywordKhongDau + "%",
                ["@limit"] = SearchLimit
            };
        }

        private static DataTable ExecuteSearchQuery(
            string sql,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dt = new DataTable();

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return dt;
        }

        private static DataTable CreateEmptySearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("ThongTinDatHang_ID", typeof(object));
            dt.Columns.Add("DanhSachMaSP_ID", typeof(object));
            dt.Columns.Add("ten", typeof(string));
            dt.Columns.Add("ma", typeof(string));
            dt.Columns.Add("donvi", typeof(string));
            dt.Columns.Add("MaDon", typeof(string));
            dt.Columns.Add("SoLuongYeuCau", typeof(decimal));
            dt.Columns.Add("SoLuongTon", typeof(decimal));
            dt.Columns.Add("DonGia", typeof(decimal));

            return dt;
        }

        private static DataTable CreateEmptyKhoHangSearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("TenKho", typeof(string));
            dt.Columns.Add("TenKho_KhongDau", typeof(string));

            return dt;
        }

        private static DataTable CreateEmptyNhaCungCapSearchTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("id", typeof(object));
            dt.Columns.Add("TenNCC", typeof(string));
            dt.Columns.Add("TenNCC_KhongDau", typeof(string));

            return dt;
        }

        private static bool IsDichVu(KieuNhapXuat_Model model)
        {
            return model.IsDichVu
                   || string.Equals(model.Ten, TenKieuNhapXuat.DICH_VU, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNhapKhoTheoDon(KieuNhapXuat_Model model)
        {
            return model.IsNhap
                   && !model.IsDichVu
                   && !model.IsKhac;
        }

        private static bool IsNhapKhoKhac(KieuNhapXuat_Model model)
        {
            return model.IsNhap
                   && !model.IsDichVu
                   && model.IsKhac;
        }

        private static bool IsXuat(KieuNhapXuat_Model model)
        {
            return !model.IsNhap
                   && !model.IsDichVu;
        }
    }
}