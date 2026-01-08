using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
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

        private async void btnShowBaoCao_Click(object sender, EventArgs e)
        {
            DateTime selected = ngayBC.Value;

            btnShowBaoCao.Enabled = false;

            try
            {
                // Lấy dữ liệu với waiting form
                DataTable dt = await WaitingHelper.RunWithWaiting(
                    async () => await Task.Run(() => DatabaseHelper.GetDataByMonth(selected, CongDoan)),
                    "ĐANG TẢI BÁO CÁO THÁNG " + selected.ToString("MM/yyyy") + "..."
                );

                // Kiểm tra kết quả
                if (dt == null || dt.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("THÁNG " + selected.ToString("MM/yyyy") + " KHÔNG CÓ DỮ LIỆU!", "LỖI");
                    return;
                }

                // Hiển thị báo cáo
                UC_MonthyReport fBaoCao = new UC_MonthyReport();
                fBaoCao.LoadData(dt);
                fBaoCao.ShowDialog();
            }
            finally
            {
                btnShowBaoCao.Enabled = true;
            }
        }
    }
}
