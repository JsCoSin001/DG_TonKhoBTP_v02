using System;
using System.Collections.Generic;
using System.Linq;

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
    internal sealed class BCTonKho_NguonTon
    {
        public long TTCuonDayId { get; set; }
        public string MaBin { get; set; } = string.Empty;
        public long SoCuonNhap { get; set; }
        public long SoCuonDaLay { get; set; }
        public long SoCuonChuyenLe { get; set; }
        public long SoCuonCon => SoCuonNhap - SoCuonDaLay - SoCuonChuyenLe;
        public long SoCuonGop { get; set; }
    }

    internal sealed class BCTonKho_LotGop
    {
        public string MaBin { get; set; } = string.Empty;
        public long SoCuon { get; set; }
        public decimal SoMet { get; set; }
    }

    internal sealed class BCTonKho_DatTruocKeHoach
    {
        public long KeHoachId { get; set; }
        public string MaKeHoach { get; set; } = string.Empty;
        public long SoCuonConPhaiLay { get; set; }
        public long SoCuonDanhDeCat { get; set; }
        public decimal SoMet { get; set; }
    }

    internal sealed class BCTonKho_LanCat
    {
        public long Id { get; set; }
        public long TonCuonLeId { get; set; }
        public long? TTCuonDayId { get; set; }
        public string MaCuon { get; set; } = string.Empty;
        public string LoaiNguon { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
        public string NguoiThucHien { get; set; } = string.Empty;
        public string MaKeHoach { get; set; } = string.Empty;
        public long? ChieuDaiCat { get; set; }
        public long? SoDauTruoc { get; set; }
        public long? SoCuoiTruoc { get; set; }
        public long? SoDauCat { get; set; }
        public long? SoCuoiCat { get; set; }
        public long? SoDauSau { get; set; }
        public long? SoCuoiSau { get; set; }
    }

    internal sealed class BCTonKho_ChiTietDong
    {
        private readonly long _chieuDaiMoiCuon;
        private readonly List<BCTonKho_LanCat> _lichSuCat = new List<BCTonKho_LanCat>();
        private readonly List<BCTonKho_NguonTon> _nguon = new List<BCTonKho_NguonTon>();
        private readonly List<BCTonKho_DatTruocKeHoach> _datTruoc = new List<BCTonKho_DatTruocKeHoach>();

        internal BCTonKho_ChiTietDong(long chieuDaiMoiCuon = 0)
        {
            _chieuDaiMoiCuon = chieuDaiMoiCuon;
        }

        public IReadOnlyList<BCTonKho_LanCat> LichSuCat => _lichSuCat;
        public IReadOnlyList<BCTonKho_NguonTon> Nguon => _nguon;
        public IReadOnlyList<BCTonKho_DatTruocKeHoach> DatTruoc => _datTruoc;

        public IReadOnlyList<BCTonKho_LotGop> LotGop => _nguon
            .Where(x => x.SoCuonGop > 0 && x.MaBin.Length > 0)
            .GroupBy(x => x.MaBin, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x =>
            {
                long soCuon = x.Sum(nguon => nguon.SoCuonGop);
                return new BCTonKho_LotGop
                {
                    MaBin = x.Key, SoCuon = soCuon, SoMet = (decimal)soCuon * _chieuDaiMoiCuon
                };
            }).ToList();

        internal void ThemLanCat(BCTonKho_LanCat lanCat) => _lichSuCat.Add(lanCat);
        internal void ThemNguon(BCTonKho_NguonTon nguon) => _nguon.Add(nguon);
        internal void ThemDatTruoc(BCTonKho_DatTruocKeHoach keHoach) => _datTruoc.Add(keHoach);
    }

    /// <summary>
    /// Dòng tồn kho cho báo cáo mới B1-B2-B3.
    /// Số cuộn của loại Cuộn: số cuộn nguyên còn sau các thao tác B3, gộp theo sản phẩm + quy cách.
    /// Số cuộn của loại Lẻ: 1 đối với mỗi TonCuonLe; chiều dài 1 cuộn là chiều dài HIỆN CÒN.
    /// </summary>
    internal sealed class BCTonKho_ExcelDong
    {
        public long DanhSachMaSPId { get; set; }
        public long? TonCuonLeId { get; set; }
        public List<long> TTCuonDayIds { get; } = new List<long>();
        public long SoCuon { get; set; }
        public long ChieuDaiMoiCuon { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string MaSanPham { get; set; } = string.Empty;
        public string DonVi { get; set; } = string.Empty;
        public string MaBinLot { get; set; } = string.Empty;
        public BCTonKho_ChiTietDong? ChiTiet { get; set; }
        public string LotHienThiBang => ChiTiet?.LotGop.Count > 1 ? "Nhiều LOT" : MaBinLot;
        // Cuộn nguyên gộp theo sản phẩm + chiều dài: giữ đầy đủ mã Bin còn tồn,
        // không thay đổi quy tắc gộp hay nhân đôi số mét.
        private readonly HashSet<string> _maBinNguon = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public void ThemMaBin(string maBin)
        {
            maBin = (maBin ?? string.Empty).Trim();
            if (maBin.Length > 0 && _maBinNguon.Add(maBin))
                MaBinLot = string.Join(" | ", _maBinNguon.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }
        public string Loai { get; set; } = string.Empty;
        // Một công thức duy nhất cho cả Cuộn và Lẻ, tránh cộng trùng số lượng nhập ban đầu.
        public decimal ChieuDaiTonThucTe => (decimal)SoCuon * ChieuDaiMoiCuon;
        public decimal ChieuDaiTonDatTruoc { get; set; }
        public decimal ChieuDaiTonKhaDung => ChieuDaiTonThucTe - ChieuDaiTonDatTruoc;
        public long? SoDau { get; set; }
        public long? SoCuoi { get; set; }
        public string CanhBao { get; private set; } = string.Empty;
        public bool LaBatThuong => !string.IsNullOrWhiteSpace(CanhBao);
        public void ThemCanhBao(string noiDung)
        {
            CanhBao += (CanhBao.Length == 0 ? "" : "\n") + noiDung;
        }
    }

}
