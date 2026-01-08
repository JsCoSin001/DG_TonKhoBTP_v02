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
    public partial class UC_CDBenRuot : UserControl, ISectionProvider<CD_BenRuot>, IDataReceiver
    {
        public UC_CDBenRuot()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDBenRuot);

        public CD_BenRuot GetSectionData()
        {
            Console.WriteLine(ChieuXoan.Text);
            return new CD_BenRuot
            {
                TTThanhPhan_ID = 0,
                DKSoi = dkSoi.Value == 0 ? (double?)null : (double)dkSoi.Value,
                SoSoi = soSoi.Value == 0 ? (int?)null : (int)soSoi.Value,
                ChieuXoan = string.IsNullOrWhiteSpace(ChieuXoan.Text) ? null : ChieuXoan.Text,
                BuocBen = buocBen.Value == 0 ? (double?)null : (double)buocBen.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DKSoi", val => dkSoi.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "BenRuot_SoSoi", val => soSoi.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "BenRuot_ChieuXoan", val => ChieuXoan.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "BuocBen", val => buocBen.Value = Convert.ToDecimal(val));
        }
    }
}
