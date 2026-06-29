using System;

namespace DG_TonKhoBTP_v02.Models.KeToan
{
    public class KiemKe
    {
        public long? id { get; set; }
        public long? TTThanhPham_ID { get; set; }
        public long? DanhSachSP_ID { get; set; }
        public string MaBin { get; set; }
        public decimal? ChieuDai { get; set; }
        public decimal? KhoiLuong { get; set; }
        public string GhiChu { get; set; }
        public string ThoiGianKiemKe { get; set; }
        public string ApprovedDate { get; set; }
        public string DateInsert { get; set; }
        public string Ten { get; set; }
        public string NguoiKK { get; set; }
    }

    public class DanhSachBin
    {
        public string TenBin { get; set; }
        public decimal? KhoiLuongBin { get; set; }
    }
}
