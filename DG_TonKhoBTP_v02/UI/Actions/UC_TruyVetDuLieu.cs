using ClosedXML.Excel;
using DG_TonKhoBTP_v02.Database;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Actions
{
    public partial class UC_TruyVetDuLieu : UserControl
    {
        private CancellationTokenSource _searchCts;
        string selectedCol;
        public UC_TruyVetDuLieu()
        {
            InitializeComponent();
            cbxLoaiTimKiem.SelectedIndex = 0;
        }

        private async void cbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            string searchData = cbxTimKiem.Text;

            if (string.IsNullOrEmpty(searchData)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(searchData, token);
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
                cbxTimKiem.DroppedDown = false;
                return;
            }
            string para = "key";
            bool stt = (cbxLoaiTimKiem.SelectedIndex > 1);

            string query = Helper.Helper.TaoSQL_LayDLTruyVet(stt, para, out selectedCol);

            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(query, keyword, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimKiem.DroppedDown = false;

            cbxTimKiem.SelectionChangeCommitted -= cbxTimKiem_SelectionChangeCommitted; // tránh trùng event
            if (sp.Rows.Count == 0) return;

            cbxTimKiem.DataSource = sp;
            cbxTimKiem.DisplayMember = selectedCol;

            string currentText = keyword;

            cbxTimKiem.DroppedDown = true;
            cbxTimKiem.Text = currentText;
            cbxTimKiem.SelectionStart = cbxTimKiem.Text.Length;
            cbxTimKiem.SelectionLength = 0;

            cbxTimKiem.SelectionChangeCommitted += cbxTimKiem_SelectionChangeCommitted;
        }

        private void cbxLoaiTimKiem_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;
        }

        private void cbxTimKiem_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Bỏ qua nếu không có item được chọn
            if (cbxTimKiem.SelectedItem == null)
                return;

            // Lấy DataRowView từ item được chọn
            if (cbxTimKiem.SelectedItem is DataRowView selectedRow)
            {
                // Tạo DataTable mới chỉ chứa dòng được chọn
                DataTable dtSelected = selectedRow.Row.Table.Clone(); // Copy cấu trúc
                dtSelected.ImportRow(selectedRow.Row); // Import dòng được chọn

                // Gán vào DataGridView
                if (grvChiTietThanhPham.DataSource is BindingSource bsGrid)
                {
                    bsGrid.DataSource = dtSelected;
                }
                else
                {
                    grvChiTietThanhPham.DefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                    grvChiTietThanhPham.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                    grvChiTietThanhPham.DataSource = dtSelected;
                }

                // Tự động chọn dòng đầu tiên (vì chỉ có 1 dòng)
                if (grvChiTietThanhPham.Rows.Count > 0)
                {
                    grvChiTietThanhPham.ClearSelection();

                    string maBin = grvChiTietThanhPham.Rows[0].Cells["mabin"].Value.ToString();
                    getSelectedCol(maBin, grvChiTietNVL, true);
                }
            }

            // Reset lại ComboBox để sẵn sàng cho lần tìm tiếp theo
            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;
        }

        private void grvChiTietThanhPham_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = grvChiTietThanhPham.Rows[e.RowIndex];
            string cellValue = row.Cells["MaBin"].Value.ToString();

            getSelectedCol(cellValue, grvChiTietNVL, true);
        }

        private void grvChiTietNVL_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0) return;

            DataGridViewRow row = grvChiTietNVL.Rows[e.RowIndex];
            string cellValue = row.Cells["MaBin"].Value.ToString();

            getSelectedCol(cellValue, grvChiTietThanhPham,  false);
            getSelectedCol(cellValue, grvChiTietNVL, true);
        }

        private void getSelectedCol(string maBin, DataGridView dgrDich, bool loai_MaBin = true)
        {
            if (maBin != "")
            {
                DataTable dlNVVL;

                if (loai_MaBin)
                {
                    dlNVVL = DatabaseHelper.GetThongTinNVLTheoMaBin(maBin);
                }
                else
                {
                    string para = "key";
                    string query = Helper.Helper.TaoSQL_LayDLTruyVet(loai_MaBin, para, out selectedCol);
                    dlNVVL = DatabaseHelper.GetData(query, maBin, para);
                }

                dgrDich.AutoGenerateColumns = true;
                dgrDich.DefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);
                dgrDich.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Tahoma", 11F, FontStyle.Regular);

                dgrDich.DataSource = dlNVVL;
            }
        }
    }
}
