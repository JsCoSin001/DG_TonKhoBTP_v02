using System;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    internal enum LoiDungMayInputMode
    {
        Manual,
        Automatic
    }

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
    /// Model tương ứng với table DanhSachLoiDungMay sau refactor.
    /// Dữ liệu được liên kết theo TTThanhPham_ID; không còn định danh theo Ngày + Máy + Ca.
    /// </summary>
    public sealed class DanhSachLoiDungMay_Model
    {
        public int Id { get; set; }
        public int TenLoiDungMayId { get; set; }
        public string TenLoi { get; set; } = string.Empty;
        public int DanhSachMayId { get; set; }
        public TimeSpan? ThoiGianBatDau { get; set; }
        public TimeSpan? ThoiGianKetThuc { get; set; }
        public int ThoiGianDung { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public int MaCongDoan { get; set; }
        public long? TTThanhPhamId { get; set; }
    }
}
