
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTNVL : UserControl, IFormSection
    {
        private CancellationTokenSource _searchCts;

        // Nếu true thì tìm Cu phi 8...
        public bool RawMaterial { get; set; } = false;
        public void SetStatusRawMaterial(bool value) => RawMaterial = value;

        public UC_TTNVL(List<ColumnDefinition> columns)
        {
            InitializeComponent();

            TaoBang(columns);

            // Bắt lỗi nhập sai định dạng
            dtgTTNVL.DataError += dtgTTNVL_DataError;

            // Hạn chế nhập ký tự không hợp lệ cho các cột số
            dtgTTNVL.EditingControlShowing += dtgTTNVL_EditingControlShowing;
        }

        private void TaoBang(List<ColumnDefinition> columns)
        {
            DataTable dt = new DataTable("ThongTin");

            // Tạo cột từ danh sách
            foreach (var col in columns) dt.Columns.Add(col.Name, col.DataType);

            dtgTTNVL.DataSource = dt;

            // Tạo mảng headers từ danh sách truyền vào
            string[] headers = columns.Select(c => c.Header).ToArray();

            // Gọi lại hàm SetColumnHeaders để cấu hình
            SetColumnHeaders(dtgTTNVL, headers);

            // Tuỳ chỉnh style
            dtgTTNVL.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12, FontStyle.Regular);
            dtgTTNVL.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dtgTTNVL.RowTemplate.Height = 30;

            // Thêm cột nút Xoá
            if (!dtgTTNVL.Columns.Contains("Delete"))
            {
                DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
                btnDelete.Name = "Delete";
                btnDelete.HeaderText = "";
                btnDelete.Text = "Xoá";
                btnDelete.UseColumnTextForButtonValue = true;
                btnDelete.Width = 60;
                dtgTTNVL.Columns.Add(btnDelete);
            }

            dtgTTNVL.CellClick += dtgTTNVL_CellClick;
        }

        private void dtgTTNVL_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dtgTTNVL.Columns["Delete"].Index)
            {
                // Xác nhận xoá
                var confirm = MessageBox.Show("Bạn có chắc muốn xoá dòng này?",
                                              "Xác nhận xoá",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dtgTTNVL.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        private void SetColumnHeaders(DataGridView dgv, string[] headers)
        {

            int defaultWidth = 105;
            int defaulHeight = 40;

            // Sẽ thay đổi nội dung này sau
            bool isShow = true;

            if (headers.Length > 5)
            {
                defaultWidth = 90;
                defaulHeight = 50;
            }

            dgv.ColumnHeadersHeight = defaulHeight;

            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            for (int i = 0; i < headers.Length && i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].HeaderText = headers[i];
                dgv.Columns[i].Width = defaultWidth;
            }

            dgv.Columns[0].Width = 80;
            dgv.Columns[0].Visible = isShow;

            dgv.Columns[1].Width = 80;
            dgv.Columns[1].Visible = isShow;
            
            dgv.Columns[2].Width = 80;
            dgv.Columns[2].Visible = isShow;

            dgv.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns[3].ReadOnly = true;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 11, FontStyle.Regular);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void dtgTTNVL_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true; // Giữ ô để sửa

            string colName = ((DataGridView)sender).Columns[e.ColumnIndex].HeaderText;

            if (e.Exception is FormatException)
            {
                MessageBox.Show(
                    $"Giá trị không hợp lệ ở cột \"{colName}\". Vui lòng nhập số hợp lệ.",
                    "Lỗi định dạng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra: " + e.Exception.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void dtgTTNVL_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dtgTTNVL.CurrentCell.OwningColumn.ValueType == typeof(double) ||
                dtgTTNVL.CurrentCell.OwningColumn.ValueType == typeof(int))
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= OnlyNumber_KeyPress;
                    tb.KeyPress += OnlyNumber_KeyPress;
                }
            }
        }

        private void OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            char dec = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != dec)
                e.Handled = true;

            // chỉ cho phép 1 dấu thập phân
            if (sender is TextBox tb && e.KeyChar == dec && tb.Text.Contains(dec))
                e.Handled = true;
        }

        private async void tbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            //ResetController_TimTenSP();
            string tenNL = cbxTimKiem.Text;

            if (string.IsNullOrEmpty(tenNL)) return;

            // --- thêm debounce + cancel ---
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // debounce: đợi user dừng gõ 250ms mới chạy
                await Task.Delay(250, token);

                // gọi async thay vì sync
                await ShowDanhSachLuaChon(tenNL, token);
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
            string para = "ten";
            string query;

            if (RawMaterial)
            {
                query = Helper.Helper.TaoSQL_LayDLNVL_TTThanhPham();
            }
            else
            {
                query = Helper.Helper.TaoSQL_LayDLTTThanhPham();
            }

            // --- sửa: chạy query trong Task.Run để không block UI ---
            DataTable sp = await Task.Run(() =>
            {
                return DatabaseHelper.GetData(keyword, query, para);
            }, ct);

            ct.ThrowIfCancellationRequested();

            cbxTimKiem.DroppedDown = false;

            cbxTimKiem.SelectionChangeCommitted -= cbxTimKiem_SelectionChangeCommitted; // tránh trùng event
            if (sp.Rows.Count == 0) return;

            cbxTimKiem.DataSource = sp;
            cbxTimKiem.DisplayMember = "BinNVL";

            string currentText = keyword;

            cbxTimKiem.DroppedDown = true;
            cbxTimKiem.Text = currentText;
            cbxTimKiem.SelectionStart = cbxTimKiem.Text.Length;
            cbxTimKiem.SelectionLength = 0;

            cbxTimKiem.SelectionChangeCommitted += cbxTimKiem_SelectionChangeCommitted;
        }

        private void cbxTimKiem_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbxTimKiem.SelectedItem == null || !(cbxTimKiem.SelectedItem is DataRowView sel))
                return;

            // Lấy DataTable đang bind với DataGridView (kể cả khi dùng BindingSource)
            DataTable table = null;
            BindingSource bs = dtgTTNVL.DataSource as BindingSource;
            if (bs != null)
                table = bs.DataSource as DataTable;
            else
                table = dtgTTNVL.DataSource as DataTable;

            if (table == null)
            {
                MessageBox.Show("DataGridView chưa bind với DataTable.");
                return;
            }


            // Chống trùng theo 'id'
            string key = sel["id"] == DBNull.Value ? string.Empty : Convert.ToString(sel["id"]);
            bool exists = table.AsEnumerable().Any(r => (r["id"] == DBNull.Value ? string.Empty : Convert.ToString(r["id"])) == key);
            if (exists) return;

            // Tạo dòng mới và gán giá trị
            DataRow newRow = table.NewRow();
            newRow["KlBatDau"] = sel["KlBatDau"];
            newRow["CdBatDau"] = sel["CdBatDau"];
            newRow["id"] = sel["id"];
            newRow["BinNVL"] = sel["BinNVL"];
            table.Rows.Add(newRow);

            // Chọn & cuộn đến dòng vừa thêm (tuỳ chọn)
            int addedIndex = table.Rows.IndexOf(newRow);
            if (addedIndex >= 0 && addedIndex < dtgTTNVL.Rows.Count)
            {
                dtgTTNVL.ClearSelection();
                dtgTTNVL.Rows[addedIndex].Selected = true;
                dtgTTNVL.FirstDisplayedScrollingRowIndex = addedIndex;
            }

            cbxTimKiem.SelectedIndex = -1;
            cbxTimKiem.Text = string.Empty;
        }

        #region AI generated code for IFormSection

        public string SectionName => nameof(UC_TTNVL);

        // Giả định có control: groupBox1, dtgTTNVL, cbxTimKiem, ...
        // dtgTTNVL cột động: tên cột nên trùng với property của TTNVL (không phân biệt hoa thường).

        public object GetData()
        {
            var list = new List<TTNVL>();

            foreach (DataGridViewRow row in dtgTTNVL.Rows)
            {
                if (row.IsNewRow) continue;

                var item = new TTNVL();
                Helper.Helper.MapRowToObject(row, item);
                list.Add(item);
            }

            return list;
        }

        public void ClearInputs()
        {
            // Xoá dữ liệu theo cách an toàn cho mọi kiểu bind
            DataGridViewUtils.ClearSmart(dtgTTNVL);

            // Reset control khác nếu cần
            if (cbxTimKiem != null)
            {
                //cbxTimKiem.SelectedIndex = -1;
                //cbxTimKiem.Text = string.Empty;
            }
        }
        #endregion
    }


}
