using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.Kho.XuatKho
{
    internal enum CatDay_SearchType
    {
        ChieuDai = 0,
        Lot = 1,
        TenSanPham = 2,
        KhachHang = 3
    }

    /// <summary>
    /// Cách quản lý tồn của một dòng TTCuonDay.
    /// - SoLuong: SoDau/SoCuoi đều NULL, tồn theo số cuộn.
    /// - ChieuDai: SoDau/SoCuoi đều có giá trị, tồn theo chiều dài.
    /// </summary>
    internal enum CatDay_InventoryGroup
    {
        SoLuong = 0,
        ChieuDai = 1
    }

    internal sealed class CatDay_SearchCriteria
    {
        public CatDay_SearchType? SearchType { get; set; }
        public string SearchValue { get; set; } = string.Empty;
        public int? ChieuDaiToiThieu { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public bool LayToanBo { get; set; }
    }

    internal sealed class CatDay_Row
    {
        public long TTCuonDay_ID { get; set; }
        public string Lot { get; set; } = string.Empty;
        public string TenSP { get; set; } = string.Empty;
        public string KhachHang { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public CatDay_InventoryGroup NhomTon { get; set; }

        /// <summary>
        /// Số cuộn CÒN TỒN để hiển thị.
        /// Nhóm số lượng: TTCuonDay.SoCuon - SUM(LichSuCatDay.SoLuong).
        /// Nhóm chiều dài: luôn bằng 1 (và TTCuonDay.SoCuon phải bằng 1).
        /// </summary>
        public int SoCuon { get; set; }

        /// <summary>
        /// Nhóm số lượng: NULL.
        /// Nhóm chiều dài: giữ nguyên TTCuonDay.SoDau.
        /// </summary>
        public long? SoDau { get; set; }

        /// <summary>
        /// Nhóm số lượng: NULL.
        /// Nhóm chiều dài:
        /// TTCuonDay.SoCuoi - HeSo * SUM(LichSuCatDay.ChieuDaiCat),
        /// HeSo = +1 khi SoDau < SoCuoi, -1 khi SoDau > SoCuoi.
        /// </summary>
        public long? SoCuoi { get; set; }

        /// <summary>
        /// Giá trị cột "CD 1 đơn vị".
        /// Nhóm số lượng: TTCuonDay.ChieuDai_1cuon.
        /// Nhóm chiều dài: ABS(SoCuoi gốc - SoDau gốc) - SUM(ChieuDaiCat).
        /// </summary>
        public long ChieuDaiConLai { get; set; }

        /// <summary>
        /// Tổng chiều dài còn = SoCuon hiển thị * ChieuDaiConLai.
        /// </summary>
        public long TongChieuDai { get; set; }
    }

    internal sealed class CatDay_DataIssue
    {
        public long TTCuonDay_ID { get; set; }
        public string Lot { get; set; } = string.Empty;
        public long? RawRemaining { get; set; }
        public string NoiDung { get; set; } = string.Empty;
    }

    internal sealed class CatDay_SearchResult
    {
        public List<CatDay_Row> Rows { get; } = new List<CatDay_Row>();
        public List<CatDay_DataIssue> DataIssues { get; } = new List<CatDay_DataIssue>();
    }
}
