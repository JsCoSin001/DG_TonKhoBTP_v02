namespace DG_TonKhoBTP_v02.Models
{
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
    }
}