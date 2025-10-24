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
                BuocXoan = (double)buocXoan.Value,
                ChieuXoan = chieuXoan?.Text,
                GoiCachMep = (double)goiCachMep.Value,
                DKBTP = (double)dkBTP.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "cgl_BuocXoan", val => buocXoan.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cgl_ChieuXoan", val => chieuXoan.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "cgl_GoiCachMep", val => goiCachMep.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "cgl_DKBTP", val => dkBTP.Value = Convert.ToDecimal(val));
        }
    }
}
