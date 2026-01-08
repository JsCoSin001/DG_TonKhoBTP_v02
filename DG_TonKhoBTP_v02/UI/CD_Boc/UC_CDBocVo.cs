using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CDBocVo : UserControl, ISectionProvider<CD_BocVo>, IDataReceiver
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
                TTThanhPhan_ID = null,                 
                DayVoTB = dayVoTB.Value == 0m ? null : (double?)dayVoTB.Value,
                InAn = string.IsNullOrEmpty(inAn.Text)  ? null : inAn.Text,         
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DayVoTB", val => dayVoTB.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "InAn", val => inAn.Text = Convert.ToString(val));
        }
    }
}
