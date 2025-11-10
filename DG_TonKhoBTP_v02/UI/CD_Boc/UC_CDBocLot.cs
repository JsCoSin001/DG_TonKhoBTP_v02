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
    public partial class UC_CDBocLot : UserControl, ISectionProvider<CD_BocLot>, IDataReceiver
    {
        public UC_CDBocLot()
        {
            InitializeComponent();
        }


        #region AI generated

        public string SectionName => nameof(UC_CDBocLot);

        public CD_BocLot GetSectionData()
        {
            return new CD_BocLot
            {
                TTThanhPhan_ID = 0,
                DoDayTBLot = (double)doDayTBLot.Value
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "DoDayTBLot", val => doDayTBLot.Value = Convert.ToDecimal(val));

        }
        #endregion
    }
}
