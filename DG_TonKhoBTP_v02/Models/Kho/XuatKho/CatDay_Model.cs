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
        public int SoCuon { get; set; }
        public int SoDau { get; set; }
        public int SoCuoi { get; set; }

        /// <summary>
        /// Chiều dài còn lại của MỘT cuộn/lô:
        /// TTCuonDay.ChieuDai_1cuon - SUM(LichSuCatDay.ChieuDaiCat theo TTCuonDay_ID).
        /// </summary>
        public long ChieuDaiConLai { get; set; }

        /// <summary>
        /// Tổng chiều dài hiện còn của dòng TTCuonDay = SoCuon * ChieuDaiConLai.
        /// Không dùng giá trị này để quyết định một cuộn có đủ chiều dài để cắt hay không.
        /// </summary>
        public long TongChieuDai { get; set; }
    }

    internal sealed class CatDay_DataIssue
    {
        public long TTCuonDay_ID { get; set; }
        public string Lot { get; set; } = string.Empty;
        public long RawRemaining { get; set; }
    }

    internal sealed class CatDay_SearchResult
    {
        public List<CatDay_Row> Rows { get; } = new List<CatDay_Row>();
        public List<CatDay_DataIssue> DataIssues { get; } = new List<CatDay_DataIssue>();
    }
}
