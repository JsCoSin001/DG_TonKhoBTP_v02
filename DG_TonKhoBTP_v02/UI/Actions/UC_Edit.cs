using DG_TonKhoBTP_v02.Core;
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
                FrmWaiting.ShowGifAlert("STT KHÔNG HỢP LỆ!");
                return;
            }

            btnTim.Enabled = false;

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    int kieuEdit = cbKieuXuLyDL.SelectedIndex;
                    // Chạy query trên thread nền
                    DataTable dt = await Task.Run(() =>
                        Database.DatabaseHelper.GetDataByID(stt.ToString(), _cd , kieuEdit));

                    // Sau await, ta đã quay lại UI thread (WinForms SynchronizationContext)
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("STT KHÔNG TỒN TẠI!");
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

            int kieuEdit = cbKieuXuLyDL.SelectedIndex;

            if (!string.IsNullOrWhiteSpace(sttLoi.Text))
            {
                value = (int)sttLoi.Value;
            }

            return new EditModel
            {
                Id = value,
                KieuXuLy = kieuEdit,
            };
        }

        public void ClearInputs()
        {
            sttLoi.Value = 0;
            cbKieuXuLyDL.SelectedIndex = 0;
        }


        private void cbKieuXuLyDL_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            int kieuEdit = cb.SelectedIndex;

            sttLoi.Enabled = cb.SelectedIndex > 0 ? true : false;
            sttLoi.Value = 0;
        }

        private void UC_Edit_Load(object sender, EventArgs e)
        {
            cbKieuXuLyDL.SelectedIndex = 0;
        }
    }
}
