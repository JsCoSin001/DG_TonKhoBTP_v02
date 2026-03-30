namespace DG_TonKhoBTP_v02.Models
{
    public class ThongTinDatHangUpdateModel
    {
        public int Id { get; set; }
        public string TenVatTu { get; set; }
        public decimal SoLuongMua { get; set; }
        public decimal DonGia { get; set; }
        public string MucDichMua { get; set; }
        public string NgayGiao { get; set; }
        public string GhiChu { get; set; }
    }

    public class LichSuXuatNhapUpdateModel
    {
        public int Id { get; set; }
        public decimal SoLuong { get; set; }
        public string NguoiGiaoNhan { get; set; }
        public string Kho { get; set; }
        public string LyDo { get; set; }
        public string Ngay { get; set; }
        public string TenPhieu { get; set; }
        public string GhiChu { get; set; }
    }
}