
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTNVL : UserControl, IFormSection
    {
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

            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 40; 

            for (int i = 0; i < headers.Length && i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].HeaderText = headers[i];
                dgv.Columns[i].Width = defaultWidth;
            }           

            dgv.Columns[0].Width = 40;
            dgv.Columns[0].ReadOnly = true;

            dgv.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns[1].ReadOnly = true;

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

        private void tbxTimKiem_TextUpdate(object sender, EventArgs e)
        {
            string keyword = cbxTimKiem.Text.Trim();
            this.LoadAutoCompleteLot(keyword, cbxTimKiem);
        }

        private void LoadAutoCompleteLot(string keyword, ComboBox cbx)
        {

            if (string.IsNullOrWhiteSpace(keyword))
            {
                //ResetController_TimLOT();
                cbx.DroppedDown = false;
                return;
            }
            string para = "Lot";

            string query = @"
                SELECT 
	                t.ID, 
	                t.Lot, 
                    0 as ConLai,
	                dssp.ten as TenSP,
	                t.KhoiLuongConLai as KL
                FROM DL_CD_Ben b
                JOIN TonKho t ON b.TonKho_ID = t.ID
                JOIN DanhSachMaSP dssp ON t.MaSP_ID = dssp.ID
                WHERE t.Lot LIKE '%' || @" + para + @" || '%'
                  AND t.KhoiLuongConLai <> 0
                  AND t.Lot NOT LIKE 'Z_%';";

            //DataTable tonKho = DatabaseHelper.GetData(keyword, query, para);

            //cbx.DroppedDown = false;

            //cbx.SelectionChangeCommitted -= cbx_SelectionChangeCommitted; // tránh trùng event
            //// check data return
            //if (tonKho.Rows.Count == 0) return;

            //cbx.DataSource = tonKho;
            //cbx.DisplayMember = "Lot";

            //string currentText = keyword;

            //cbx.DroppedDown = true;
            //cbx.Text = currentText;
            //cbx.SelectionStart = cbx.Text.Length;
            //cbx.SelectionLength = 0;

            cbx.SelectionChangeCommitted += cbx_SelectionChangeCommitted;


        }

        private void cbx_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ////ResetController_TimLOT();


            ////if (cbTimLot.SelectedItem == null || !(cbTimLot.SelectedItem is DataRowView)) return;

            //DataRowView row = (DataRowView)cbxTimKiem.SelectedItem;

            //string id = row["ID"].ToString();

            ////DataRowView dong = (DataRowView)row;

            //bool isDuplicate = dtgTTNVL.Rows.Cast<DataGridViewRow>()
            //    .Any(r => r.Cells["ID"].Value?.ToString() == id);

            //if (!isDuplicate)
            //{
            //    int index = dtgTTNVL.Rows.Add();
            //    DataGridViewRow newRow = dtgTTNVL.Rows[index];
            //    newRow.Cells["ID"].Value = row["ID"];
            //    newRow.Cells["lot"].Value = row["Lot"];
            //    newRow.Cells["conLai"].Value = row["ConLai"];
            //    newRow.Cells["ten"].Value = row["TenSP"];
            //    newRow.Cells["kl"].Value = row["KL"];
            //}
            //else MessageBox.Show("Lô này đã được thêm vào danh sách.");

            //cbxTimKiem.SelectedIndex = -1;  
            //cbxTimKiem.Text = string.Empty; 
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void cbxTimKiem_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        #region AI generated code for IFormSection

        public string SectionName => nameof(UC_TTNVL);

        // Giả định có control: groupBox1, dtgTTNVL, cbxTimKiem, ...
        // dtgTTNVL cột động: tên cột nên trùng với property của TTNVL (không phân biệt hoa thường).

        public object GetData()
        {
            // [Luồng 2.1] Đảm bảo các cột tối thiểu tồn tại (theo model)
            EnsureColumnsFromModel<TTNVL>(dtgTTNVL);

            // [Luồng 2.2] Duyệt từng hàng -> TTNVL (ô nào null/"" => 0 cho numeric)
            var list = new List<TTNVL>();

            foreach (DataGridViewRow row in dtgTTNVL.Rows)
            {
                if (row.IsNewRow) continue;

                var item = new TTNVL();
                MapRowToObject(row, item);
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

        // ===== Helpers =====

        private static void EnsureColumnsFromModel<T>(DataGridView grid)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var existing = new HashSet<string>(grid.Columns
                                                  .Cast<DataGridViewColumn>()
                                                  .Select(c => c.Name),
                                               StringComparer.OrdinalIgnoreCase);

            foreach (var p in props)
            {
                if (!existing.Contains(p.Name))
                {
                    // Thêm cột động (text/number) đơn giản
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = p.Name,
                        HeaderText = p.Name
                    };
                    grid.Columns.Add(col);
                }
            }
        }

        private static void MapRowToObject<T>(DataGridViewRow row, T target)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                if (!row.DataGridView.Columns.Contains(p.Name))
                    continue;

                var raw = row.Cells[p.Name]?.Value;

                try
                {
                    object value = null;

                    if (p.PropertyType == typeof(string))
                    {
                        value = raw?.ToString() ?? string.Empty;
                    }
                    else if (IsNumeric(p.PropertyType))
                    {
                        // Ô trống => 0
                        var s = raw?.ToString();
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            value = ConvertToNumericDefaultZero(p.PropertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(s, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
                        }
                    }
                    else
                    {
                        // Kiểu khác (int?, double?, …)
                        var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        if (raw == null || string.IsNullOrWhiteSpace(raw.ToString()))
                        {
                            value = underlying.IsValueType ? Activator.CreateInstance(underlying) : null;
                        }
                        else
                        {
                            value = Convert.ChangeType(raw, underlying);
                        }
                    }

                    p.SetValue(target, value);
                }
                catch
                {
                    // Nếu chuyển kiểu lỗi -> gán 0 cho numeric, "" cho string
                    if (p.PropertyType == typeof(string))
                        p.SetValue(target, string.Empty);
                    else if (IsNumeric(p.PropertyType))
                        p.SetValue(target, ConvertToNumericDefaultZero(p.PropertyType));
                }
            }
        }

        private static bool IsNumeric(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(double) || t == typeof(float) || t == typeof(decimal);
        }

        private static object ConvertToNumericDefaultZero(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int)) return 0;
            if (t == typeof(long)) return 0L;
            if (t == typeof(short)) return (short)0;
            if (t == typeof(double)) return 0.0d;
            if (t == typeof(float)) return 0.0f;
            if (t == typeof(decimal)) return 0.0m;
            return 0;
        }

        #endregion
    }

    // Class mô tả cột

}
