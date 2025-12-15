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
                DKSoi = (double)dkSoi.Value,
                SoSoi = (int)soSoi.Value,
                ChieuXoan = ChieuXoan.Text,
                BuocBen = (double)buocBen.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "DKSoi", val => dkSoi.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "BenRuot_SoSoi", val => soSoi.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "BenRuot_ChieuXoan", val => ChieuXoan.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "BuocBen", val => buocBen.Value = Convert.ToDecimal(val));
        }
    }
}
