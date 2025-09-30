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
    public partial class UC_CDBocVo : UserControl, ISectionProvider<CD_BocVo>
    {
        public UC_CDBocVo()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDBocVo);

        public CD_BocVo GetSectionData()
        {
            return new CD_BocVo
            {
                ThongTinSP_ID = null,                 // gán khi lưu DB
                DayVoTB = (double)dayVoTB.Value,   // thay nudDayVoTB bằng control thật
                InAn = inAn.Text,         // nếu không có checkbox, tự quyết theo combobox/text
            };
        }
    }
}
