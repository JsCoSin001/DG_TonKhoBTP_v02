using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Setting
{
    public partial class Setting : Form
    {

        private static readonly string _quyenMaster = Properties.Settings.Default.UserPass;
        private static readonly string _url = Properties.Settings.Default.URL;
        private static readonly string _printerName = Properties.Settings.Default.PrinterName;

        public Setting()
        {
            InitializeComponent();
            tbPathDB.Text = _url;
            Helper.Helper.LoadPrinters(cbxPrinterName);            

            cbxMayIn.Checked = _printerName != "";
            cbxPrinterName.Enabled = cbxMayIn.Checked;
        }

        public async Task LoadDataAsync()
        {
            await WaitingHelper.RunWithWaiting(async () =>
            {
                ConfigDB config = null;
                Exception loadException = null;

                // Chạy việc đọc database trong background thread
                await Task.Run(() =>
                {
                    try
                    {
                        config = DatabaseHelper.GetConfig();
                    }
                    catch (Exception ex)
                    {
                        loadException = ex;
                    }
                });

                // Cập nhật UI trên UI thread (không cần Invoke vì đã ở UI thread)
                if (loadException != null)
                {
                    MessageBox.Show("Không thể đọc dữ liệu từ database!\nChi tiết lỗi: " + loadException.Message,
                        "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (config != null)
                {
                    //cbxActive.SelectedIndex = config.Active ? 1 : 0;
                    rdoHoatDong.Checked = config.Active;
                    rdoTamDung.Checked = !config.Active;

                    tbxNguoiThucHien.Text = config.Author.ToString();
                    rtbMessage.Text = config.Message;
                    lblThongBao.Visible = !config.Active;
                }

            }, "ĐANG LẤY DỮ LIỆU...");
        }

        private void btnFindPathDB_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;
            string result = Helper.Helper.SetURLDatabase();

            if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show("DATABASE CHƯA ĐƯỢC CHỌN.", "THÔNG BÁO");
                return;
            }

            Properties.Settings.Default.URL = result;

            Properties.Settings.Default.Save();

            MessageBox.Show("Ứng dụng sẽ được khởi động lại để áp dụng thay đổi.", "THÔNG BÁO");
            Application.Restart();
        }

        private void btnPhanQuyen_Click(object sender, EventArgs e)
        {
            string pass = tbQuyenUser.Text.Trim();
            Helper.Helper.UpdatePassApp(pass);

            MessageBox.Show("Ứng dụng sẽ được khởi động lại để áp dụng thay đổi.", "THÔNG BÁO");
            Application.Restart();
        }

        private void btnLuuPrinter_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;

            string printerName = "";

            if (cbxMayIn.Checked)
            {
                printerName = cbxPrinterName.Text;

                if (string.IsNullOrEmpty(printerName))
                {
                    MessageBox.Show("Vui lòng chọn tên máy in.", "CẢNH BÁO");
                    return;
                }
            }

            Properties.Settings.Default.PrinterName = printerName;
            Properties.Settings.Default.Save();

            MessageBox.Show("Ứng dụng sẽ được khởi động lại để áp dụng thay đổi.".ToUpper(), "THÔNG BÁO");
            Application.Restart();

        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;

            string rtbMsg = rtbMessage.Text.Trim();

            if (tbxNguoiThucHien.Text.Trim() == "")
            {
                DialogResult result = MessageBox.Show(
                    "NGƯỜI THỰC HIỆN KHÔNG ĐƯỢC BỎ TRỐNG.",
                    "CẢNH BÁO",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (rdoTamDung.Checked && rtbMsg.Length == 0)
            {
                DialogResult result = MessageBox.Show(
                    "LỜI NHẮC ĐANG BỊ BỎ TRỐNG.",
                    "CẢNH BÁO",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return  ;
            }

            ConfigDB config = new ConfigDB
            {
                Active = rdoHoatDong.Checked,
                Author = tbxNguoiThucHien.Text.Trim(),
                Message = rtbMsg,
                Ngay = DateTime.Now.ToString("yyyy/MM/dd HH:mm")
            };

            bool flg = DatabaseHelper.InsertConfig(config);
            string ms = "THAO TÁC THẤT BẠI.";

            if (flg)
            {
                ms =  "THAO TÁC THÀNH CÔNG";
                tbxNguoiThucHien.Text = "";
            }

            MessageBox.Show(ms, "THÔNG BÁO");
        }

        private void rdoHoatDong_CheckedChanged(object sender, EventArgs e)
        {
            lblThongBao.Visible = rdoTamDung.Checked;
            tbxNguoiThucHien.Text = "";
        }

        private void cbxMayIn_CheckedChanged(object sender, EventArgs e)
        {
            cbxPrinterName.Enabled = cbxMayIn.Checked;
        }
    }
}
