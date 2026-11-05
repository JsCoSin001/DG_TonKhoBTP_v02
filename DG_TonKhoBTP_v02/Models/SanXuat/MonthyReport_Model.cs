using System;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    /// <summary>Một thành phẩm trong báo cáo tháng (không nhân dòng theo NVL).</summary>
    public sealed class MonthyReport_Model
    {
        public long Id { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? CaKetThuc { get; set; }
        public string? May { get; set; }
        public string? NguoiKetThuc { get; set; }
        public string? MaBin { get; set; }
        public string? TenSanPham { get; set; }
        public string? GhiChu { get; set; }
        public double? KhoiLuongSau { get; set; }
        public double? ChieuDaiSau { get; set; }
        public string? DonVi { get; set; }
        public int? Temp { get; set; }
        public int CongDoanId { get; set; }
    }

    /// <summary>Thông tin NVL đã gắn với một thành phẩm; không đồng nghĩa tồn kho hiện tại.</summary>
    public sealed class MonthyReport_MaterialModel
    {
        public long Id { get; set; }
        public long TTThanhPhamId { get; set; }
        public string? BinNVL { get; set; }
        public string? MaNVL { get; set; }
        public string? TenNVL { get; set; }
        public string? DonVi { get; set; }
        public double? KlBatDau { get; set; }
        public double? KlConLai { get; set; }
        public double? CdBatDau { get; set; }
        public double? CdConLai { get; set; }
        public string? QC { get; set; }
        public double? DuongKinhSoiDong { get; set; }
        public double? SoSoi { get; set; }
        public double? KetCauLoi { get; set; }
        public double? DuongKinhSoiMach { get; set; }
        public double? BanRongBang { get; set; }
        public double? DoDayBang { get; set; }
    }

    public sealed class MonthyReport_ShiftModel
    {
        public long Id { get; set; }
        public long TTThanhPhamId { get; set; }
        public string? May { get; set; }
        public string? Ca { get; set; }
        public string? NguoiLam { get; set; }
        public string? NgayBatDau { get; set; }
        public string? GioBatDau { get; set; }
        public string? NgayKetThuc { get; set; }
        public string? GioKetThuc { get; set; }
        public string? CaKetThuc { get; set; }
        public string? NguoiKetThuc { get; set; }
        public string? ToTruong { get; set; }
        public string? QuanDoc { get; set; }
    }
}
