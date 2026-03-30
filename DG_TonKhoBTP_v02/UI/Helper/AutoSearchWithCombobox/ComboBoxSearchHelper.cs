using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    public class ComboBoxSearchHelper<T> : IDisposable
    {
        // ------------------------------------------------------------------ //
        //  Cấu hình
        // ------------------------------------------------------------------ //
        private readonly ComboBox _comboBox;
        private readonly Func<string, Task<List<T>>> _searchFunc;
        private readonly Func<T, string> _displaySelector;
        private readonly Action<T> _onItemSelected;

        private System.Windows.Forms.Timer _debounceTimer;
        private CancellationTokenSource _cts;
        private bool _suppressEvents = false;

        // Flag riêng: đánh dấu người dùng đang chủ động chọn item (click/Enter)
        private bool _isUserSelecting = false;

        // ------------------------------------------------------------------ //
        //  Khởi tạo
        // ------------------------------------------------------------------ //
        public ComboBoxSearchHelper(
            ComboBox comboBox,
            Func<string, Task<List<T>>> searchFunc,
            Func<T, string> displaySelector,
            Action<T> onItemSelected,
            int debounceMs = 200)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
            _searchFunc = searchFunc ?? throw new ArgumentNullException(nameof(searchFunc));
            _displaySelector = displaySelector ?? throw new ArgumentNullException(nameof(displaySelector));
            _onItemSelected = onItemSelected;

            _comboBox.AutoCompleteMode = AutoCompleteMode.None;
            _comboBox.DropDownStyle = ComboBoxStyle.DropDown;

            _debounceTimer = new System.Windows.Forms.Timer { Interval = debounceMs };
            _debounceTimer.Tick += OnDebounceElapsed;

            _comboBox.TextChanged += OnTextChanged;
            _comboBox.SelectedIndexChanged += OnSelectedIndexChanged;
            _comboBox.KeyDown += OnKeyDown;
            _comboBox.SelectionChangeCommitted += OnSelectionChangeCommitted;
        }

        // ------------------------------------------------------------------ //
        //  Xử lý sự kiện
        // ------------------------------------------------------------------ //
        private void OnTextChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void OnDebounceElapsed(object sender, EventArgs e)
        {
            _debounceTimer.Stop();

            string keyword = _comboBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(keyword)) return;

            await ExecuteSearchAsync(keyword);
        }

        private async Task ExecuteSearchAsync(string keyword)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var results = await _searchFunc(keyword);
                if (token.IsCancellationRequested) return;

                _comboBox.BeginInvoke((MethodInvoker)(() =>
                {
                    if (token.IsCancellationRequested) return;

                    _suppressEvents = true;
                    try
                    {
                        string currentText = _comboBox.Text;

                        _comboBox.Items.Clear();
                        if (results != null && results.Count > 0)
                        {
                            foreach (var item in results)
                                _comboBox.Items.Add(new ComboBoxItem<T>(item, _displaySelector(item)));

                            // Giữ nguyên text người dùng đã gõ, KHÔNG chọn item nào
                            _comboBox.SelectedIndex = -1;
                            _comboBox.Text = currentText;
                            _comboBox.SelectionStart = currentText.Length;
                            _comboBox.SelectionLength = 0;

                            if (!_comboBox.DroppedDown)
                                _comboBox.DroppedDown = true;
                        }
                        else
                        {
                            _comboBox.Text = currentText;
                        }
                    }
                    finally
                    {
                        _suppressEvents = false;
                    }
                }));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboBoxSearchHelper] Lỗi tìm kiếm: {ex.Message}");
            }
        }

        // SelectionChangeCommitted chỉ fire khi người dùng chủ động chọn item
        // (click vào item trong dropdown hoặc nhấn Enter) — không fire khi code set SelectedIndex
        private void OnSelectionChangeCommitted(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (_comboBox.SelectedItem is ComboBoxItem<T> selected)
            {
                _isUserSelecting = true;
                try
                {
                    _onItemSelected?.Invoke(selected.Value);
                }
                finally
                {
                    _isUserSelecting = false;
                }
            }
        }

        // SelectedIndexChanged vẫn giữ nhưng bỏ qua — mọi logic chọn item
        // đã chuyển sang OnSelectionChangeCommitted để tránh tự động chọn
        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            // Không làm gì — tránh tự động trigger khi DroppedDown mở
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _suppressEvents = true;
                _comboBox.DroppedDown = false;
                _comboBox.Text = "";
                _comboBox.Items.Clear();
                _suppressEvents = false;
            }
        }

        // ------------------------------------------------------------------ //
        //  Public Methods
        // ------------------------------------------------------------------ //
        public void Clear()
        {
            _suppressEvents = true;
            try
            {
                _debounceTimer.Stop();
                _comboBox.Items.Clear();
                _comboBox.Text = "";
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        // ------------------------------------------------------------------ //
        //  Dọn dẹp
        // ------------------------------------------------------------------ //
        public void Dispose()
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();

            _comboBox.TextChanged -= OnTextChanged;
            _comboBox.SelectedIndexChanged -= OnSelectedIndexChanged;
            _comboBox.SelectionChangeCommitted -= OnSelectionChangeCommitted;
            _comboBox.KeyDown -= OnKeyDown;
        }
    }

    // ------------------------------------------------------------------ //
    //  Wrapper để giữ object gốc trong Items của ComboBox
    // ------------------------------------------------------------------ //
    internal class ComboBoxItem<T>
    {
        public T Value { get; }
        private readonly string _display;

        public ComboBoxItem(T value, string display)
        {
            Value = value;
            _display = display;
        }

        public override string ToString() => _display;
    }
}