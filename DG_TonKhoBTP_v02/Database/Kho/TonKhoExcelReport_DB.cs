using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho
{
    /// <summary>
    /// Lấy dữ liệu tồn kho hiện tại để xuất Excel.
    ///
    /// Sheet Lô:
    /// - TTNhapKho.Loai = 'Lô'
    /// - Lấy cả Kieu = 1 và Kieu = 0, hiển thị thành Lô / Lẻ.
    /// - Tồn thực tế: SoCuoiTon = COALESCE(MIN(TTXuatKho.SoDau), TTCuonDay.SoCuoi).
    /// - Chỉ lấy dòng còn tồn: SoCuoiTon > SoDau.
    ///
    /// Sheet Cuộn:
    /// - TTNhapKho.Loai = 'Cuộn'
    /// - Lấy cả Kieu = 1 và Kieu = 0, hiển thị thành Lô / Lẻ.
    /// - Tồn thực tế: SoCuonTon = TTCuonDay.SoCuon - SUM(TTXuatKho.SoCuon).
    /// - Chỉ lấy dòng còn tồn: SoCuonTon > 0.
    /// - Chiều dài/cuộn lấy trực tiếp từ TTCuonDay.TongChieuDai.
    ///
    /// Cả 2 sheet:
    /// - Tên sản phẩm lấy từ DanhSachMaSP.Ten.
    /// - Nhóm lấy từ TTBoSung.TenChiTiet, rỗng/null thì gom vào Khác.
    /// - Nếu Mã LOT rỗng/null thì báo lỗi và dừng xuất báo cáo.
    /// </summary>
    public static class TonKhoExcelReport_DB
    {
        private const string NhomKhac = "Khác";

        public static DataTable LayTonKhoLo()
        {
            const string sql = @"
                WITH ton AS (
                    SELECT  COALESCE(NULLIF(TRIM(bs.TenChiTiet), ''), 'Khác') AS Nhom,
                            sp.Ten AS TenSanPham,
                            nk.LoaiDon AS LoaiDon,
                            nk.Kieu AS KieuRaw,
                            CASE WHEN COALESCE(nk.Kieu, 1) = 1 THEN 'Lô' ELSE 'Lẻ' END AS KieuHienThi,
                            tp.MaBin AS MaLot,
                            nk.KhachHang AS KhachHang,
                            COALESCE(bs.M, '') AS MauSac,
                            nk.ChieuCaoLo AS ChieuCaoLo,
                            cd.SoDau AS SoDauTon,
                            COALESCE(MIN(xk.SoDau), cd.SoCuoi) AS SoCuoiTon,
                            COALESCE(MIN(xk.SoDau), cd.SoCuoi) - cd.SoDau AS TongChieuDaiTon,
                            cd.GhiChu AS GhiChu
                    FROM    TTNhapKho      nk
                    JOIN    TTThanhPham    tp ON tp.id = nk.TTThanhPham_ID
                    JOIN    DanhSachMaSP   sp ON sp.id = tp.DanhSachSP_ID
                    JOIN    TTCuonDay      cd ON cd.ThongTinNhapKho_ID = nk.id
                    LEFT JOIN TTBoSung     bs ON bs.DanhSachMaSP_ID = sp.id
                    LEFT JOIN TTXuatKho    xk ON xk.TTCuonDay_ID = cd.id
                    WHERE   nk.Loai = 'Lô'
                      AND   nk.LoaiDon IN ('Hàng bán', 'Hàng đặt')
                      AND   COALESCE(nk.Kieu, 1) IN (0, 1)
                    GROUP BY nk.id, cd.id
                )
                SELECT  Nhom AS [Nhóm],
                        TenSanPham AS [Tên sản phẩm],
                        LoaiDon AS [Loại đơn],
                        KieuHienThi AS [Kiểu],
                        MaLot AS [Mã LOT],
                        KhachHang AS [Khách hàng],
                        MauSac AS [Màu sắc],
                        ChieuCaoLo AS [Chiều cao lô],
                        SoDauTon AS [Số đầu tồn],
                        SoCuoiTon AS [Số cuối tồn],
                        TongChieuDaiTon AS [Tổng chiều dài tồn],
                        GhiChu AS [Ghi chú]
                FROM    ton
                WHERE   TongChieuDaiTon > 0
                ORDER BY
                        CASE WHEN Nhom = 'Khác' THEN 1 ELSE 0 END,
                        Nhom,
                        TenSanPham,
                        LoaiDon,
                        KieuRaw DESC,
                        MaLot;";

            DataTable dt = ExecuteDataTable(sql);
            ValidateMaLot(dt, "Lô");
            return dt;
        }

        public static DataTable LayTonKhoCuon()
        {
            const string sql = @"
                WITH ton AS (
                    SELECT  COALESCE(NULLIF(TRIM(bs.TenChiTiet), ''), 'Khác') AS Nhom,
                            sp.Ten AS TenSanPham,
                            nk.LoaiDon AS LoaiDon,
                            nk.Kieu AS KieuRaw,
                            CASE WHEN COALESCE(nk.Kieu, 1) = 1 THEN 'Lô' ELSE 'Lẻ' END AS KieuHienThi,
                            tp.MaBin AS MaLot,
                            nk.KhachHang AS KhachHang,
                            COALESCE(bs.M, '') AS MauSac,
                            COALESCE(cd.SoCuon, 0) AS SoCuonNhap,
                            COALESCE(SUM(COALESCE(xk.SoCuon, 0)), 0) AS SoCuonDaXuat,
                            COALESCE(cd.SoCuon, 0) - COALESCE(SUM(COALESCE(xk.SoCuon, 0)), 0) AS SoCuonTon,
                            cd.SoDau AS SoDau,
                            cd.SoCuoi AS SoCuoi,
                            cd.TongChieuDai AS ChieuDaiCuon,
                            cd.GhiChu AS GhiChu
                    FROM    TTNhapKho      nk
                    JOIN    TTThanhPham    tp ON tp.id = nk.TTThanhPham_ID
                    JOIN    DanhSachMaSP   sp ON sp.id = tp.DanhSachSP_ID
                    JOIN    TTCuonDay      cd ON cd.ThongTinNhapKho_ID = nk.id
                    LEFT JOIN TTBoSung     bs ON bs.DanhSachMaSP_ID = sp.id
                    LEFT JOIN TTXuatKho    xk ON xk.TTCuonDay_ID = cd.id
                    WHERE   nk.Loai = 'Cuộn'
                      AND   nk.LoaiDon IN ('Hàng bán', 'Hàng đặt')
                      AND   COALESCE(nk.Kieu, 1) IN (0, 1)
                    GROUP BY nk.id, cd.id
                )
                SELECT  Nhom AS [Nhóm],
                        TenSanPham AS [Tên sản phẩm],
                        LoaiDon AS [Loại đơn],
                        KieuHienThi AS [Kiểu],
                        MaLot AS [Mã LOT],
                        KhachHang AS [Khách hàng],
                        MauSac AS [Màu sắc],
                        SoCuonNhap AS [Số cuộn nhập],
                        SoCuonDaXuat AS [Số cuộn đã xuất],
                        SoCuonTon AS [Số cuộn tồn],
                        SoDau AS [Số đầu],
                        SoCuoi AS [Số cuối],
                        ChieuDaiCuon AS [Chiều dài/cuộn],
                        GhiChu AS [Ghi chú]
                FROM    ton
                WHERE   SoCuonTon > 0
                ORDER BY
                        CASE WHEN Nhom = 'Khác' THEN 1 ELSE 0 END,
                        Nhom,
                        TenSanPham,
                        LoaiDon,
                        KieuRaw DESC,
                        MaLot;";

            DataTable dt = ExecuteDataTable(sql);
            ValidateMaLot(dt, "Cuộn");
            return dt;
        }

        private static DataTable ExecuteDataTable(string sql)
        {
            var dt = new DataTable();

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);

            return dt;
        }

        private static void ValidateMaLot(DataTable dt, string tenSheet)
        {
            if (dt == null || !dt.Columns.Contains("Mã LOT"))
                return;

            var invalidRows = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                string maLot = Convert.ToString(row["Mã LOT"])?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(maLot))
                    continue;

                string tenSanPham = dt.Columns.Contains("Tên sản phẩm")
                    ? Convert.ToString(row["Tên sản phẩm"])?.Trim() ?? string.Empty
                    : string.Empty;

                string loaiDon = dt.Columns.Contains("Loại đơn")
                    ? Convert.ToString(row["Loại đơn"])?.Trim() ?? string.Empty
                    : string.Empty;

                invalidRows.Add($"- Sheet: {tenSheet}; Sản phẩm: {tenSanPham}; Loại đơn: {loaiDon}");
            }

            if (invalidRows.Count == 0)
                return;

            string detail = string.Join(Environment.NewLine, invalidRows.Take(20));
            if (invalidRows.Count > 20)
                detail += Environment.NewLine + $"... và {invalidRows.Count - 20} dòng khác.";

            throw new InvalidOperationException(
                "Không thể tạo báo cáo tồn kho vì có dòng tồn kho bị thiếu Mã LOT." +
                Environment.NewLine + Environment.NewLine +
                detail +
                Environment.NewLine + Environment.NewLine +
                "Vui lòng kiểm tra lại TTThanhPham.MaBin trước khi xuất báo cáo.");
        }
    }
}
