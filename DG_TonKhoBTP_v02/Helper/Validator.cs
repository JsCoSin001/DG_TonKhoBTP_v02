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
            if (string.IsNullOrWhiteSpace(data.May))
                return 1;

            if (string.IsNullOrWhiteSpace(data.NguoiLam))
                return 2;

            return 0;
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
                if (NvlNhapTayPolicy.ApDung(congDoan, nvl))
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

            result.Add(chiTietCD);
            result.Add(caiDatCDBoc);

            return result;
        }

        private static bool Check_CaiDatCDBoc(CaiDatCDBoc data)
        {
            return true;
        }

    }
}
