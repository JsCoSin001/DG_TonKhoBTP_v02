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
}
