using System;

namespace DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac
{
    internal class DanhSachDatHangModel
    {
        public int Id { get; set; }
        public string MaDon { get; set; }
        public int LoaiDon { get; set; }
        public DateTime DateInsert { get; set; }
        public string NguoiDat { get; set; }
        public DateTime NgayThem { get; set; }
    }

    internal class ThongTinDatHangModel
    {
        public int Id { get; set; }
        public int? DanhSachMaSP_ID { get; set; }
        public int DanhSachDatHang_ID { get; set; }
        public string TenVatTu { get; set; }
        public string TenVatTu_KhongDau { get; set; }
        public decimal SoLuongMua { get; set; }
        public string MucDichMua { get; set; }
        public DateTime? NgayGiao { get; set; }
        public DateTime Date_Insert { get; set; }
        public decimal? DonGia { get; set; }
    }

    internal class MuaVatTuGridRowModel
    {
        public int ThongTinDatHangId { get; set; }
        public int DanhSachDatHangId { get; set; }
        public string MaDon { get; set; }
        public DateTime? NgayThem { get; set; }
        public int? DanhSachMaSPId { get; set; }
        public string MaVatTu { get; set; }
        public string TenVatTu { get; set; }
        public string DonVi { get; set; }
        public decimal SoLuongMua { get; set; }
        public string MucDichMua { get; set; }
        public DateTime? NgayGiao { get; set; }
        public string TenVatTuKhongDau { get; set; }
        public decimal? DonGia { get; set; }
        public decimal? SLTon { get; set; }
    }

    internal class DeleteDatHangResult
    {
        public bool DeletedDetail { get; set; }
        public bool DeletedHeader { get; set; }
        public int DanhSachDatHangId { get; set; }
        public string MaDon { get; set; }
    }
}
