using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Models.Kho
{
    /// <summary>
    /// Dữ liệu báo cáo tồn kho loại Lô, đã được group theo TenChiTiet -> sản phẩm.
    /// </summary>
    public class TonKhoLoReport
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<TonKhoLoReportGroup> Groups { get; set; } = new List<TonKhoLoReportGroup>();

        public bool HasData => Groups.Any(g => g.Rows.Any(r => r.HasData));
    }

    /// <summary>
    /// Một nhóm sản phẩm theo TTBoSung.TenChiTiet.
    /// Nếu TenChiTiet rỗng/null thì dùng tên nhóm "Khác".
    /// </summary>
    public class TonKhoLoReportGroup
    {
        public string TenNhom { get; set; } = string.Empty;
        public List<TonKhoLoProductRow> Rows { get; set; } = new List<TonKhoLoProductRow>();
    }

    /// <summary>
    /// Một dòng trong bảng báo cáo, tương ứng 1 tên sản phẩm.
    /// </summary>
    public class TonKhoLoProductRow
    {
        public string TenSanPham { get; set; } = string.Empty;
        public TonKhoLoCell HangBan { get; set; } = new TonKhoLoCell();
        public TonKhoLoCell HangDat { get; set; } = new TonKhoLoCell();

        public bool HasData => HangBan.HasData || HangDat.HasData;
    }

    /// <summary>
    /// Nội dung của 1 ô Hàng bán hoặc Hàng đặt + Hàng gửi.
    /// Luôn render đủ nhãn Lô và Lẻ; nếu list rỗng thì chỉ để trống bên dưới nhãn.
    /// </summary>
    public class TonKhoLoCell
    {
        /// <summary>TTNhapKho.Kieu = 1, hiển thị dưới nhãn "Lô:".</summary>
        public List<TonKhoLotCodeGroup> LoGroups { get; set; } = new List<TonKhoLotCodeGroup>();

        /// <summary>TTNhapKho.Kieu = 0, hiển thị dưới nhãn "Lẻ:".</summary>
        public List<TonKhoLotCodeGroup> LeGroups { get; set; } = new List<TonKhoLotCodeGroup>();

        public bool HasData => LoGroups.Any(g => g.Lots.Count > 0)
                            || LeGroups.Any(g => g.Lots.Count > 0);
    }

    /// <summary>
    /// Một nhóm LotCode có cùng KHSX và MauSac.
    /// Writer sẽ render dạng: (lot1 + lot2 + lot3)MauSac/KHSX theo style sup/sub của writer cũ.
    /// </summary>
    public class TonKhoLotCodeGroup
    {
        public string KHSX { get; set; } = string.Empty;
        public string MauSac { get; set; } = string.Empty;
        public List<LotCode> Lots { get; set; } = new List<LotCode>();
    }
}
