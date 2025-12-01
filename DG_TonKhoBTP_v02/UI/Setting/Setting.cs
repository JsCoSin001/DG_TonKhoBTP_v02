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
            cbxPrinterName.Text = _printerName;

            LoadConfigAndShow();

        }

        public void LoadConfigAndShow()
        {
            try
            {
                ConfigDB config = DatabaseHelper.GetConfig();
                cbxActive.SelectedIndex = config.Active ? 1 : 0;
                rtbMessage.Text = config.Message;
                lblThongBao.Visible = config.Active;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc dữ liệu từ database!\nChi tiết lỗi: " + ex.Message, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        }

        private void btnLuuPrinter_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;
            string printerName = cbxPrinterName.Text;
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Vui lòng chọn tên máy in.", "CẢNH BÁO");
                return;
            }

            Properties.Settings.Default.PrinterName = printerName;
            Properties.Settings.Default.Save();

            MessageBox.Show("Ứng dụng sẽ được khởi động lại để áp dụng thay đổi.", "THÔNG BÁO");
            Application.Restart();

        }

        private void cbxActive_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblThongBao.Visible = cbxActive.SelectedIndex == 1;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;

            string rtbMsg = rtbMessage.Text.Trim();
            if (rtbMsg.Length == 0 && cbxActive.SelectedIndex == 1)
            {
                DialogResult result = MessageBox.Show(
                    "LỜI NHẮC KHÔNG ĐƯỢC BỎ TRỐNG!",
                    "CẢNH BÁO",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return  ;
            }

            ConfigDB config = new ConfigDB
            {
                Active = cbxActive.SelectedIndex == 1,
                Message = rtbMsg
            };

            DatabaseHelper.UpdateConfig(config);
        }
    }
}
