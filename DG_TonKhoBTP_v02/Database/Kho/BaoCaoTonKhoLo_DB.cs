using DG_TonKhoBTP_v02.Models.Kho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    public static class BaoCaoTonKhoLo_DB
    {
        private const string NhomKhac = "Khác";

        /// <summary>
        /// Lấy dữ liệu tồn kho hiện tại cho báo cáo loại Lô.
        ///
        /// Phạm vi:
        /// - TTNhapKho.Loai = 'Lô'
        /// - TTNhapKho.LoaiDon IN ('Hàng bán', 'Hàng đặt')
        /// - Lấy cả Kieu = 1 và Kieu = 0
        /// - Cả Kieu = 1 và Kieu = 0 đều tính tồn thực tế theo TTXuatKho.
        ///
        /// Công thức tồn Lô:
        /// - SoCuoiTon = COALESCE(MIN(TTXuatKho.SoDau), TTCuonDay.SoCuoi)
        /// - SoM       = SoCuoiTon - TTCuonDay.SoDau
        /// - Chỉ lấy nếu SoM > 0
        /// </summary>
        public static TonKhoLoReport LayBaoCaoTonKhoLo()
        {
            DataTable dt = QueryTonKhoLo();

            var rawRows = new List<RawTonKhoLoRow>();
            var invalidMaBins = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                string tenSanPham = GetString(row, "TenSanPham");
                string maBin = GetString(row, "MaBin");
                string[] parts = CatMaBin(maBin);

                if (parts.Length < 3)
                {
                    invalidMaBins.Add($"- LOT: {maBin}; Sản phẩm: {tenSanPham}");
                    continue;
                }

                string tenNhom = GetString(row, "TenNhom");
                if (string.IsNullOrWhiteSpace(tenNhom))
                    tenNhom = NhomKhac;

                int kieu = GetInt(row, "Kieu");
                string loaiDon = GetString(row, "LoaiDon");
                string khsx = parts[1];
                string soThuTuBin = parts[3];
                string mauSac = GetString(row, "MauSac");

                var lot = new LotCode
                {
                    SoM = FormatNumber(GetDouble(row, "SoM")),
                    ChieuCaoLo = FormatNumber(GetDouble(row, "ChieuCaoLo")),
                    SoDau = FormatNumber(GetDouble(row, "SoDauTon")),
                    SoCuoi = FormatNumber(GetDouble(row, "SoCuoiTon")),
                    SoThuTuBin = soThuTuBin,
                    TenKhach = GetString(row, "TenKhach"),
                    MauSac = mauSac,
                    KHSX = khsx
                };

                rawRows.Add(new RawTonKhoLoRow
                {
                    TenNhom = tenNhom,
                    TenSanPham = tenSanPham,
                    LoaiDon = loaiDon,
                    Kieu = kieu,
                    KHSX = khsx,
                    MauSac = mauSac,
                    SoThuTuBin = soThuTuBin,
                    SoDau = GetDouble(row, "SoDauTon"),
                    Lot = lot
                });
            }

            if (invalidMaBins.Count > 0)
            {
                string detail = string.Join(Environment.NewLine, invalidMaBins.Take(20));
                if (invalidMaBins.Count > 20)
                    detail += Environment.NewLine + $"... và {invalidMaBins.Count - 20} LOT khác.";

                throw new InvalidOperationException(
                    "Mã LOT không đúng định dạng, không thể tạo báo cáo." + Environment.NewLine + Environment.NewLine +
                    detail + Environment.NewLine + Environment.NewLine +
                    "Yêu cầu mã LOT phải đủ phần để lấy KHSX và Số thứ tự Bin." + Environment.NewLine +
                    "Ví dụ hợp lệ: E10-261743/7-01");
            }

            return BuildReport(rawRows);
        }

        private static DataTable QueryTonKhoLo()
        {
            const string sql = @"
                WITH ton AS (
                    SELECT  COALESCE(NULLIF(TRIM(bs.TenChiTiet), ''), 'Khác') AS TenNhom,
                            sp.Ten       AS TenSanPham,
                            nk.LoaiDon   AS LoaiDon,
                            nk.Kieu      AS Kieu,
                            nk.ChieuCaoLo AS ChieuCaoLo,
                            nk.KhachHang AS TenKhach,
                            COALESCE(bs.M, '') AS MauSac,
                            tp.MaBin     AS MaBin,
                            cd.SoDau     AS SoDauTon,
                            COALESCE(MIN(xk.SoDau), cd.SoCuoi) AS SoCuoiTon
                    FROM    TTNhapKho      nk
                    JOIN    TTThanhPham    tp ON tp.id = nk.TTThanhPham_ID
                    JOIN    DanhSachMaSP   sp ON sp.id = tp.DanhSachSP_ID
                    JOIN    TTCuonDay      cd ON cd.ThongTinNhapKho_ID = nk.id
                    LEFT JOIN TTBoSung     bs ON bs.DanhSachMaSP_ID = sp.id
                    LEFT JOIN TTXuatKho    xk ON xk.TTCuonDay_ID = cd.id
                    WHERE   nk.Loai = 'Lô'
                      AND   nk.LoaiDon IN ('Hàng bán', 'Hàng đặt')
                      AND   nk.Kieu IN (0, 1)
                    GROUP BY nk.id, cd.id
                )
                SELECT  TenNhom,
                        TenSanPham,
                        LoaiDon,
                        Kieu,
                        ChieuCaoLo,
                        TenKhach,
                        MauSac,
                        MaBin,
                        SoDauTon,
                        SoCuoiTon,
                        SoCuoiTon - SoDauTon AS SoM
                FROM    ton
                WHERE   SoCuoiTon > SoDauTon
                ORDER BY
                        CASE WHEN TenNhom = 'Khác' THEN 1 ELSE 0 END,
                        TenNhom,
                        TenSanPham,
                        MaBin,
                        Kieu";

            var dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        private static TonKhoLoReport BuildReport(List<RawTonKhoLoRow> rows)
        {
            var report = new TonKhoLoReport
            {
                CreatedAt = DateTime.Now
            };

            var groupKeys = rows
                .Select(r => r.TenNhom)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(g => string.Equals(g, NhomKhac, StringComparison.CurrentCultureIgnoreCase) ? 1 : 0)
                .ThenBy(g => g)
                .ToList();

            foreach (string tenNhom in groupKeys)
            {
                var groupRows = rows
                    .Where(r => string.Equals(r.TenNhom, tenNhom, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();

                var reportGroup = new TonKhoLoReportGroup
                {
                    TenNhom = tenNhom
                };

                var productNames = groupRows
                    .Select(r => r.TenSanPham)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(name => name)
                    .ToList();

                foreach (string productName in productNames)
                {
                    var productRows = groupRows
                        .Where(r => string.Equals(r.TenSanPham, productName, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();

                    var product = new TonKhoLoProductRow
                    {
                        TenSanPham = productName,
                        HangBan = BuildCell(productRows, "Hàng bán"),
                        HangDat = BuildCell(productRows, "Hàng đặt")
                    };

                    // Không hiển thị sản phẩm nếu không có hàng bán và không có hàng đặt.
                    if (product.HasData)
                        reportGroup.Rows.Add(product);
                }

                if (reportGroup.Rows.Count > 0)
                    report.Groups.Add(reportGroup);
            }

            return report;
        }

        private static TonKhoLoCell BuildCell(List<RawTonKhoLoRow> productRows, string loaiDon)
        {
            var cellRows = productRows
                .Where(r => string.Equals(r.LoaiDon, loaiDon, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new TonKhoLoCell
            {
                LoGroups = BuildLotGroups(cellRows.Where(r => r.Kieu == 1)),
                LeGroups = BuildLotGroups(cellRows.Where(r => r.Kieu == 0))
            };
        }

        private static List<TonKhoLotCodeGroup> BuildLotGroups(IEnumerable<RawTonKhoLoRow> rows)
        {
            return rows
                .GroupBy(r => new { r.KHSX, r.MauSac })
                .OrderBy(g => g.Key.KHSX)
                .ThenBy(g => g.Key.MauSac)
                .Select(g => new TonKhoLotCodeGroup
                {
                    KHSX = g.Key.KHSX,
                    MauSac = g.Key.MauSac,
                    Lots = g
                        .OrderBy(r => ParseSortableInt(r.SoThuTuBin))
                        .ThenBy(r => r.SoThuTuBin)
                        .ThenBy(r => r.SoDau)
                        .Select(r => r.Lot)
                        .ToList()
                })
                .Where(g => g.Lots.Count > 0)
                .ToList();
        }

        public static string[] CatMaBin(string input)
        {
            char[] separators = { '-', '/' };
            return (input ?? string.Empty)
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToArray();
        }

        private static int ParseSortableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                ? n
                : int.MaxValue;
        }

        private static string FormatNumber(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.000001)
                return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string GetString(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return string.Empty;

            return row[column]?.ToString()?.Trim() ?? string.Empty;
        }

        private static int GetInt(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0;

            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        private static double GetDouble(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0d;

            return Convert.ToDouble(row[column], CultureInfo.InvariantCulture);
        }

        private class RawTonKhoLoRow
        {
            public string TenNhom { get; set; } = string.Empty;
            public string TenSanPham { get; set; } = string.Empty;
            public string LoaiDon { get; set; } = string.Empty;
            public int Kieu { get; set; }
            public string KHSX { get; set; } = string.Empty;
            public string MauSac { get; set; } = string.Empty;
            public string SoThuTuBin { get; set; } = string.Empty;
            public double SoDau { get; set; }
            public LotCode Lot { get; set; } = new LotCode();
        }
    }
}
