using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class TTSanPham
    {
        public UC_CaiDatMay uC_CaiDatMay { get; } = new UC_CaiDatMay();
        public UC_DieuKienBoc uC_DieuKienBoc { get; } = new UC_DieuKienBoc();
    }
}
