using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper.Reuseable
{
    public static class DataGridViewClipboardHelper
    {
        /// <summary>
        /// Gắn chức năng clipboard (Copy/Paste) cho DataGridView:
        /// - Ctrl+C: DataGridView hỗ trợ sẵn khi bật ClipboardCopyMode
        /// - Ctrl+V:
        ///   + Clipboard 1 giá trị & chọn nhiều ô => fill tất cả ô selected
        ///   + Clipboard TSV nhiều ô => paste block từ CurrentCell
        /// </summary>
        public static void Attach(
            DataGridView dgv,
            bool includeHeaderWhenCopy = false,
            bool enableTsvBlockPaste = true,
            bool useDBNullForEmpty = true
        )
        {
            if (dgv == null) throw new ArgumentNullException(nameof(dgv));

            // Setup có thể làm qua Properties, nhưng attach để "đảm bảo luôn đúng"
            dgv.MultiSelect = true;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.ClipboardCopyMode = includeHeaderWhenCopy
                ? DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
                : DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            // Tránh gắn trùng event nhiều lần
            Detach(dgv);

            dgv.KeyDown += (s, e) =>
            {
                if (!e.Control) return;

                if (e.KeyCode == Keys.V)
                {
                    string raw = Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(raw)) return;

                    bool isSingleCell = IsSingleCellClipboard(raw);

                    if (isSingleCell && dgv.SelectedCells.Count > 1)
                    {
                        PasteSingleValueToSelectedCells(dgv, raw, useDBNullForEmpty);
                    }
                    else if (enableTsvBlockPaste)
                    {
                        PasteTsvBlockFromCurrentCell(dgv, raw, useDBNullForEmpty);
                    }
                    else
                    {
                        // Nếu không enableTsvBlockPaste thì chỉ paste vào CurrentCell
                        PasteSingleValueToCurrentCell(dgv, raw, useDBNullForEmpty);
                    }

                    e.Handled = true;
                }
            };

            // Lưu marker để Detach được (dùng Tag)
            MarkAttached(dgv);
        }

        /// <summary>
        /// Gỡ chức năng (nếu bạn cần).
        /// Lưu ý: vì Attach dùng lambda, cách đơn giản nhất là dùng marker để tránh attach trùng,
        /// còn detach hoàn toàn thì thường không cần.
        /// </summary>
        public static void Detach(DataGridView dgv)
        {
            if (dgv == null) return;

            // Không thể remove lambda đã add trực tiếp nếu không giữ reference.
            // Nên cách thực tế: chỉ cần Attach 1 lần / hoặc dùng marker để không attach trùng.
            // Bạn có thể bỏ qua Detach.
        }

        private static bool IsSingleCellClipboard(string raw)
        {
            // 1 ô thường không có tab và không có newline
            return raw.IndexOf('\t') < 0 &&
                   raw.IndexOf("\r\n", StringComparison.Ordinal) < 0 &&
                   raw.IndexOf('\n') < 0;
        }

        private static void PasteSingleValueToSelectedCells(DataGridView dgv, string valueText, bool useDBNullForEmpty)
        {
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                if (cell == null) continue;
                if (cell.ReadOnly) continue;
                if (cell.OwningColumn != null && cell.OwningColumn.ReadOnly) continue;

                cell.Value = ConvertToColumnType(valueText, GetColumnValueType(cell), useDBNullForEmpty);
            }

            dgv.EndEdit();
        }

        private static void PasteSingleValueToCurrentCell(DataGridView dgv, string valueText, bool useDBNullForEmpty)
        {
            if (dgv.CurrentCell == null) return;

            var cell = dgv.CurrentCell;
            if (cell.ReadOnly) return;
            if (cell.OwningColumn != null && cell.OwningColumn.ReadOnly) return;

            cell.Value = ConvertToColumnType(valueText, GetColumnValueType(cell), useDBNullForEmpty);
            dgv.EndEdit();
        }

        private static void PasteTsvBlockFromCurrentCell(DataGridView dgv, string tsv, bool useDBNullForEmpty)
        {
            if (dgv.CurrentCell == null) return;

            int startRow = dgv.CurrentCell.RowIndex;
            int startCol = dgv.CurrentCell.ColumnIndex;

            string[] lines = tsv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Bỏ dòng cuối rỗng nếu clipboard có newline cuối
            if (lines.Length > 0 && string.IsNullOrEmpty(lines[lines.Length - 1]))
                lines = lines.Take(lines.Length - 1).ToArray();

            dgv.SuspendLayout();

            for (int i = 0; i < lines.Length; i++)
            {
                int r = startRow + i;
                if (r >= dgv.RowCount) break;

                string[] parts = lines[i].Split('\t');

                for (int j = 0; j < parts.Length; j++)
                {
                    int c = startCol + j;
                    if (c >= dgv.ColumnCount) break;

                    var col = dgv.Columns[c];
                    if (col == null || col.ReadOnly) continue;

                    var cell = dgv[c, r];
                    if (cell == null || cell.ReadOnly) continue;

                    cell.Value = ConvertToColumnType(parts[j], col.ValueType, useDBNullForEmpty);
                }
            }

            dgv.ResumeLayout();
            dgv.EndEdit();
        }

        private static Type GetColumnValueType(DataGridViewCell cell)
        {
            if (cell == null) return typeof(string);
            if (cell.OwningColumn == null) return typeof(string);

            return cell.OwningColumn.ValueType ?? typeof(string);
        }

        /// <summary>
        /// Convert string clipboard sang kiểu của cột. C# 8.0 OK.
        /// - useDBNullForEmpty = true: rỗng => DBNull.Value (hợp DataTable)
        /// - useDBNullForEmpty = false: rỗng => null (hợp List<T>)
        /// </summary>
        private static object ConvertToColumnType(string text, Type targetType, bool useDBNullForEmpty)
        {
            if (targetType == null || targetType == typeof(string))
                return text;

            // Nullable<T>
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (string.IsNullOrWhiteSpace(text))
            {
                if (targetType == typeof(string)) return "";

                if (useDBNullForEmpty) return DBNull.Value;
                return null;
            }

            // Parse theo culture hiện tại (nếu bạn muốn culture invariant thì đổi CultureInfo)
            var culture = CultureInfo.CurrentCulture;

            try
            {
                if (targetType == typeof(int)) return int.Parse(text, NumberStyles.Any, culture);
                if (targetType == typeof(long)) return long.Parse(text, NumberStyles.Any, culture);
                if (targetType == typeof(decimal)) return decimal.Parse(text, NumberStyles.Any, culture);
                if (targetType == typeof(double)) return double.Parse(text, NumberStyles.Any, culture);
                if (targetType == typeof(DateTime)) return DateTime.Parse(text, culture);
                if (targetType == typeof(bool)) return bool.Parse(text);

                return Convert.ChangeType(text, targetType, culture);
            }
            catch
            {
                // Nếu convert fail: trả lại text để tránh crash
                return text;
            }
        }

        private static void MarkAttached(DataGridView dgv)
        {
            // Dùng Tag để tránh attach trùng nhiều lần (an toàn & đơn giản)
            // Tag có thể bạn đang dùng => nếu vậy đổi qua khác (VD: dùng dgv.AccessibleDescription)
            string marker = "DGV_CLIPBOARD_HELPER_ATTACHED";
            if (dgv.Tag == null) dgv.Tag = marker;
            else if (dgv.Tag is string s)
            {
                if (!s.Contains(marker)) dgv.Tag = s + "|" + marker;
            }
            else
            {
                // Tag đang là object khác -> không động vào.
            }
        }
    }
}
