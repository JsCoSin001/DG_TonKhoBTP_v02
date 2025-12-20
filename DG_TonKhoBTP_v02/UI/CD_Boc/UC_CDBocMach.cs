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
    public partial class UC_CDBocMach : UserControl, ISectionProvider<CD_BocMach>, IDataReceiver
    {
        public UC_CDBocMach()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDBocMach);

        public CD_BocMach GetSectionData()
        {
            return new CD_BocMach
            {
                TTThanhPhan_ID = 0, // bind khi lưu DB
                NgoaiQuan = string.IsNullOrEmpty(ngoaiQuan.Text) ? null : ngoaiQuan.Text,
                LanDanhThung = lanDanhThung.Value == 0m ? null : (int?)lanDanhThung.Value,
                SoMet = soMet.Value == 0m ? null : (double?)soMet.Value,
            };
        }

        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "BocMach_NgoaiQuan", val => ngoaiQuan.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "LanDanhThung", val => lanDanhThung.Value = Convert.ToDecimal(val));
            Helper.Helper.SetIfPresent(row, "SoMet", val => soMet.Text = Convert.ToString(val));

        }
    }
}
