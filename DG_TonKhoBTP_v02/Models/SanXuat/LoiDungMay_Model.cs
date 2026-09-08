using System;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
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

    /// <summary>
    /// Model tương ứng với table DanhSachLoiDungMay.
    /// ThoiGianDung luôn được tính từ ThoiGianBatDau/ThoiGianKetThuc,
    /// không nhận giá trị nhập tay từ UI.
    /// </summary>
    internal sealed class DanhSachLoiDungMay_Model
    {
        public int Id { get; set; }
        public int TenLoiDungMayId { get; set; }
        public DateTime Ngay { get; set; }
        public int DanhSachMayId { get; set; }
        public string NguoiLam { get; set; } = string.Empty;
        public TimeSpan ThoiGianBatDau { get; set; }
        public TimeSpan ThoiGianKetThuc { get; set; }
        public int ThoiGianDung { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public int Ca { get; set; }
        public int MaCongDoan { get; set; }

        // Dùng cho validation/hiển thị; không phải cột của DanhSachLoiDungMay.
        public string TenLoi { get; set; } = string.Empty;
    }
}
