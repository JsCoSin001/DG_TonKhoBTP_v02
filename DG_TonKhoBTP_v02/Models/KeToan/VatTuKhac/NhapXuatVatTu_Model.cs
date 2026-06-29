using System;

namespace DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac
{
    public class KieuNhapXuat_Model
    {
        public int Id { get; set; }
        public string Ten { get; set; }
        public bool IsNhap { get; set; }
        public bool IsDichVu { get; set; }
        public bool IsKhac { get; set; }
    }

    public static class TenKieuNhapXuat
    {
        public const string NHAP_KHO_THEO_DON = "NHẬP KHO THEO ĐƠN ĐỀ NGHỊ";
        public const string NHAP_KHO_KHAC = "NHẬP KHÁC";
        public const string XUAT_THEO_DON = "XUẤT THEO ĐƠN ĐỀ NGHỊ";
        public const string XUAT_KHAC = "XUẤT KHÁC";
        public const string DICH_VU = "XÁC NHẬN DỊCH VỤ";
    }

    public class NhapXuatVatTu_SaveModel
    {
        public KieuNhapXuat_Model Mode { get; set; }

        public int? LichSuXuatNhapId { get; set; }
        public string OldTenPhieu { get; set; }
        public int? OldDanhSachMaSPId { get; set; }
        public int? OldDanhSachKhoId { get; set; }

        public int? ThongTinDatHangId { get; set; }
        public int? DanhSachDatHangId { get; set; }
        public int? DanhSachMaSPId { get; set; }
        public int? DanhSachKhoId { get; set; }
        public int? DanhSachNCCId { get; set; }

        public DateTime Ngay { get; set; }
        public string NguoiGiaoNhan { get; set; }
        public string LyDo { get; set; }
        public decimal SoLuongNguoiNhap { get; set; }
        public string TenPhieu { get; set; }
        public string GhiChu { get; set; }
        public string NhaCC { get; set; }
        public decimal DonGia { get; set; }
        public string NguoiLam { get; set; }
    }

    public class NhapXuatVatTu_SaveResult
    {
        public string TenPhieu { get; set; }
        public decimal TongSoLuong { get; set; }
    }

    public class NhapXuatVatTu_DeleteModel
    {
        public KieuNhapXuat_Model Mode { get; set; }

        public int? LichSuXuatNhapId { get; set; }
        public string TenPhieu { get; set; }
        public int? DanhSachMaSPId { get; set; }
        public int? DanhSachKhoId { get; set; }
        public int? ThongTinDatHangId { get; set; }
        public int? DanhSachDatHangId { get; set; }
        public string NguoiLam { get; set; }
    }

    public class NhapXuatVatTu_DeleteResult
    {
        public int DeletedRows { get; set; }
        public string TenPhieu { get; set; }
    }

}
