using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CaiDatMay : UserControl, ISectionProvider<CaiDatCDBoc>
    {
        public UC_CaiDatMay()
        {
            InitializeComponent();
        }

        #region AI generated

        public string SectionName => nameof(UC_CaiDatMay);

        public CaiDatCDBoc GetSectionData()
        {
            return new CaiDatCDBoc
            {
                // ComboBox -> bool (đơn giản hoá: có chọn là true)
                MangNuoc = !string.IsNullOrWhiteSpace(mangNuoc?.Text),
                PuliDanDay = !string.IsNullOrWhiteSpace(puliDanDay?.Text),
                BoDemMet = !string.IsNullOrWhiteSpace(boDemMet?.Text),
                MayIn = !string.IsNullOrWhiteSpace(mayIn?.Text),

                v1 = (double?)v1?.Value,
                v2 = (double?)v2?.Value,
                v3 = (double?)v3?.Value,
                v4 = (double?)v4?.Value,
                v5 = (double?)v5?.Value,
                v6 = (double?)v6?.Value,

                Co = (double?)co?.Value,
                Dau1 = (double?)dau1?.Value,
                Dau2 = (double?)dau2?.Value,
                Khuon = (double?)khuon?.Value,
                BinhSay = (double?)binhSay?.Value
            };
        }
        #endregion


    }
}
