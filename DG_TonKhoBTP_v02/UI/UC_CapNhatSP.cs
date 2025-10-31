using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CapNhatSP : UserControl
    {

        private CancellationTokenSource _searchCts;

        string table = "Ma";
        public UC_CapNhatSP()
        {
            InitializeComponent();
            cbxLoaiTimKiem.SelectedItem = cbxLoaiTimKiem.Items[0];
            cbxLoaiSP.SelectedItem = cbxLoaiSP.Items[1];
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            btnLuu.Enabled = false;

            try
            {
                if (kieuSP.SelectedItem == null)
                {
                    MessageBox.Show("KIỂU SẢN PHẨM KHÔNG HỢP LỆ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DanhSachMaSP sp = new DanhSachMaSP();
                sp.Ma = ma.Text.Trim().ToUpper();
                sp.Ten = ten.Text.Trim().ToUpper();
                sp.KieuSP = kieuSP.Text.Trim().ToUpper(); ;
                sp.DonVi = donVi.Text.ToString().ToUpper(); 
                sp.DateInsert = DateTime.Now;

                if (string.IsNullOrEmpty(sp.Ma) || string.IsNullOrEmpty(sp.Ten) || string.IsNullOrEmpty(sp.DonVi))
                {
                    MessageBox.Show("THIẾU DỮ LIỆU.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string result = string.IsNullOrEmpty(cbxMaSP.Text)
                    ? DatabaseHelper.InsertDSMaSP(sp)
                    : DatabaseHelper.UpdateDanhSachMaSP(sp, int.Parse(id.Text));

                if (string.IsNullOrEmpty(result))
                {
                    MessageBox.Show("THAO TÁC THÀNH CÔNG", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                }
                else
                    MessageBox.Show(result, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLuu.Enabled = true;
            }           
        }

        private void Clear()
        {
            ma.Text = "";
            ten.Text = "";
            kieuSP.SelectedItem = null;
            donVi.SelectedItem = null;
            cbxMaSP.Text = "";
            id.Text = "";
        }

        private void ma_TextChanged(object sender, EventArgs e)
        {
            string maSP = ma.Text.Trim();
            kieuSP.SelectedItem = null;

            if (!string.IsNullOrEmpty(maSP))
            {
                Console.WriteLine(maSP);
                kieuSP.Text = maSP.Split('.')[0];
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void btnShowList_Click(object sender, EventArgs e)
        {
            int loaiSP = cbxLoaiSP.SelectedIndex;
            string query = "SELECT * FROM DanhSachMaSP";

            DataTable dt = new DataTable();

            string col = null;

            if (loaiSP != 3)
            {
                col = cbxLoaiSP.Items[loaiSP].ToString();
                query += " WHERE KieuSP = @KieuSP";                
            }
            query += " ORDER BY id DESC";

            dt = DatabaseHelper.GetData(query, col, "KieuSP");
            grvDanhSach.DataSource = dt;
            grvDanhSach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grvDanhSach.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Regular);

            grvDanhSach.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold);
            grvDanhSach.Columns[0].Width = 100;
            grvDanhSach.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grvDanhSach.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        

        }        

        private async void cbxMaSP_TextUpdate(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            string tenTP = cbxMaSP.Text.Trim();
            if (string.IsNullOrEmpty(tenTP)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(tenTP, token);
            }
            catch (OperationCanceledException)
            {
                // bị huỷ vì user gõ tiếp, bỏ qua
            }
        }

        private async Task ShowDanhSachLuaChon(string keyword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                cbxMaSP.DroppedDown = false;
                return;
            }


            string table = cbxLoaiTimKiem.SelectedIndex == 0 ? "Ma" : "Ten";

            string query = "SELECT * FROM DanhSachMaSP " +
               "WHERE " + table + " LIKE '%' || @Key || '%' " +
               "ORDER BY DateInsert DESC ";

            string para = "Key";


            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxMaSP.DroppedDown = false;

            cbxMaSP.SelectionChangeCommitted -= GanGiaTri_SelectionChangeCommitted;
            if (sp.Rows.Count == 0) return;

            cbxMaSP.DataSource = sp;
            cbxMaSP.DisplayMember = table;

            string currentText = keyword;

            cbxMaSP.DroppedDown = true;
            cbxMaSP.Text = currentText;
            cbxMaSP.SelectionStart = cbxMaSP.Text.Length;
            cbxMaSP.SelectionLength = 0;

            cbxMaSP.SelectionChangeCommitted += GanGiaTri_SelectionChangeCommitted;
        }

        private void GanGiaTri_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxMaSP.SelectedItem == null || !(cbxMaSP.SelectedItem is DataRowView)) return;
            DataRowView row = (DataRowView)cbxMaSP.SelectedItem;

            ten.Text = row["ten"].ToString();
            ma.Text = row["ma"].ToString();
            donVi.Text = row["donVi"].ToString();
            kieuSP.Text = row["kieuSP"].ToString();

            cbxMaSP.SelectedIndex = -1;
            cbxMaSP.Text = string.Empty;
        }
    }
}
