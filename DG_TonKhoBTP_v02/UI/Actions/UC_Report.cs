using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                List<MonthyReport_Model> rows = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() => MonthyReport_DB.GetMonthlyReport(selected, CongDoan.Id)),
                    "ĐANG TẢI BÁO CÁO THÁNG " + selected.ToString("MM/yyyy") + "..."
                );

                if (IsDisposed || Disposing) return;
                if (rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("THÁNG " + selected.ToString("MM/yyyy") + " KHÔNG CÓ DỮ LIỆU!", "THÔNG BÁO");
                    return;
                }

                using (var report = new UC_MonthyReport())
                {
                    report.LoadData(rows, CongDoan);
                    report.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                    FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}", "LỖI");
            }
            finally
            {
                if (!IsDisposed && !Disposing) btnShowBaoCao.Enabled = true;
            }
        }
    }
}
