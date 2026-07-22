using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models
{
    public class BomComponentData
    {
        public int ComponentId { get; set; }

        // Mã và kiểu sản phẩm của component trong DanhSachMaSP.
        // Hai giá trị này chỉ dùng để xác định component bắt buộc khi lưu.
        public string ComponentMa { get; set; } = string.Empty;

        public string ComponentKieuSP { get; set; } = string.Empty;

        // true khi component KieuSP = "NVL" được cấu hình bắt buộc
        // trong bảng DanhSachNVLBatBuoc.
        public bool LaNVLBatBuoc { get; set; }

        public decimal TyLe { get; set; } = 1m;

        public decimal TyLeHoanDoi { get; set; } = 1m;
    }

    public static class BomDataTableProperties
    {
        public const string Loaded = "BomComponentsLoaded";
        public const string Components = "BomComponents";
    }

    public class ThanhPhamData
    {
        public int DanhSachSPId { get; set; }

        public string MaTP { get; set; } = string.Empty;

        public string TenTP { get; set; } = string.Empty;

        public string DonVi { get; set; } = string.Empty;

        public decimal KhoiLuong { get; set; }

        public decimal ChieuDai { get; set; }

        public decimal ChuyenDoi { get; set; } = 1m;

        public decimal Phe { get; set; }

        public string GhiChu { get; set; } = string.Empty;

        public string SoLOT { get; set; } = string.Empty;

        public string TenMay { get; set; } = string.Empty;

        // null = thành phẩm đã tải thành công nhưng không có BOM active.
        public List<BomComponentData> BomComponents { get; set; }
    }
}