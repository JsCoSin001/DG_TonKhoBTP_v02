using DG_TonKhoBTP_v02.Core;
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

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_Edit : UserControl, IFormSection
    {
        CongDoan _cd;


        public event EventHandler<DataTable> DataTableSubmitted;

        public UC_Edit(CongDoan cd)
        {
            InitializeComponent();
            _cd = cd;
        }

        private async void btnTim_Click(object sender, EventArgs e)
        {
            long stt = (long)sttLoi.Value;

            // Kiểm tra input trước, chưa cần waiting form
            if (stt <= 0)
            {
                MessageBox.Show("STT KHÔNG HỢP LỆ!", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTim.Enabled = false;

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    // Chạy query trên thread nền
                    DataTable dt = await Task.Run(() =>
                        Database.DatabaseHelper.GetDataByID(stt.ToString(), _cd));

                    // Sau await, ta đã quay lại UI thread (WinForms SynchronizationContext)
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        MessageBox.Show("STT KHÔNG TỒN TẠI!", "Thông báo",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Raise event / cập nhật UI bình thường
                    DataTableSubmitted?.Invoke(this, dt);

                }, "ĐANG TÌM KIẾM, VUI LÒNG ĐỢI...");
            }
            finally
            {
                btnTim.Enabled = true;
            }
        }


        public string SectionName => nameof(UC_Edit);

        public object GetData()
        {
            int value = 0;

            if (!string.IsNullOrWhiteSpace(sttLoi.Text))
            {
                value = (int)sttLoi.Value;
            }

            return new EditModel
            {
                Id = value
            };
        }

        public void ClearInputs()
        {
            sttLoi.Value = 0;
        }
    }
}
