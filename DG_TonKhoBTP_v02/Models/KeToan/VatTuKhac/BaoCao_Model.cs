using System;

namespace DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac
{
    internal class BaoCao_Model
    {
        internal class CanEdit
        {
            public int Id { get; set; }
            public int Value { get; set; }
        }

        internal class ThongTinDatHangUpdate
        {
            public int Id { get; set; }
            public string TenVatTu { get; set; }
            public decimal SoLuongMua { get; set; }
            public decimal DonGia { get; set; }
            public string MucDichMua { get; set; }
            public string NgayGiao { get; set; }
            public string GhiChu { get; set; }
        }

        internal class LichSuXuatNhapUpdate
        {
            public int Id { get; set; }
            public decimal SoLuong { get; set; }
            public string NguoiGiaoNhan { get; set; }
            public int? DanhSachKhoId { get; set; }
            public string LyDo { get; set; }
            public string Ngay { get; set; }
            public string TenPhieu { get; set; }
            public string GhiChu { get; set; }
        }
    }
}
