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
                    FrmWaiting.ShowGifAlert("Không thể đọc dữ liệu từ database!\nChi tiết lỗi: " + loadException.Message);
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
                FrmWaiting.ShowGifAlert("DATABASE CHƯA ĐƯỢC CHỌN");
                return;
            }

            Properties.Settings.Default.URL = result;

            Properties.Settings.Default.Save();

            MessageBox.Show("ỨNG DỤNG SẼ KHỞI ĐỘNG LẠI ĐỂ ÁP DỤNG THAY ĐỔI", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
         
            Program.RestartApplication();
        }

        private void btnPhanQuyen_Click(object sender, EventArgs e)
        {
            string pass = tbQuyenUser.Text.Trim();
            Helper.Helper.UpdatePassApp(pass);
            MessageBox.Show("ỨNG DỤNG SẼ KHỞI ĐỘNG LẠI ĐỂ ÁP DỤNG THAY ĐỔI", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Program.RestartApplication();
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
                    FrmWaiting.ShowGifAlert("CHỌN TÊN MÁY IN");
                    return;
                }
            }

            Properties.Settings.Default.PrinterName = printerName;
            Properties.Settings.Default.Save();

            MessageBox.Show("ỨNG DỤNG SẼ KHỞI ĐỘNG LẠI ĐỂ ÁP DỤNG THAY ĐỔI", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.RestartApplication();

        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (!Helper.Helper.kiemTraPhanQuyen(_quyenMaster)) return;

            string rtbMsg = rtbMessage.Text.Trim();

            if (tbxNguoiThucHien.Text.Trim() == "")
            {
                FrmWaiting.ShowGifAlert("NGƯỜI THỰC HIỆN KHÔNG ĐƯỢC BỎ TRỐNG.");
                return;
            }

            if (rdoTamDung.Checked && rtbMsg.Length == 0)
            {
                FrmWaiting.ShowGifAlert("CHƯA CÓ LỜI NHẮC.");
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
            string icon = EnumStore.Icon.Warning;

            if (flg)
            {
                ms =  "THAO TÁC THÀNH CÔNG";
                icon = EnumStore.Icon.Success;
                tbxNguoiThucHien.Text = "";
            }

            FrmWaiting.ShowGifAlert(ms,"THÔNG BÁO", icon);
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

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {            
            TabPage selectedTab = e.TabPage;

            if (selectedTab.Name == "pinterTab") {

                Helper.Helper.LoadPrinters(cbxPrinterName);

                cbxMayIn.Checked = _printerName != "";
                cbxPrinterName.Enabled = cbxMayIn.Checked;
            }

        }
    }
}
