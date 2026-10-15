using System;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    // Danh mục sử dụng riêng cho UC_BaoCaoDungMay.
    internal sealed class DanhSachCongDoan_Model
    {
        public int Id { get; set; }
        public int MaCongDoan { get; set; }
        public string TenCongDoan { get; set; } = string.Empty;
    }

    internal sealed class DanhSachMay_Model
    {
        public int Id { get; set; }
        public int MaCongDoan { get; set; }
        public string TenMay { get; set; } = string.Empty;
    }

    internal sealed class TenLoiDungMay_Model
    {
        public int Id { get; set; }
        public string TenLoi { get; set; } = string.Empty;
        public string MoTaLoi { get; set; } = string.Empty;
        public int MaCongDoan { get; set; }
    }

    // Bản ghi cha trong ThongTinDungMay.
    internal sealed class ThongTinDungMay_Model
    {
        public long Id { get; set; }
        public int DanhSachCongDoanId { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public int DanhSachMayId { get; set; }
        public string NguoiLam { get; set; } = string.Empty;
    }

    // Bản ghi con trong DSLoiDungMay (khác DanhSachLoiDungMay cũ).
    internal sealed class DSLoiDungMay_Model
    {
        public long Id { get; set; }
        public long ThongTinDungMayId { get; set; }
        public int ThoiGianDung { get; set; }
        public int TenLoiDungMayId { get; set; }
        public string GhiChu { get; set; } = string.Empty;
    }
}
