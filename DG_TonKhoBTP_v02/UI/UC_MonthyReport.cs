using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public class UC_MonthyReport : Form
    {
        private DataGridView dataGridView1;

        public UC_MonthyReport()
        {
            SetupForm();
            SetupGrid();
        }

        private void SetupForm()
        {
            this.Text = "Báo cáo sản xuất theo tháng";
            this.StartPosition = FormStartPosition.CenterScreen;

            // Chỉ cho phép đóng, không minimize/maximize/resize
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = true;
            this.ShowIcon = false;
            this.Padding = new Padding(5);

            // Full màn hình (tuỳ bạn giữ hoặc bỏ)
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupGrid()
        {
            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dataGridView1.RowTemplate.Height = 35;
            this.Controls.Add(dataGridView1);
        }

        public void LoadData(DataTable table)
        {
            if (table == null)
            {
                MessageBox.Show("Không có dữ liệu để hiển thị!", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            dataGridView1.DataSource = table;
        }
    }
}
