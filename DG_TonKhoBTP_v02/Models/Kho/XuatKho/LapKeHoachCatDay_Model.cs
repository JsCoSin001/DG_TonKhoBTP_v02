using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Models.Kho.XuatKho
{
    internal enum LapKeHoachCatDay_SearchType
    {
        ChieuDai = 0,
        Lot = 1,
        TenSanPham = 2,
        KhachHang = 3,
        TatCa = 4
    }

    internal enum LapKeHoachCatDay_LoaiDong
    {
        CuonChan = 0,
        CuonLe = 1
    }

    internal static class LapKeHoachCatDay_TrangThai
    {
        public const string ChuaLuu = "Chưa lưu";
        public const string ChuaThucHien = "Chưa thực hiện";
        public const string DangThucHien = "Đang thực hiện";
        public const string HoanThanh = "Hoàn thành";
        public const string HetTon = "Hết tồn";
        public const string DaThucHien = "Đã thực hiện";
    }

    internal sealed class LapKeHoachCatDay_KeHoachHeader
    {
        public long Id { get; set; }
        public string MaKeHoach { get; set; } = string.Empty;
        public string NgayKeHoach { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "ACTIVE";
        public string NguoiNhan { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string NguoiTao { get; set; } = string.Empty;
    }

    internal sealed class LapKeHoachCatDay_ChiTietCat
    {
        public long? Id { get; set; }
        public long? KeHoachCatNhom_ID { get; set; }
        public int ChieuDai { get; set; }
        public bool DaThucHien { get; set; }

        public LapKeHoachCatDay_ChiTietCat Clone()
        {
            return new LapKeHoachCatDay_ChiTietCat
            {
                Id = Id,
                KeHoachCatNhom_ID = KeHoachCatNhom_ID,
                ChieuDai = ChieuDai,
                DaThucHien = DaThucHien
            };
        }
    }

    /// <summary>
    /// Một nhóm cắt = một cuộn vật lý.
    /// TonCuonLe_ID = null: nhóm chưa bắt đầu, B3 sẽ chọn một cuộn chẵn thực tế.
    /// TonCuonLe_ID != null: nhóm đã gắn với đúng cuộn lẻ đó.
    /// </summary>
    internal sealed class LapKeHoachCatDay_NhomCat
    {
        public long? Id { get; set; }
        public long? TonCuonLe_ID { get; set; }
        public List<LapKeHoachCatDay_ChiTietCat> ChiTiet { get; set; } = new List<LapKeHoachCatDay_ChiTietCat>();

        public bool CoThucHien => ChiTiet.Any(x => x.DaThucHien);
        public bool HoanThanh => ChiTiet.Count > 0 && ChiTiet.All(x => x.DaThucHien);
        public int TongChuaThucHien => ChiTiet.Where(x => !x.DaThucHien).Sum(x => x.ChieuDai);

        public LapKeHoachCatDay_NhomCat Clone()
        {
            return new LapKeHoachCatDay_NhomCat
            {
                Id = Id,
                TonCuonLe_ID = TonCuonLe_ID,
                ChiTiet = ChiTiet.Select(x => x.Clone()).ToList()
            };
        }
    }

    internal sealed class LapKeHoachCatDay_GridRow
    {
        public string RowKey { get; set; } = string.Empty;
        public LapKeHoachCatDay_LoaiDong LoaiDong { get; set; }

        public long DanhSachMaSP_ID { get; set; }
        public long? KeHoachHang_ID { get; set; }
        public long? TonCuonLe_ID { get; set; }
        public long? TTThanhPham_IDNguon { get; set; }
        public long? TTCuonDay_IDNguon { get; set; }

        public string TenSP { get; set; } = string.Empty;
        public string MaNguon { get; set; } = string.Empty;

        /// <summary>Cuộn chẵn: số cuộn. Cuộn lẻ: chiều dài còn thực tế.</summary>
        public int TonThucTe { get; set; }

        /// <summary>Reservation của các kế hoạch khác. Cuộn chẵn: cuộn; cuộn lẻ: chiều dài.</summary>
        public int DatTruoc { get; set; }

        public int TonKhaDung => Math.Max(0, TonThucTe - DatTruoc);

        /// <summary>
        /// Giá trị hiển thị cột Chiều dài 1 cuộn.
        /// Cuộn chẵn: chiều dài chuẩn. Cuộn lẻ: chiều dài còn hiện tại.
        /// </summary>
        public int ChieuDai1Cuon { get; set; }

        /// <summary>Chiều dài chuẩn của cuộn chẵn dùng snapshot KeHoachHang.</summary>
        public int? ChieuDaiChuanKeHoach { get; set; }

        public int? SoDau { get; set; }
        public int? SoCuoi { get; set; }

        /// <summary>Tổng số cuộn nguyên theo kế hoạch (không phải số còn lại).</summary>
        public int SoLuongCanLay { get; set; }
        public int SoLuongDaLay { get; set; }

        /// <summary>
        /// Chỉ chứa phần chưa thực hiện có thể sửa trực tiếp ở cell Chiều dài cắt.
        /// Với cuộn chẵn, chuỗi này đại diện đúng một KeHoachCatNhom.
        /// Với cuộn lẻ, tất cả đoạn chưa thực hiện của cuộn được gom để người dùng sửa.
        /// </summary>
        public string ChuoiChieuDaiCat { get; set; } = string.Empty;

        /// <summary>Các nhóm cắt nhiều cuộn chẵn được chỉnh trong Frm_CatLeCuonChan.</summary>
        public List<LapKeHoachCatDay_NhomCat> NhomCatPopup { get; set; } = new List<LapKeHoachCatDay_NhomCat>();

        /// <summary>Tất cả nhóm đã lưu của dòng, dùng để bảo toàn lịch sử B3 khi update.</summary>
        public List<LapKeHoachCatDay_NhomCat> NhomCatDaLuu { get; set; } = new List<LapKeHoachCatDay_NhomCat>();

        public bool DaTonTaiTrongDB { get; set; }
        public string TrangThai { get; set; } = LapKeHoachCatDay_TrangThai.ChuaLuu;
        public bool CanEdit { get; set; } = true;
        public bool CanDelete { get; set; }

        public bool CoThucHienB3 => SoLuongDaLay > 0 || NhomCatDaLuu.Any(x => x.CoThucHien);
        public bool HoanThanh => string.Equals(TrangThai, LapKeHoachCatDay_TrangThai.HoanThanh, StringComparison.Ordinal);

        public List<int> LayCacDoanDaThucHien()
        {
            return NhomCatDaLuu
                .SelectMany(x => x.ChiTiet)
                .Where(x => x.DaThucHien)
                .Select(x => x.ChieuDai)
                .ToList();
        }

        public string TaoChuoiHienThiChieuDaiCat()
        {
            var daThucHien = LayCacDoanDaThucHien();
            var parts = new List<string>();

            if (daThucHien.Count > 0)
                parts.AddRange(daThucHien.Select(x => x + " (Đã thực hiện)"));

            if (!string.IsNullOrWhiteSpace(ChuoiChieuDaiCat))
                parts.AddRange(ChuoiChieuDaiCat.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            return string.Join("; ", parts);
        }
    }

    internal sealed class LapKeHoachCatDay_KeHoachContext
    {
        public bool TonTai { get; set; }
        public LapKeHoachCatDay_KeHoachHeader Header { get; set; }
        public List<LapKeHoachCatDay_GridRow> Rows { get; set; } = new List<LapKeHoachCatDay_GridRow>();
    }

    internal sealed class LapKeHoachCatDay_SearchCriteria
    {
        public LapKeHoachCatDay_SearchType SearchType { get; set; }
        public string SearchValue { get; set; } = string.Empty;
        public int? ChieuDaiToiThieu { get; set; }
        public long? KeHoach_IDDangSua { get; set; }
    }

    internal sealed class LapKeHoachCatDay_SaveRequest
    {
        public long? KeHoach_IDDuKien { get; set; }
        public string MaKeHoach { get; set; } = string.Empty;
        public string NguoiNhan { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string NguoiTao { get; set; } = string.Empty;
        public LapKeHoachCatDay_GridRow Row { get; set; }
    }

    internal sealed class LapKeHoachCatDay_SaveResult
    {
        public bool ThanhCong { get; set; }
        public bool TaoMoiKeHoach { get; set; }
        public bool DuLieuDaCu { get; set; }
        public long KeHoach_ID { get; set; }
        public string Loi { get; set; } = string.Empty;
    }

    internal sealed class LapKeHoachCatDay_DeletePreview
    {
        public bool CoTheXoa { get; set; }
        public bool CoThucHienMotPhan { get; set; }
        public bool SeXoaCaKeHoach { get; set; }
        public string Loi { get; set; } = string.Empty;
    }

    internal sealed class LapKeHoachCatDay_DeleteRequest
    {
        public long KeHoach_ID { get; set; }
        public LapKeHoachCatDay_GridRow Row { get; set; }
    }

    internal sealed class LapKeHoachCatDay_DeleteResult
    {
        public bool ThanhCong { get; set; }
        public bool DaXoaKeHoach { get; set; }
        public string Loi { get; set; } = string.Empty;
    }

    internal static class LapKeHoachCatDay_ChieuDaiParser
    {
        /// <summary>
        /// Chỉ chuẩn hóa khoảng trắng và dấu ';' cuối. Không tự bỏ token sai.
        /// </summary>
        public static bool TryNormalize(
            string input,
            out string normalized,
            out List<int> values,
            out string invalidValue,
            out bool syntaxError)
        {
            normalized = string.Empty;
            values = new List<int>();
            invalidValue = string.Empty;
            syntaxError = false;

            if (string.IsNullOrWhiteSpace(input))
                return true;

            string compact = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
            compact = compact.TrimEnd(';');

            if (compact.Length == 0)
                return true;

            string[] tokens = compact.Split(';');
            foreach (string token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    syntaxError = true;
                    return false;
                }

                if (!int.TryParse(token, out int value) || value <= 0)
                {
                    invalidValue = token;
                    return false;
                }

                values.Add(value);
            }

            normalized = string.Join(";", values);
            return true;
        }

        public static string TaoThongBaoLoi(string invalidValue, bool syntaxError)
        {
            if (syntaxError)
                return "Cú pháp chiều dài cắt không hợp lệ";

            if (int.TryParse(invalidValue, out int numericValue) && numericValue <= 0)
                return "Chiều dài cắt phải là số nguyên lớn hơn 0.";

            return "Giá trị " + invalidValue + " không hợp lệ";
        }
    }
}
