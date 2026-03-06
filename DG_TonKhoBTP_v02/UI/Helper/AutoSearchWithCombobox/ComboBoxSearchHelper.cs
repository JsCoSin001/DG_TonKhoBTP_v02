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
        private readonly Func<string, Task<List<T>>> _searchFunc;   // hàm truy vấn async
        private readonly Func<T, string> _displaySelector;          // field hiển thị trong list
        private readonly Action<T> _onItemSelected;                 // callback khi chọn item

        private System.Windows.Forms.Timer _debounceTimer;
        private CancellationTokenSource _cts;
        private bool _suppressEvents = false;                        // tránh vòng lặp sự kiện

        // ------------------------------------------------------------------ //
        //  Khởi tạo
        // ------------------------------------------------------------------ //
        /// <param name="comboBox">ComboBox cần gắn chức năng tìm kiếm</param>
        /// <param name="searchFunc">Hàm async nhận keyword → trả List<T></param>
        /// <param name="displaySelector">Chọn field hiển thị trong dropdown (vd: x => x.Ten)</param>
        /// <param name="onItemSelected">Callback khi người dùng chọn 1 item</param>
        /// <param name="debounceMs">Thời gian debounce (mặc định 200ms)</param>
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

            // Cấu hình ComboBox
            _comboBox.AutoCompleteMode = AutoCompleteMode.None;
            _comboBox.DropDownStyle = ComboBoxStyle.DropDown;

            // Debounce timer
            _debounceTimer = new System.Windows.Forms.Timer { Interval = debounceMs };
            _debounceTimer.Tick += OnDebounceElapsed;

            // Gắn sự kiện
            _comboBox.TextChanged += OnTextChanged;
            _comboBox.SelectedIndexChanged += OnSelectedIndexChanged;
            _comboBox.KeyDown += OnKeyDown;
        }

        // ------------------------------------------------------------------ //
        //  Xử lý sự kiện
        // ------------------------------------------------------------------ //
        private void OnTextChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            // Reset debounce mỗi lần gõ
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
            // Hủy tìm kiếm cũ nếu đang chạy
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var results = await _searchFunc(keyword);
                if (token.IsCancellationRequested) return;

                // Cập nhật UI trên UI thread
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

                            _comboBox.Text = currentText;
                            _comboBox.SelectionStart = currentText.Length;

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
            catch (OperationCanceledException) { /* bỏ qua */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboBoxSearchHelper] Lỗi tìm kiếm: {ex.Message}");
            }
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (_comboBox.SelectedItem is ComboBoxItem<T> selected)
            {
                _onItemSelected?.Invoke(selected.Value);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Nhấn Escape → đóng dropdown và xóa text
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
