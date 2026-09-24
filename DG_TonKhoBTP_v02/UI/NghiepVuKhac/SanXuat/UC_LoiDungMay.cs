using System;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    /// <summary>
    /// Màn hình lỗi dừng máy kiểu cũ đã ngừng dùng sau khi nghiệp vụ được
    /// chuyển sang Frm_BCDungMay và liên kết theo TTThanhPham_ID.
    /// Giữ UserControl này để không phá menu/cấu trúc form cũ.
    /// </summary>
    public partial class UC_LoiDungMay : UserControl
    {
        public UC_LoiDungMay()
        {
            InitializeComponent();
            Load += UC_LoiDungMay_Load;
        }

        private void UC_LoiDungMay_Load(object sender, EventArgs e)
        {
            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            btnLuu.Enabled = false;
            button1.Enabled = false;

            label1.Text = "BÁO CÁO DỪNG MÁY ĐÃ CHUYỂN VÀO MÀN HÌNH THÔNG TIN THÀNH PHẨM";
            label1.AutoSize = false;
            label1.Dock = DockStyle.Top;
            label1.Height = 42;
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Báo cáo dừng máy đã chuyển sang nút BCDungMay tại màn hình thông tin thành phẩm.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
