using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
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

namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class UC_KTraBC : UserControl
    {
        public UC_KTraBC()
        {
            InitializeComponent();;
            LoadCongDoanComboBox();
        }

        private void LoadCongDoanComboBox()
        {
            cbxDSCongDoan.DataSource = Helper.Helper.GetDanhSachCongDoan();
            cbxDSCongDoan.DisplayMember = "TenCongDoan";  // tên hiển thị
            cbxDSCongDoan.ValueMember = "Id";             // giá trị

            cbxCa.DataSource = new List<string> { "1", "2", "3" };
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            if (dgKetQua.Columns.Contains("colCheck"))
                dgKetQua.Columns.Remove("colCheck");

            dgKetQua.DataSource = null;

            string nguoiKT = "";
            CongDoan congDoan = cbxDSCongDoan.SelectedItem as CongDoan;
            DateTime ngay = dtNgay.Value;
            int ca = int.Parse(cbxCa.SelectedItem.ToString());

            DataTable dt = DatabaseHelper.GetDataByCongDoan(ngay, congDoan, ca, nguoiKT);

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("KHÔNG TÌM THẤY DỮ LIỆU PHÙ HỢP.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            dgKetQua.DataSource = dt;

            // Thêm cột checkbox nếu chưa có
            if (!dgKetQua.Columns.Contains("colCheck"))
            {
                DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
                chk.HeaderText = "Đã Kiểm Tra";
                chk.Name = "colCheck";
                dgKetQua.Columns.Insert(0, chk);
            }

            foreach (DataGridViewColumn col in dgKetQua.Columns)
            {
                if (col.Name == "colCheck")
                    col.ReadOnly = false;
                else
                    col.ReadOnly = true;
            }
        }

        private void btnChecked_Click(object sender, EventArgs e)
        {
            string nguoiKT = tbNguoiKiemTra.Text.Trim();

            // check nguoi kiem tra empty
            if (string.IsNullOrEmpty(nguoiKT))
            {
                MessageBox.Show("VUI LÒNG NHẬP NGƯỜI KIỂM TRA.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HashSet<int> listStt = new HashSet<int>();

            foreach (DataGridViewRow row in dgKetQua.Rows)
            {
                bool isChecked = row.Cells["colCheck"].Value != null
                                 && Convert.ToBoolean(row.Cells["colCheck"].Value);

                if (isChecked)
                {
                    int stt = Convert.ToInt32(row.Cells["stt"].Value);
                    listStt.Add(stt);   // HashSet tự loại bỏ giá trị trùng
                }
            }

            // kiểm tra danh sách rỗng
            if (listStt.Count == 0)
            {
                MessageBox.Show("CHƯA THẤY NỘI DUNG NÀO ĐƯỢC CHỌN.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool flg = DatabaseHelper.UpdateNguoiKiemTra(listStt.ToList(), nguoiKT);

            string message = "THAO TÁC";
            message += flg ? " THÀNH CÔNG." : " THẤT BẠI";

            MessageBox.Show(message, "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void cbxCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            bool check = cbxCheckAll.Checked; // giá trị người dùng chọn

            foreach (DataGridViewRow row in dgKetQua.Rows)
            {
                // Gán giá trị checked vào cột số 1
                row.Cells[0].Value = check;
            }
        }
    }
}
