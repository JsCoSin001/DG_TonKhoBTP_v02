using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox
{
    /// <summary>
    /// Helper tái sử dụng cho ComboBox tìm kiếm bất đồng bộ (async search-as-you-type).
    /// Xử lý: debounce, cancellation, DroppedDown fix (không dùng DataSource binding),
    /// điều hướng bằng phím ↓ / Enter.
    /// </summary>
    public sealed class ComboBoxSearchHelper : IDisposable
    {
        // ── Cấu hình ────────────────────────────────────────────────────────────
        /// <summary>Thời gian chờ sau khi user ngừng gõ trước khi gọi DB (ms).</summary>
        public int DebounceMs { get; set; } = 500;

        /// <summary>Tên cột dùng để hiển thị trong dropdown. Mặc định "ten".</summary>
        public string DisplayColumn { get; set; } = "ten";

        // ── Events ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired khi user chọn một item (click chuột hoặc Enter sau khi điều hướng bằng ↓).
        /// Trả về DataRowView của dòng được chọn.
        /// </summary>
        public event Action<DataRowView> ItemSelected;

        /// <summary>
        /// Fired khi ô tìm kiếm bị reset (text rỗng hoặc gọi Reset()).
        /// Dùng để xoá các field liên quan bên ngoài.
        /// </summary>
        public event Action Cleared;

        // ── Internals ───────────────────────────────────────────────────────────
        private readonly ComboBox _comboBox;

        /// <summary>
        /// Hàm truy vấn DB do caller cung cấp.
        /// Nhận keyword, trả về DataTable kết quả (hoặc null nếu không có).
        /// Chạy trên thread pool — KHÔNG được thao tác UI trong hàm này.
        /// </summary>
        private readonly Func<string, CancellationToken, Task<DataTable>> _queryFunc;

        private CancellationTokenSource _cts;
        private bool _suppressTextChange;
        private bool _userNavigatingSuggestions;

        // ── Constructor ─────────────────────────────────────────────────────────
        /// <param name="comboBox">ComboBox với DropDownStyle = DropDown.</param>
        /// <param name="queryFunc">
        ///     Async delegate thực hiện truy vấn DB.
        ///     Ví dụ: (keyword, ct) => Task.Run(() => DatabaseHelper.GetData(query, keyword, "ten"), ct)
        /// </param>
        public ComboBoxSearchHelper(
            ComboBox comboBox,
            Func<string, CancellationToken, Task<DataTable>> queryFunc)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
            _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));

            _comboBox.TextUpdate += OnTextUpdate;
            _comboBox.SelectionChangeCommitted += OnSelectionChangeCommitted;
            _comboBox.KeyDown += OnKeyDown;
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Xoá trắng ComboBox và huỷ mọi tìm kiếm đang chờ.
        /// Gọi khi muốn reset form từ bên ngoài.
        /// </summary>
        public void Reset()
        {
            _cts?.Cancel();

            _suppressTextChange = true;
            try
            {
                _comboBox.DroppedDown = false;
                _comboBox.DataSource = null;
                _comboBox.Items.Clear();
                _comboBox.Text = string.Empty;
                _userNavigatingSuggestions = false;
            }
            finally
            {
                _suppressTextChange = false;
            }

            Cleared?.Invoke();
        }

        // ── Event handlers ──────────────────────────────────────────────────────

        private async void OnTextUpdate(object sender, EventArgs e)
        {
            if (_suppressTextChange) return;

            string keyword = _comboBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                _userNavigatingSuggestions = false;
                _comboBox.DroppedDown = false;
                _comboBox.DataSource = null;
                _comboBox.Items.Clear();
                Cleared?.Invoke();
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(DebounceMs, token);
                await ShowSuggestions(keyword, token);
            }
            catch (OperationCanceledException) { }
            catch
            {
                _comboBox.DroppedDown = false;
            }
        }

        private void OnSelectionChangeCommitted(object sender, EventArgs e)
        {
            if (_comboBox.SelectedItem is DataRowViewWrapper wrapper)
                FireItemSelected(wrapper.Row);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (!_comboBox.DroppedDown && _comboBox.Items.Count > 0)
                    _comboBox.DroppedDown = true;

                if (_comboBox.Items.Count > 0)
                {
                    _userNavigatingSuggestions = true;
                    if (_comboBox.SelectedIndex < 0)
                        _comboBox.SelectedIndex = 0;
                }

                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                if (_userNavigatingSuggestions && _comboBox.SelectedItem is DataRowViewWrapper wrapper)
                {
                    FireItemSelected(wrapper.Row);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        // ── Core logic ──────────────────────────────────────────────────────────

        private async Task ShowSuggestions(string keyword, CancellationToken ct)
        {
            DataTable result = await _queryFunc(keyword, ct);

            ct.ThrowIfCancellationRequested();

            // Snapshot text ngay sau khi query xong để bắt kịp ký tự gõ thêm trong lúc chờ DB
            string currentText = _comboBox.Text;

            // Tháo event để thay đổi Items/DataSource không trigger lại tìm kiếm
            _comboBox.SelectionChangeCommitted -= OnSelectionChangeCommitted;
            _comboBox.TextUpdate -= OnTextUpdate;

            _suppressTextChange = true;
            try
            {
                _comboBox.DroppedDown = false;
                _comboBox.DisplayMember = string.Empty;
                _comboBox.ValueMember = string.Empty;
                _comboBox.DataSource = null;
                _comboBox.Items.Clear();

                if (result == null || result.Rows.Count == 0)
                {
                    _userNavigatingSuggestions = false;
                    _comboBox.Text = currentText;
                    _comboBox.SelectionStart = currentText.Length;
                    _comboBox.SelectionLength = 0;
                    return;
                }

                // Nạp Items trực tiếp (không qua DataSource) để WinForms không tự sync
                // Text theo DisplayMember khi gọi DroppedDown = true.
                foreach (DataRow row in result.Rows)
                {
                    var drv = result.DefaultView[result.Rows.IndexOf(row)];
                    _comboBox.Items.Add(new DataRowViewWrapper(drv, DisplayColumn));
                }

                _userNavigatingSuggestions = false;

                string textToRestore = currentText;
                _comboBox.BeginInvoke((MethodInvoker)(() =>
                {
                    if (ct.IsCancellationRequested) return;

                    _suppressTextChange = true;
                    try
                    {
                        _comboBox.SelectedIndex = -1;
                        _comboBox.DroppedDown = true;
                        _comboBox.Text = textToRestore;
                        _comboBox.SelectionStart = textToRestore.Length;
                        _comboBox.SelectionLength = 0;
                    }
                    finally
                    {
                        _suppressTextChange = false;
                    }

                    Cursor.Current = Cursors.Default;
                    Cursor.Show();
                }));
            }
            finally
            {
                _suppressTextChange = false;
                _comboBox.TextUpdate += OnTextUpdate;
                _comboBox.SelectionChangeCommitted += OnSelectionChangeCommitted;
            }
        }

        private void FireItemSelected(DataRowView row)
        {
            if (row == null) return;
            _userNavigatingSuggestions = false;
            _comboBox.DroppedDown = false;
            _comboBox.SelectedIndex = -1;
            _comboBox.Text = string.Empty;
            ItemSelected?.Invoke(row);
        }

        // ── IDisposable ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _comboBox.TextUpdate -= OnTextUpdate;
            _comboBox.SelectionChangeCommitted -= OnSelectionChangeCommitted;
            _comboBox.KeyDown -= OnKeyDown;
        }
    }

    // ── Wrapper giữ DataRowView — hiển thị cột chỉ định trong ComboBox ────────
    // Không dùng DataSource binding để tránh WinForms sync Text theo DisplayMember
    // khi DroppedDown = true với DropDownStyle.DropDown.
    internal sealed class DataRowViewWrapper
    {
        private readonly string _displayColumn;
        public DataRowView Row { get; }

        public DataRowViewWrapper(DataRowView row, string displayColumn = "ten")
        {
            Row = row;
            _displayColumn = displayColumn;
        }

        public override string ToString() => Row[_displayColumn]?.ToString() ?? string.Empty;
    }
}