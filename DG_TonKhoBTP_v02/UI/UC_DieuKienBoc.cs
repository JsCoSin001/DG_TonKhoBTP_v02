﻿using DG_TonKhoBTP_v02.Core;
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
    public partial class UC_DieuKienBoc : UserControl, ISectionProvider<CaiDatCDBoc>
    {
        public UC_DieuKienBoc()
        {
            InitializeComponent();
        }

        #region AI generated
        public string SectionName => nameof(UC_DieuKienBoc);

        public CaiDatCDBoc GetSectionData()
        {
            return new CaiDatCDBoc
            {
                DKKhuon1 = (double)dkKhuon1.Value,
                DKKhuon2 = (double)dkKhuon2.Value,
                TTNhua = ttNhua?.Text ?? string.Empty,
                NhuaPhe = (double)nhuaPhe.Value,
                GhiChuNhuaPhe = ghiChuNhuaPhe?.Text ?? string.Empty,
                DayPhe = (double)dayPhe.Value,
                GhiChuDayPhe = ghiChuDayPhe?.Text ?? string.Empty,
                KTDKLan1 = (double)KtDkLan1.Value,
                KTDKLan2 = (double)KtDkLan2.Value,
                KTDKLan3 = (double)KtDkLan3.Value,
                DiemMongLan1 = (double)DiemMongLan1.Value,
                DiemMongLan2 = (double)DiemMongLan2.Value
            };
        }
        #endregion
    }
}
