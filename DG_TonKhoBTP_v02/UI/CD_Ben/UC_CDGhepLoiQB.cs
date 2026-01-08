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
    public partial class UC_CDGhepLoiQB : UserControl, ISectionProvider<CD_GhepLoiQB>, IDataReceiver
    {
        public UC_CDGhepLoiQB()
        {
            InitializeComponent();
        }


        public string SectionName => nameof(CD_GhepLoiQB);

        public CD_GhepLoiQB GetSectionData()
        {
            return new CD_GhepLoiQB
            {
                TTThanhPhan_ID = 0,
                BuocXoan = (double)buocXoan.Value == 0 ? (double?)null : (double)buocXoan.Value,
                ChieuXoan = chieuXoan.SelectedIndex == -1 ? null : chieuXoan.Text,
                GoiCachMep = (double)goiCachMep.Value == 0 ? (double?)null : (double)goiCachMep.Value,
                DKBTP = (double)dkBTP.Value == 0 ? (double?)null : (double)dkBTP.Value
            };
        }



        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "BuocXoan", val => buocXoan.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "GhepLoi_ChieuXoan", val => chieuXoan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "GoiCachMep", val => goiCachMep.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "DKBTP", val => dkBTP.Value = Convert.ToDecimal(val));
        }
    }
}
