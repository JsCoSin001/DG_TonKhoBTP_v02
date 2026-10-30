using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.Kho.TonKho
{
    internal enum BCTonKho_LoaiTimKiem { Bin = 1, SanPham = 2, CuonLe = 3 }
    internal enum BCTonKho_KieuDong { CuonChan = 1, CuonLe = 2 }

    internal sealed class BCTonKho_GoiY
    {
        public BCTonKho_LoaiTimKiem Loai { get; set; }
        public long? Id { get; set; }
        public string GiaTri { get; set; } = string.Empty;
    }

    internal sealed class BCTonKho_DongTon
    {
        public long SanPhamId { get; set; }
        public long? TTThanhPhamId { get; set; }
        public long? TTCuonDayId { get; set; }
        public long? TonCuonLeId { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string MaBinGoc { get; set; } = string.Empty;
        public string MaHienThi { get; set; } = string.Empty;
        public BCTonKho_KieuDong Kieu { get; set; }
        public long SoLuong { get; set; }
        public long? SoDau { get; set; }
        public long? SoCuoi { get; set; }
        public long? ChieuDaiBanDau { get; set; }
        public long? ChieuDaiConLai { get; set; }
        public long DatCho { get; set; }
        public string DonViDatCho { get; set; } = "cuộn";
        public bool DatChoDuKien { get; set; }
        public bool LaBatThuong { get; set; }
        public string LyDoBatThuong { get; set; } = string.Empty;
        // Thông tin để phân bổ đặt chỗ theo ngày nhập, không hiển thị trong grid.
        public string NgayNhap { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public void DanhDauLoi(string reason)
        {
            LaBatThuong = true;
            LyDoBatThuong += (LyDoBatThuong.Length == 0 ? "" : "; ") + reason;
        }
    }

    internal sealed class BCTonKho_KetQua
    {
        public List<BCTonKho_DongTon> Dong { get; set; } = new List<BCTonKho_DongTon>();
        public bool CoBatThuong { get; set; }
    }
}
