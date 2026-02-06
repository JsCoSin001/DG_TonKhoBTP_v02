using DG_TonKhoBTP_v02.Core;
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

        public static string TTNVL(List<TTNVL> data, string tenMay)
        {
            string errorMessage = "";

            (int Id, string Lot)? tupleError = null;

            if (data == null || data.Count == 0) return EnumStore.ErrorNVL[1];

            foreach (TTNVL t in data)
            {
                #region B1) Kiểm tra việc nhập hay không nhập dữ liệu
                string lot = t.BinNVL;


                // B1.1) Nếu là nguyên vật liệu thì bỏ qua để kiểm tra dòng khác
                if (t.CdBatDau < 0 && t.KlBatDau < 0) continue;
                

                // B1.2) Nếu BTP, nếu đơn vị là kg thì phải nhập khối lượng còn lại
                // Nếu đơn vị là m thì phải nhập chiều dài còn lại
                if (t.DonVi == "KG" && t.KlConLai == null)
                {
                    tupleError = (Id: 2, Lot: lot);
                    break;
                }

                if (t.DonVi == "M" && t.CdConLai == null)
                {
                    tupleError = (Id: 3, Lot: lot);
                    break;
                }

                // B1.3) Các dữ liệu yêu cầu phải được nhập
                if (t.BanRongBang == null || t.DoDayBang == null || t.KetCauLoi == null || t.DanhSachMaSP_ID == 0 || t.BinNVL == "")
                {
                    tupleError = (Id: 4, Lot: lot);
                    break;
                }


                // B1.3) Tên QC phải nhập
                if (t.QC == "")
                {
                    tupleError = (Id: 7, Lot: lot);
                    break;
                }

                #endregion


                #region B2) Kiểm tra logic khi nhập dữ liệu

                // B2.1) Kiểm tra logic trong các dữ liệu nhập


                if (t.CdBatDau <= t.CdConLai)
                {
                    tupleError = (Id: 6, Lot: lot);
                    break;
                }

                string tenMayNVL = CoreHelper.CatMaBin(t.BinNVL)[0];

                if (EnumStore.dsTenMayBoQuaKiemTraKhoiLuongConLai.Contains(tenMay) || EnumStore.dsTenMayBoQuaKiemTraKhoiLuongConLai.Contains(tenMayNVL)) continue;

                if (t.KlBatDau <= t.KlConLai)
                {
                    tupleError = (Id: 5, Lot: lot);
                    break;
                }


                #endregion

            }

            if (tupleError.HasValue)
            {
                string errorName = EnumStore.ErrorNVL[tupleError.Value.Id];
                errorMessage = $"Lot {tupleError.Value.Lot}: {errorName}";
            }

            return errorMessage;

        }

        public static int TTThanhPham(TTThanhPham data)
        {
            if (data.DanhSachSP_ID == 0) return 1;

            if (string.IsNullOrWhiteSpace(data.MaBin)) return 2;

            if (data.DonVi == "KG" && data.KhoiLuongSau == 0) return 3;

            if (data.DonVi == "M" && data.ChieuDaiSau == 0) return 4;
            
            return 0;
        }

        public static List<object> KiemTraChiTietCongDoan(FormSnapshot data)
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
                // xử lý logic riêng cho BenRuot...

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
                // xử lý logic riêng cho BocLot...

                // xử lý logic riêng cho cài đặt bọc...
                caiDatCDBoc = (CaiDatCDBoc)data.Sections["CaiDatCDBoc"];
                if (!Check_CaiDatCDBoc(caiDatCDBoc)) caiDatCDBoc = null;

                chiTietCD = (CD_BocVo)bocVoObj;
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
