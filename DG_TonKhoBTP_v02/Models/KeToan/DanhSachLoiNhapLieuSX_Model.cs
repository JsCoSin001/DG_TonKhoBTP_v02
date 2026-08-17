using System;

namespace DG_TonKhoBTP_v02.Models.KeToan
{
    public class DanhSachLoiNhapLieuSX_Model
    {
        public int IdLoi { get; set; }
        public int TTThanhPhamId { get; set; }
        public string LotThanhPham { get; set; }
        public string Ngay { get; set; }
        public string May { get; set; }
        public string Ca { get; set; }
        public string NguoiLam { get; set; }
        public int? CongDoanId { get; set; }
        public string TenCongDoan { get; set; }
        public string TenThanhPham { get; set; }
        public string NoiDungLoi { get; set; }
        public string LyDoLoi { get; set; }
        public bool Confirmed { get; set; }
    }

    public class ChiTietLoiNhapLieuSX_Model
    {
        public int? ComponentId { get; set; }
        public string TenNLBom { get; set; }

        public int? DanhSachMaSPThucTeId { get; set; }
        public string TenNLThucTe { get; set; }
        public string LotThucTe { get; set; }

        public bool CoTrongBom
        {
            get { return ComponentId.HasValue; }
        }

        public bool CoTrongThucTe
        {
            get { return DanhSachMaSPThucTeId.HasValue || !string.IsNullOrWhiteSpace(LotThucTe); }
        }
    }
}
