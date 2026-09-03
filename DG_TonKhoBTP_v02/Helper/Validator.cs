using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.VariantTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class Validator
    {
        public static int TTCaLamViec(ThongTinCaLamViec data)
        {
            if (data == null || CaLamViecPolicy.LaNgayChuaChon(data.Ngay))
                return 4;

            if (string.IsNullOrWhiteSpace(data.May))
                return 1;

            if (string.IsNullOrWhiteSpace(data.NguoiLam))
                return 2;

            return 0;
        }

        public static List<string> LayDanhSachLoiTTCaLamViec(ThongTinCaLamViec data)
        {
            var result = new List<string>();

            if (data == null || CaLamViecPolicy.LaNgayChuaChon(data.Ngay))
                result.Add(EnumStore.ErrorCaLamViec[4]);

            if (data == null || string.IsNullOrWhiteSpace(data.May))
                result.Add(EnumStore.ErrorCaLamViec[1]);

            if (data == null || string.IsNullOrWhiteSpace(data.NguoiLam))
                result.Add(EnumStore.ErrorCaLamViec[2]);

            return result;
        }

        public static string TTNVL(
            List<TTNVLRow> data,
            string tenMay,
            CongDoan congDoan)
        {
            if (data == null || data.Count == 0)
                return EnumStore.ErrorNVL[1];

            foreach (TTNVLRow nvl in data)
            {
                string lot = nvl.BinNVL ?? string.Empty;

                // Dòng nhập tay phải được kiểm tra trước điều kiện bỏ qua NVL cũ.
                if (NvlNhapTayPolicy.ApDung(nvl))
                {
                    string loiNhapTay = KiemTraDongNhapTay(nvl);
                    if (!string.IsNullOrEmpty(loiNhapTay))
                        return TaoThongBaoTheoLot(lot, loiNhapTay);
                }

                // Giữ nguyên hoạt động hiện tại đối với dòng không có cả hai giá trị bắt đầu.
                if (nvl.CdBatDau < 0 && nvl.KlBatDau < 0)
                    continue;

                // IsCorrect == false không chặn lưu tại Validator.TTNVL.
                // Dòng khác BOM đã được cảnh báo khi quét.

                if (nvl.KetCauLoi == null ||
                    nvl.DanhSachMaSP_ID == 0 ||
                    string.IsNullOrEmpty(nvl.BinNVL))
                {
                    return TaoThongBaoTheoLot(lot, EnumStore.ErrorNVL[4]);
                }

                if (string.IsNullOrEmpty(nvl.QC))
                {
                    return TaoThongBaoTheoLot(lot, EnumStore.ErrorNVL[7]);
                }
            }

            return string.Empty;
        }

        // Giữ overload cũ để các vị trí khác trong dự án không bị lỗi biên dịch.
        public static string TTNVL(List<TTNVLRow> data, string tenMay)
        {
            return TTNVL(data, tenMay, null);
        }

        public static List<string> LayDanhSachLoiTTNVL(
            List<TTNVLRow> data,
            string tenMay,
            CongDoan congDoan)
        {
            var result = new List<string>();

            if (data == null || data.Count == 0)
            {
                result.Add(EnumStore.ErrorNVL[1]);
                return result;
            }

            foreach (TTNVLRow nvl in data)
            {
                if (nvl == null)
                {
                    result.Add(EnumStore.ErrorNVL[4]);
                    continue;
                }

                string lot = nvl.BinNVL ?? string.Empty;

                if (NvlNhapTayPolicy.ApDung(nvl))
                {
                    string loiNhapTay = KiemTraDongNhapTay(nvl);
                    if (!string.IsNullOrEmpty(loiNhapTay))
                        result.Add(TaoThongBaoTheoLot(lot, loiNhapTay));
                }

                if (nvl.CdBatDau < 0 && nvl.KlBatDau < 0)
                    continue;

                if (nvl.KetCauLoi == null ||
                    nvl.DanhSachMaSP_ID == 0 ||
                    string.IsNullOrEmpty(nvl.BinNVL))
                {
                    result.Add(TaoThongBaoTheoLot(lot, EnumStore.ErrorNVL[4]));
                }

                if (string.IsNullOrEmpty(nvl.QC))
                    result.Add(TaoThongBaoTheoLot(lot, EnumStore.ErrorNVL[7]));
            }

            return result
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string KiemTraDongNhapTay(TTNVLRow nvl)
        {
            string donVi = (nvl.DonVi ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            switch (donVi)
            {
                case "KG":
                    return KiemTraDongNhapTayDonViKg(nvl);

                case "M":
                    return KiemTraDongNhapTayDonViMet(nvl);

                default:
                    return $"Đơn vị NVL/BTP '{nvl.DonVi}' chưa được hỗ trợ cho chế độ nhập tay.";
            }
        }

        private static string KiemTraDongNhapTayDonViKg(TTNVLRow nvl)
        {
            if (!CoGiaTriBatDauBatBuoc(nvl.KlBatDau))
                return "Không có KL bắt đầu hợp lệ.";

            if (!nvl.KlConLai.HasValue)
                return "Vui lòng nhập KL còn lại.";

            string loi = KiemTraGiaTriConLai(
                tenCotConLai: "KL còn lại",
                tenCotBatDau: "KL bắt đầu",
                giaTriConLai: nvl.KlConLai,
                giaTriBatDau: nvl.KlBatDau,
                batBuoc: true);

            if (!string.IsNullOrEmpty(loi))
                return loi;

            return KiemTraGiaTriConLai(
                tenCotConLai: "CD còn lại",
                tenCotBatDau: "CD bắt đầu",
                giaTriConLai: nvl.CdConLai,
                giaTriBatDau: nvl.CdBatDau,
                batBuoc: false);
        }

        private static string KiemTraDongNhapTayDonViMet(TTNVLRow nvl)
        {
            if (!CoGiaTriBatDauBatBuoc(nvl.CdBatDau))
                return "Không có CD bắt đầu hợp lệ.";

            if (!nvl.CdConLai.HasValue)
                return "Vui lòng nhập CD còn lại.";

            string loi = KiemTraGiaTriConLai(
                tenCotConLai: "CD còn lại",
                tenCotBatDau: "CD bắt đầu",
                giaTriConLai: nvl.CdConLai,
                giaTriBatDau: nvl.CdBatDau,
                batBuoc: true);

            if (!string.IsNullOrEmpty(loi))
                return loi;

            return KiemTraGiaTriConLai(
                tenCotConLai: "KL còn lại",
                tenCotBatDau: "KL bắt đầu",
                giaTriConLai: nvl.KlConLai,
                giaTriBatDau: nvl.KlBatDau,
                batBuoc: false);
        }

        private static string KiemTraGiaTriConLai(
            string tenCotConLai,
            string tenCotBatDau,
            double? giaTriConLai,
            double? giaTriBatDau,
            bool batBuoc)
        {
            if (!giaTriConLai.HasValue)
            {
                return batBuoc
                    ? $"Vui lòng nhập {tenCotConLai}."
                    : string.Empty;
            }

            if (giaTriConLai.Value < 0)
                return $"{tenCotConLai} không được âm.";

            // Cột không bắt buộc: nếu không có giá trị bắt đầu theo quy ước hiện tại
            // (null hoặc < 0) thì chỉ kiểm tra số âm và ghi nhận giá trị người dùng nhập.
            if (!CoGiaTriBatDauDeSoSanh(giaTriBatDau))
                return string.Empty;

            if (giaTriConLai.Value >= giaTriBatDau.Value)
                return $"{tenCotConLai} phải nhỏ hơn {tenCotBatDau}.";

            return string.Empty;
        }

        private static bool CoGiaTriBatDauBatBuoc(double? value)
        {
            return value.HasValue && value.Value > 0;
        }

        private static bool CoGiaTriBatDauDeSoSanh(double? value)
        {
            return value.HasValue && value.Value >= 0;
        }

        private static string TaoThongBaoTheoLot(string lot, string noiDung)
        {
            return string.IsNullOrWhiteSpace(lot)
                ? noiDung
                : $"Lot {lot}: {noiDung}";
        }

        public static int TTThanhPham(TTThanhPham data)
        {
            if (data.DanhSachSP_ID == 0) return 1;

            if (string.IsNullOrWhiteSpace(data.MaBin)) return 2;

            if (data.DonVi == "KG" && data.KhoiLuongSau == 0) return 3;

            if (data.DonVi == "M" && data.ChieuDaiSau == 0) return 4;
            
            return 0;
        }

        public static List<string> LayDanhSachLoiTTThanhPham(TTThanhPham data)
        {
            var result = new List<string>();
            if (data == null)
            {
                result.Add("Thiếu thông tin Thành phẩm");
                return result;
            }

            if (data.DanhSachSP_ID == 0) result.Add(EnumStore.ErrorTP[1]);
            if (string.IsNullOrWhiteSpace(data.MaBin)) result.Add(EnumStore.ErrorTP[2]);
            if (data.DonVi == "KG" && data.KhoiLuongSau <= 0) result.Add(EnumStore.ErrorTP[3]);
            if (data.DonVi == "M" && data.ChieuDaiSau <= 0) result.Add(EnumStore.ErrorTP[4]);
            return result;
        }


        public static List<object> KiemTraChiTietCongDoan(FormSnapshot data, int idCongDoan = 0)
        {
            var result = new List<object>();
            var chiTietCD = new object();

            CaiDatCDBoc caiDatCDBoc = null;


            if (data.Sections.TryGetValue("CD_KeoRut", out var keoRutObj))
            {
                // Kiểm tra input của kéo rút

                chiTietCD = (CD_KeoRut)keoRutObj;
            }

            if (data.Sections.TryGetValue("CD_BenRuot", out var benRuotObj))
            {
                chiTietCD = (CD_BenRuot)benRuotObj;
            }

            if (data.Sections.TryGetValue("CD_GhepLoiQB", out var ghepLoiObj))
            {
                // xử lý logic riêng cho GhepLoiQB...

                chiTietCD = (CD_GhepLoiQB)ghepLoiObj;
            }

            if (data.Sections.TryGetValue("CD_BocLot", out var bocLotObj))
            {
                // xử lý logic riêng cho BocLot...

                // xử lý logic riêng cho cài đặt bọc...
                caiDatCDBoc = (CaiDatCDBoc)data.Sections["CaiDatCDBoc"];
                if (!Check_CaiDatCDBoc(caiDatCDBoc)) caiDatCDBoc = null;

                chiTietCD = (CD_BocLot)bocLotObj;
            }

            if (data.Sections.TryGetValue("CD_BocMach", out var bocMachObj))
            {

                // xử lý logic riêng cho bọc mạch...

                // xử lý logic riêng cho cài đặt bọc...
                caiDatCDBoc = (CaiDatCDBoc)data.Sections["CaiDatCDBoc"];
                if (!Check_CaiDatCDBoc(caiDatCDBoc)) caiDatCDBoc = null;

                chiTietCD = (CD_BocMach)bocMachObj;

            }

            if (data.Sections.TryGetValue("CD_BocVo", out var bocVoObj))
            {
                // xử lý logic riêng cho cài đặt bọc...
                caiDatCDBoc = (CaiDatCDBoc)data.Sections["CaiDatCDBoc"];
                if (!Check_CaiDatCDBoc(caiDatCDBoc)) caiDatCDBoc = null;

                CD_BocVo bocVo = (CD_BocVo)bocVoObj;

                // Kiểm tra xem có thông tin cuộn dây chưa
                chiTietCD = bocVo.TTCuonDay_CD.Count > 0 ? bocVo: null;

            }

            if (data.Sections.TryGetValue("CD_ChieuXa", out var chieuXaObj))
            {
                var chieuXa = (CD_ChieuXa)chieuXaObj;
                chiTietCD = LayDanhSachLoiCDChieuXa(chieuXa).Count == 0
                    ? (object)chieuXa
                    : null;
            }

            result.Add(chiTietCD);
            result.Add(caiDatCDBoc);

            return result;
        }

        public static List<string> LayDanhSachLoiChiTietCongDoan(FormSnapshot data)
        {
            if (data?.Sections == null)
                return new List<string> { "Chi tiết công đoạn chưa hợp lệ" };

            if (data.Sections.TryGetValue("CD_ChieuXa", out var chieuXaObj))
                return LayDanhSachLoiCDChieuXa(chieuXaObj as CD_ChieuXa);

            List<object> chiTiet = KiemTraChiTietCongDoan(data);
            if (chiTiet == null || chiTiet.Count == 0 || chiTiet[0] == null)
                return new List<string> { "Chi tiết công đoạn chưa hợp lệ" };

            return new List<string>();
        }

        private static List<string> LayDanhSachLoiCDChieuXa(CD_ChieuXa data)
        {
            var result = new List<string>();
            if (data == null)
            {
                result.Add("Chi tiết công đoạn Chiếu Xạ chưa hợp lệ");
                return result;
            }

            if (!data.LucCangThu.HasValue || data.LucCangThu.Value <= 0) result.Add("Lực căng thu phải lớn hơn 0");
            if (!data.LucCangTha.HasValue || data.LucCangTha.Value <= 0) result.Add("Lực căng thả phải lớn hơn 0");
            if (!data.SoVong.HasValue || data.SoVong.Value <= 0) result.Add("Số vòng phải lớn hơn 0");
            if (!data.TocDo.HasValue || data.TocDo.Value <= 0) result.Add("Tốc độ phải lớn hơn 0");
            if (!data.NLCX.HasValue || data.NLCX.Value <= 0) result.Add("NLCX phải lớn hơn 0");
            if (!data.DongDien.HasValue || data.DongDien.Value <= 0) result.Add("Dòng điện phải lớn hơn 0");
            if (!data.LieuChieu.HasValue || data.LieuChieu.Value <= 0) result.Add("Liều chiếu phải lớn hơn 0");
            if (string.IsNullOrWhiteSpace(data.NgoaiQuan)) result.Add("Ngoại quan chưa được chọn");
            if (string.IsNullOrWhiteSpace(data.DoChiuNhiet)) result.Add("Độ chịu nhiệt chưa được chọn");
            return result;
        }

        private static bool Check_CaiDatCDBoc(CaiDatCDBoc data)
        {
            return true;
        }

    }

    public static class CongDoanPolicy
    {
        // Chiếu Xạ là công đoạn duy nhất được bổ sung quy tắc bỏ qua BOM hoàn toàn.
        // Không thay đổi hành vi BOM hiện hữu của các công đoạn khác.
        public static bool CanKiemTraBom(CongDoan congDoan)
        {
            return congDoan != null && congDoan.Id != 10;
        }

        // Bện hiện tại không bật hộp thoại xác nhận sai BOM khi quét.
        // Chiếu Xạ cũng không bật vì không kiểm tra BOM.
        public static bool CanCanhBaoSaiBomKhiQuet(CongDoan congDoan)
        {
            return congDoan != null && congDoan.Id != 1 && congDoan.Id != 10;
        }
    }

    public static class CaLamViecPolicy
    {
        public static readonly DateTime NgayChuaChon = new DateTime(1753, 1, 1);

        public static bool LaNgayChuaChon(string ngay)
        {
            if (string.IsNullOrWhiteSpace(ngay))
                return true;

            return DateTime.TryParse(ngay, out DateTime value) &&
                   value.Date == NgayChuaChon.Date;
        }
    }
}
