using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class Validator
    {
        public static bool TTCaLamViec(ThongTinCaLamViec data)
        {

            return true;
        }


        public static bool TTNVL(List<TTNVL> data)
        {

            return true;
        }


        public static bool TTThanhPham(TTThanhPham data)
        {

            return true;
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

                // xử lý logic riêng cho BocLot...

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
