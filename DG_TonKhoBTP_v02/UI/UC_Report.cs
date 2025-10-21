using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ClosedXML.Excel.XLPredefinedFormat;
using DateTime = System.DateTime;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_Report : UserControl
    {
        public CongDoan CongDoan { get; private set; }
        public UC_Report(CongDoan congDoan)
        {
            InitializeComponent();
            CongDoan = congDoan;
        }

        private void btnShowBaoCao_Click(object sender, EventArgs e)
        {
            DateTime selected = ngayBC.Value;
            DataTable dt = DatabaseHelper.GetDataByMonth(selected, CongDoan);
            UC_MonthyReport fBaoCao = new UC_MonthyReport();
            fBaoCao.LoadData(dt);
            fBaoCao.ShowDialog();
        }
    }
}
