using System;

namespace DG_TonKhoBTP_v02.Models.KeToan
{
    public class CapNhatSP_Model
    {
        public int? Id { get; set; }
        public string Ma { get; set; }
        public string Ten { get; set; }
        public string Ten_KhongDau { get; set; }
        public string DonVi { get; set; }
        public string KieuSP { get; set; }
        public decimal ChuyenDoi { get; set; } = 1;
        public bool Active { get; set; } = true;
        public DateTime? DateInsert { get; set; }

        public class NhaCungCap_Model
        {
            public int? Id { get; set; }
            public string Ma { get; set; }
            public string TenNCC { get; set; }
            public string TenNCC_KhongDau { get; set; }
            public string DiaChi { get; set; }
        }

        public class Kho_Model
        {
            public int? Id { get; set; }
            public string KiHieu { get; set; }
            public string TenKho { get; set; }
            public string TenKho_KhongDau { get; set; }
            public string GhiChu { get; set; }
        }
    }
}
