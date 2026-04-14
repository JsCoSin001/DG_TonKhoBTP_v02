using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    // ════════════════════════════════════════════════════════════════════════════
    //
    //  ComboBoxSearchHelper<T>
    //  ───────────────────────
    //  Bọc ComboBox để thực hiện tìm kiếm async an toàn, giải quyết toàn bộ
    //  các lỗi phổ biến trong WinForms:
    //
    //  ①  Debounce              — gõ liên tục chỉ gọi DB 1 lần sau khi dừng gõ
    //  ②  Race condition        — kết quả cũ không bao giờ đè kết quả mới
    //  ③  Bind guard            — _suppressEvents chặn event khi đang bind
    //  ④  Dispose safety        — kiểm tra IsDisposed/IsHandleCreated sau mọi await
    //  ⑤  Dropdown control      — chỉ mở khi có kết quả, tự đóng khi rỗng
    //  ⑥  Select lock           — sau khi chọn item không search lại
    //  ⑦  Event leak            — hook/unhook chính xác, không bao giờ hook 2 lần
    //  ⑧  Escape / FocusLost    — Esc xóa text, mất focus đóng dropdown sạch
    //  ⑨  DroppedDown re-open   — DroppedDown=true sau khi Items đã có (WinForms bug)
    //  ⑩  Duplicate keyword     — bỏ qua nếu keyword không đổi so với lần search trước
    //  ⑪  Thread-safe CTS swap  — Interlocked đảm bảo không race khi swap CTS
    //  ⑫  Invoke vs BeginInvoke — dùng đúng loại để tránh deadlock
    //  ⑬  ConfigureAwait        — tránh deadlock SynchronizationContext trong Task chain
    //
    // ════════════════════════════════════════════════════════════════════════════
    public sealed class ComboBoxSearchHelper_v1<T> : IDisposable
    {
        // ── Cấu hình công khai ───────────────────────────────────────────────

        /// <summary>ms chờ sau khi dừng gõ trước khi gọi DB. Mặc định 300.</summary>
        public int DebounceMs { get; set; } = 300;

        /// <summary>Số ký tự tối thiểu để kích hoạt search. Mặc định 1.</summary>
        public int MinLength { get; set; } = 1;

        /// <summary>Nhấn Escape sẽ xóa text. Mặc định true.</summary>
        public bool ClearOnEscape { get; set; } = true;

        // ── Dependency ───────────────────────────────────────────────────────
        private readonly ComboBox _cb;
        private readonly Func<string, Task<List<T>>> _searchFunc;
        private readonly Func<T, string> _displaySelector;
        private readonly Action<T> _onItemSelected;

        // ── Trạng thái nội bộ ───────────────────────────────────────────────
        private CancellationTokenSource _cts;            // ② ⑪
        private volatile bool _suppressEvents;           // ③  volatile để cross-thread safe
        private volatile bool _justSelected;             // ⑥
        private bool _disposed;                 // ④
        private string _lastSearchedKeyword;    // ⑩

        // ── Constructor ──────────────────────────────────────────────────────
        public ComboBoxSearchHelper_v1(
            ComboBox comboBox,
            Func<string, Task<List<T>>> searchFunc,
            Func<T, string> displaySelector,
            Action<T> onItemSelected)
        {
            _cb = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
            _searchFunc = searchFunc ?? throw new ArgumentNullException(nameof(searchFunc));
            _displaySelector = displaySelector ?? throw new ArgumentNullException(nameof(displaySelector));
            _onItemSelected = onItemSelected;

            // ⑦ Hook đúng 1 lần
            _cb.TextChanged += OnTextChanged;
            _cb.SelectedIndexChanged += OnSelectedIndexChanged;
            _cb.KeyDown += OnKeyDown;
            _cb.LostFocus += OnLostFocus;
            _cb.HandleDestroyed += OnHandleDestroyed;
        }

        // ════════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Xóa text và danh sách gợi ý.
        /// Gọi sau khi callback onItemSelected đã xử lý xong.
        /// </summary>
        public void Clear()
        {
            if (_disposed || !IsAlive()) return;
            SafeUi(() =>
            {
                WithSuppressed(() =>
                {
                    _cb.Items.Clear();
                    _cb.Text = string.Empty;
                    _cb.DroppedDown = false;
                    _lastSearchedKeyword = null;
                });
                _justSelected = false; // ⑥ reset sau khi Clear
            });
        }

        /// <summary>
        /// Gán text thủ công mà không kích hoạt search.
        /// Dùng khi cần điền sẵn giá trị lúc khởi tạo.
        /// </summary>
        public void SetTextSilent(string text)
        {
            if (_disposed || !IsAlive()) return;
            SafeUi(() => WithSuppressed(() => _cb.Text = text ?? string.Empty));
        }

        // ════════════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════════════════════════════

        // ① ② ③ ⑥ ⑩
        private void OnTextChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return; // ③ đang bind
            if (_justSelected) return; // ⑥ vừa chọn item

            string keyword = _cb.Text;

            // ⑩ Cùng keyword → không cần search lại
            if (keyword == _lastSearchedKeyword) return;

            // ⑪ Hủy CTS cũ thread-safe, lấy token mới
            CancelAndReplaceCts(out var token);

            if (keyword.Length < MinLength)
            {
                _lastSearchedKeyword = null;
                CloseDropdown();
                return;
            }

            // ① Bắt đầu debounce + search
            _ = RunSearchAfterDelayAsync(keyword, token);
        }

        // ① ② ④ ⑬
        private async Task RunSearchAfterDelayAsync(string keyword, CancellationToken token)
        {
            try
            {
                // ① Debounce: nếu user gõ tiếp trong khoảng này → bị cancel
                await Task.Delay(DebounceMs, token).ConfigureAwait(false); // ⑬

                if (token.IsCancellationRequested) return;

                // ② Gọi DB; WithCancellation cho phép hủy giữa chừng
                List<T> results = await _searchFunc(keyword)
                                        .WithCancellation(token)
                                        .ConfigureAwait(false); // ⑬

                // ④ Sau await: control có thể đã dispose
                if (token.IsCancellationRequested || !IsAlive()) return;

                // ⑫ Dùng BeginInvoke để không block thread pool nếu UI thread bận
                SafeUiAsync(() => BindResults(results, keyword));
            }
            catch (OperationCanceledException) { /* bình thường khi user gõ tiếp */ }
            catch (ObjectDisposedException) { /* control bị hủy trong khi await */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComboBoxSearch] Search error: {ex}");
            }
        }

        // ③ ⑤ ⑨
        private void BindResults(List<T> results, string originalKeyword)
        {
            if (_disposed || !IsAlive()) return; // ④

            // ② Keyword đã thay đổi trong lúc chờ → bỏ kết quả này
            if (_cb.Text != originalKeyword) return;

            // ③ Bind trong suppress để không kích hoạt TextChanged/SelectedIndexChanged
            WithSuppressed(() =>
            {
                _cb.Items.Clear();

                if (results == null || results.Count == 0)
                {
                    // ⑤ Không có dữ liệu → đóng dropdown
                    _cb.DroppedDown = false;
                    _lastSearchedKeyword = originalKeyword; // ⑩ ghi nhận đã search
                    return;
                }

                foreach (var item in results)
                    _cb.Items.Add(new ComboItem<T>(item, _displaySelector(item)));

                _lastSearchedKeyword = originalKeyword; // ⑩
            });

            // ⑨ WinForms bug: DroppedDown = true PHẢI đặt SAU WithSuppressed
            //    (suppressed = false) và SAU khi Items đã có đủ phần tử.
            //    Nếu đặt bên trong WithSuppressed sẽ không mở được dropdown
            //    vì WinForms kiểm tra suppressEvents nội bộ của nó.
            if (_cb.Items.Count > 0 && !_disposed && IsAlive())
            {
                _cb.DroppedDown = true;
                Cursor.Current = Cursors.Default; // fix: WinForms hay bị cursor loading
            }
        }

        // ③ ⑥ Chọn item từ dropdown
        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return; // ③
            if (_cb.SelectedIndex < 0) return;
            if (!(_cb.SelectedItem is ComboItem<T> selected)) return;

            // ⑥ Đánh dấu TRƯỚC khi gán Text vì gán Text sẽ bắn TextChanged ngay
            _justSelected = true;

            // ② Hủy search đang pending
            CancelAndReplaceCts(out _);

            WithSuppressed(() =>
            {
                _cb.Text = selected.Display;
                _cb.DroppedDown = false;
                _cb.Items.Clear();
            });

            // Callback ra ngoài (ví dụ: LoadChiTietDonTheoTenAsync)
            // Không bọc trong suppressed vì callback thường là async
            _onItemSelected?.Invoke(selected.Value);
            // _justSelected sẽ được reset bởi Clear() sau khi callback xử lý xong
        }

        // ⑧ Escape
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape) return;

            CancelAndReplaceCts(out _);
            WithSuppressed(() =>
            {
                _cb.DroppedDown = false;
                _cb.Items.Clear();
                if (ClearOnEscape)
                {
                    _cb.Text = string.Empty;
                    _lastSearchedKeyword = null;
                }
            });
            _justSelected = false;

            // Tránh Escape propagate lên đóng form/dialog cha
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        // ⑧ Mất focus → đóng dropdown (không xóa text)
        private void OnLostFocus(object sender, EventArgs e)
        {
            if (!IsAlive()) return;

            // ⑫ BeginInvoke: cho SelectedIndexChanged kịp bắn trước
            // (trường hợp user click thẳng vào item trong danh sách dropdown)
            _cb.BeginInvoke(new Action(() =>
            {
                if (_disposed || !IsAlive()) return;
                if (_cb.DroppedDown) return; // đã chọn item → bỏ qua

                WithSuppressed(() =>
                {
                    _cb.Items.Clear();
                    _cb.DroppedDown = false;
                });
            }));
        }

        // ════════════════════════════════════════════════════════════════════
        // INTERNAL HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>⑪ Thread-safe: hủy CTS cũ, tạo CTS mới, trả token mới.</summary>
        private void CancelAndReplaceCts(out CancellationToken newToken)
        {
            var newCts = new CancellationTokenSource();
            var oldCts = Interlocked.Exchange(ref _cts, newCts);
            oldCts?.Cancel();
            oldCts?.Dispose();
            newToken = newCts.Token;
        }

        /// <summary>③ Bọc action trong _suppressEvents = true/false an toàn.</summary>
        private void WithSuppressed(Action action)
        {
            _suppressEvents = true;
            try { action(); }
            finally { _suppressEvents = false; }
        }

        private void CloseDropdown()
        {
            if (!IsAlive()) return;
            SafeUi(() => WithSuppressed(() =>
            {
                _cb.DroppedDown = false;
                _cb.Items.Clear();
            }));
        }

        /// <summary>④ Control còn sống và có handle?</summary>
        private bool IsAlive()
            => _cb != null && !_cb.IsDisposed && _cb.IsHandleCreated;

        /// <summary>⑫ Invoke (blocking) — dùng khi cần chạy xong mới tiếp tục.</summary>
        private void SafeUi(Action action)
        {
            if (!IsAlive()) return;
            if (_cb.InvokeRequired) _cb.Invoke(action);
            else action();
        }

        /// <summary>⑫ BeginInvoke (non-blocking) — tránh deadlock khi gọi từ thread pool.</summary>
        private void SafeUiAsync(Action action)
        {
            if (!IsAlive()) return;
            if (_cb.InvokeRequired) _cb.BeginInvoke(action);
            else action();
        }

        // ④ Khi handle bị hủy (form đóng, UC unload) → tự dispose
        private void OnHandleDestroyed(object sender, EventArgs e) => Dispose();

        // ════════════════════════════════════════════════════════════════════
        // IDisposable
        // ════════════════════════════════════════════════════════════════════
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // ⑦ Unhook toàn bộ — không bao giờ leak
            _cb.TextChanged -= OnTextChanged;
            _cb.SelectedIndexChanged -= OnSelectedIndexChanged;
            _cb.KeyDown -= OnKeyDown;
            _cb.LostFocus -= OnLostFocus;
            _cb.HandleDestroyed -= OnHandleDestroyed;

            // ② Hủy task đang chạy
            var old = Interlocked.Exchange(ref _cts, null);
            old?.Cancel();
            old?.Dispose();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Wrapper lưu cả giá trị gốc lẫn text hiển thị trong Items
    // ════════════════════════════════════════════════════════════════════════
    internal sealed class ComboItem<T>
    {
        public T Value { get; }
        public string Display { get; }

        public ComboItem(T value, string display)
        {
            Value = value;
            Display = display ?? string.Empty;
        }

        // ComboBox gọi ToString() để render từng dòng trong dropdown
        public override string ToString() => Display;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Extension: gắn CancellationToken vào Task không hỗ trợ sẵn
    // ════════════════════════════════════════════════════════════════════════
    internal static class TaskExtensions
    {
        /// <summary>
        /// Nếu token bị cancel thì ném OperationCanceledException ngay lập tức,
        /// không chờ task gốc hoàn thành.
        /// Task gốc vẫn chạy ngầm đến khi tự kết thúc (safe vì không giữ reference UI).
        /// </summary>
        public static async Task<TResult> WithCancellation<TResult>(
            this Task<TResult> task,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // RunContinuationsAsynchronously: tránh deadlock khi continuation chạy inline
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (token.Register(
                state => ((TaskCompletionSource<bool>)state).TrySetResult(true),
                tcs,
                useSynchronizationContext: false)) // ⑬ không cần SyncCtx
            {
                var winner = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);

                if (winner != task)
                    throw new OperationCanceledException(token);
            }

            // Task DB thắng → await để propagate exception nếu DB lỗi
            return await task.ConfigureAwait(false);
        }
    }
}