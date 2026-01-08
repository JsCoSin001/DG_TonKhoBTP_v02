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
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.UI.Helper;

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
            cbxDSCongDoan.DataSource = CoreHelper.GetDanhSachCongDoan();
            cbxDSCongDoan.DisplayMember = "TenCongDoan";  // tên hiển thị
            cbxDSCongDoan.ValueMember = "Id";             // giá trị

            cbxCa.DataSource = new List<string> { "1", "2", "3" };
        }

        private async void btnTimKiem_Click(object sender, EventArgs e)
        {
            if (dgKetQua.Columns.Contains("colCheck"))
                dgKetQua.Columns.Remove("colCheck");

            dgKetQua.DataSource = null;

            string nguoiKT = "";
            CongDoan congDoan = cbxDSCongDoan.SelectedItem as CongDoan;
            DateTime ngay = dtNgay.Value;
            int ca = int.Parse(cbxCa.SelectedItem.ToString());

            btnTimKiem.Enabled = false;

            try
            {
                // Truy vấn dữ liệu với waiting form
                DataTable dt = await WaitingHelper.RunWithWaiting(
                    async () => await Task.Run(() => DatabaseHelper.GetDataByCongDoan(ngay, congDoan, ca, nguoiKT)),
                    "ĐANG TÌM KIẾM DỮ LIỆU..."
                );

                // Kiểm tra kết quả sau khi waiting form đóng
                if (dt == null || dt.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert($"KHÔNG TÌM THẤY DỮ LIỆU PHÙ HỢP.");
                    return;
                }

                // Hiển thị dữ liệu
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
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}");
            }
            finally
            {
                btnTimKiem.Enabled = true;
            }
        }

        private async void btnChecked_Click(object sender, EventArgs e)
        {
            string k = EnumStore.Group["KiemTraBaoCaoSX"];            

            if (!CoreHelper.CheckLoginAndPermission(k)) return;

            string nguoiKT = tbNguoiKiemTra.Text.Trim();

            // Kiểm tra người kiểm tra
            if (string.IsNullOrEmpty(nguoiKT))
            {
                FrmWaiting.ShowGifAlert($"VUI LÒNG NHẬP NGƯỜI KIỂM TRA.");
                return;
            }

            // Thu thập danh sách STT được chọn
            HashSet<int> listStt = new HashSet<int>();
            foreach (DataGridViewRow row in dgKetQua.Rows)
            {
                bool isChecked = row.Cells["colCheck"].Value != null
                                 && Convert.ToBoolean(row.Cells["colCheck"].Value);
                if (isChecked)
                {
                    int stt = Convert.ToInt32(row.Cells["stt"].Value);
                    listStt.Add(stt);
                }
            }

            // Kiểm tra danh sách rỗng
            if (listStt.Count == 0)
            {
                FrmWaiting.ShowGifAlert($"CHƯA THẤY NỘI DUNG NÀO ĐƯỢC CHỌN");

                return;
            }

            btnChecked.Enabled = false;

            try
            {
                // Cập nhật với waiting form
                bool flg = await WaitingHelper.RunWithWaiting(
                    async () => await Task.Run(() => DatabaseHelper.UpdateNguoiKiemTra(listStt.ToList(), nguoiKT)),
                    "ĐANG CẬP NHẬT NGƯỜI KIỂM TRA..."
                );

                // Hiển thị kết quả sau khi waiting form đóng
                string message = "THAO TÁC THẤT BẠI.";                
                string icon = EnumStore.Icon.Warning;

                if (flg)
                {
                    message = "THAO TÁC THÀNH CÔNG.";
                    icon = EnumStore.Icon.Success;
                    dgKetQua.DataSource = null;
                    dgKetQua.Rows.Clear();
                    dgKetQua.Columns.Clear(); // ⭐ QUAN TRỌNG
                    cbxCheckAll.Checked = false;
                    tbNguoiKiemTra.Text = "";
                }

                FrmWaiting.ShowGifAlert(message, "THÔNG BÁO", icon);

            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Có lỗi xảy ra: {ex.Message}");
            }
            finally
            {
                btnChecked.Enabled = true;
            }
        }

        private void cbxCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            bool check = cbxCheckAll.Checked; // giá trị người dùng chọn

            foreach (DataGridViewRow row in dgKetQua.Rows)
            {
                // Gán giá trị checked vào cột số 1
                row.Cells[0].Value = check;
            }
            lblCanhBao.Visible = check;
        }
    }
}
