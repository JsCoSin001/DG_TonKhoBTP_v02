using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class GridBuilder
    {
        public static void ApplySchema(
        DataGridView grid,
        ColumnDef[] schema,
        bool allowNullForCombos = false,
        string nullDisplayText = "— Chọn —")
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            foreach (var def in schema)
            {
                DataGridViewColumn col;

                // 1) Nếu có Options => ComboBox
                if (def.Options != null && def.Options.Length > 0)
                {
                    // Bool + Options: map nhãn -> true/false (2 nhãn)
                    if (def.DataType == typeof(bool) || def.DataType == typeof(Boolean))
                    {
                        var items = new List<KeyValuePair<string, bool?>>();
                        if (allowNullForCombos)
                            items.Add(new KeyValuePair<string, bool?>(nullDisplayText, null));

                        // Bảo vệ: nếu thiếu nhãn, tự bổ sung
                        string labelTrue = def.Options.Length >= 1 ? def.Options[0] : "True";
                        string labelFalse = def.Options.Length >= 2 ? def.Options[1] : "False";

                        items.Add(new KeyValuePair<string, bool?>(labelTrue, true));
                        items.Add(new KeyValuePair<string, bool?>(labelFalse, false));

                        var comboBool = new DataGridViewComboBoxColumn
                        {
                            HeaderText = def.Header,
                            Name = def.Header,
                            ValueType = allowNullForCombos ? typeof(bool?) : typeof(bool),
                            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                            FlatStyle = FlatStyle.Standard,
                            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                            DataSource = items,
                            DisplayMember = "Key",
                            ValueMember = "Value"
                        };

                        col = comboBool;
                    }
                    else
                    {
                        // Non-bool + Options: ComboBox đơn giản với chuỗi
                        var options = def.Options.ToList();
                        if (allowNullForCombos)
                            options.Insert(0, nullDisplayText);

                        var combo = new DataGridViewComboBoxColumn
                        {
                            HeaderText = def.Header,
                            Name = def.Header,
                            ValueType = def.DataType == typeof(string) ? typeof(string) : typeof(string),
                            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                            FlatStyle = FlatStyle.Standard,
                            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                            DataSource = options
                        };

                        col = combo;
                    }

                    // Đánh dấu Required
                    if (def.Required)
                    {
                        col.DefaultCellStyle.BackColor = Color.LightYellow;
                        col.HeaderCell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                    }

                    grid.Columns.Add(col);
                    continue;
                }

                // 2) Bool không có Options => CheckBox
                if (def.DataType == typeof(bool) || def.DataType == typeof(Boolean))
                {
                    col = new DataGridViewCheckBoxColumn
                    {
                        HeaderText = def.Header,
                        Name = def.Header,
                        ValueType = typeof(bool),
                        ThreeState = false,
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    };

                    if (def.Required)
                    {
                        col.DefaultCellStyle.BackColor = Color.LightYellow;
                        col.HeaderCell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                    }

                    grid.Columns.Add(col);
                    continue;
                }

                // 3) Các kiểu còn lại => TextBox
                var textCol = new DataGridViewTextBoxColumn
                {
                    HeaderText = def.Header,
                    Name = def.Header,
                    ValueType = def.DataType,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                };

                if (!string.IsNullOrEmpty(def.Format))
                    textCol.DefaultCellStyle.Format = def.Format;

                if (IsNumeric(def.DataType))
                    textCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                if (def.Required)
                {
                    textCol.DefaultCellStyle.BackColor = Color.LightYellow;
                    textCol.HeaderCell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                }

                grid.Columns.Add(textCol);
            }

            // Sự kiện xử lý lỗi & validate (gắn một lần)
            grid.DataError -= Grid_DataError;
            grid.DataError += Grid_DataError;

            grid.CellEndEdit -= Grid_CellEndEdit_ClearError;
            grid.CellEndEdit += Grid_CellEndEdit_ClearError;

            grid.CellValidating -= (s, e) => { }; // tránh trùng delegate ẩn danh
            grid.CellValidating += (s, e) => Grid_CellValidating(s, e, schema, allowNullForCombos, nullDisplayText);
        }

        // ------------ Helpers ------------

        private static bool IsNumeric(Type t) =>
            t == typeof(byte) || t == typeof(sbyte) ||
            t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) ||
            t == typeof(long) || t == typeof(ulong) ||
            t == typeof(float) || t == typeof(double) ||
            t == typeof(decimal);

        private static void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Không văng exception ra UI; để người dùng sửa
            e.ThrowException = false;
        }

        private static void Grid_CellEndEdit_ClearError(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count)
                grid.Rows[e.RowIndex].ErrorText = string.Empty;
        }

        private static void Grid_CellValidating(
            object sender,
            DataGridViewCellValidatingEventArgs e,
            ColumnDef[] schema,
            bool allowNullForCombos,
            string nullDisplayText)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var column = grid.Columns[e.ColumnIndex];
            var def = schema.FirstOrDefault(c => c.Header == column.HeaderText);
            if (def == null) return;

            // 1) Cột có Options (ComboBox)
            if (def.Options != null && def.Options.Length > 0)
            {
                // Lấy giá trị thực tế trong cell (Value), không dựa vào chuỗi hiển thị
                var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

                // Required: không cho để trống (null) hoặc chọn mục rỗng
                if (def.Required)
                {
                    var val = cell.Value;

                    // null khi allowNullForCombos hoặc chưa chọn
                    bool isNull =
                        val == null ||
                        (val is string s && (string.IsNullOrWhiteSpace(s) || s == nullDisplayText));

                    if (isNull)
                    {
                        grid.Rows[e.RowIndex].ErrorText = $"{def.Header} là bắt buộc.";
                        e.Cancel = true;
                        return;
                    }
                }

                // Không cần parse gì thêm cho ComboBox
                return;
            }

            // 2) Bool dạng CheckBox (không Options): luôn có true/false ⇒ bỏ qua Required
            if (def.DataType == typeof(bool) || def.DataType == typeof(Boolean))
                return;

            // 3) Các kiểu còn lại (TextBox)
            var input = e.FormattedValue?.ToString();

            if (def.Required && string.IsNullOrWhiteSpace(input))
            {
                grid.Rows[e.RowIndex].ErrorText = $"{def.Header} là bắt buộc.";
                e.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(input))
                return; // cho phép rỗng nếu không Required

            try
            {
                if (def.DataType == typeof(int))
                    _ = int.Parse(input, NumberStyles.Integer, CultureInfo.CurrentCulture);
                else if (def.DataType == typeof(long))
                    _ = long.Parse(input, NumberStyles.Integer, CultureInfo.CurrentCulture);
                else if (def.DataType == typeof(float))
                    _ = float.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture);
                else if (def.DataType == typeof(double))
                    _ = double.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture);
                else if (def.DataType == typeof(decimal))
                    _ = decimal.Parse(input, NumberStyles.Number, CultureInfo.CurrentCulture);
                // string, DateTime... có thể bổ sung thêm tuỳ nhu cầu
            }
            catch
            {
                grid.Rows[e.RowIndex].ErrorText = $"{def.Header} không hợp lệ (định dạng {def.DataType.Name}).";
                e.Cancel = true;
            }
        }
    }
}
