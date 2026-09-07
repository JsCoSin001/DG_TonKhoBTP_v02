using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    /// <summary>
    /// Cột thời gian HH:mm cho DataGridView.
    /// Giá trị dữ liệu là TimeSpan?.
    /// Khi chỉnh sửa sử dụng DateTimePicker dạng up/down, không hiển thị ngày.
    /// </summary>
    public class DataGridViewTimeColumn : DataGridViewColumn
    {
        public DataGridViewTimeColumn()
            : base(new DataGridViewTimeCell())
        {
            SortMode = DataGridViewColumnSortMode.NotSortable;
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DefaultCellStyle.NullValue = string.Empty;
        }

        public override object Clone()
        {
            return base.Clone();
        }
    }

    public class DataGridViewTimeCell : DataGridViewTextBoxCell
    {
        public override Type EditType
        {
            get { return typeof(DataGridViewTimeEditingControl); }
        }

        public override Type ValueType
        {
            get { return typeof(TimeSpan); }
        }

        public override object DefaultNewRowValue
        {
            get { return null; }
        }

        public override void InitializeEditingControl(
            int rowIndex,
            object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(
                rowIndex,
                initialFormattedValue,
                dataGridViewCellStyle);

            DataGridViewTimeEditingControl control =
                DataGridView.EditingControl as DataGridViewTimeEditingControl;

            if (control != null)
            {
                control.SetNullableTime(Value);
            }
        }

        protected override object GetFormattedValue(
            object value,
            int rowIndex,
            ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter,
            TypeConverter formattedValueTypeConverter,
            DataGridViewDataErrorContexts context)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is TimeSpan)
            {
                TimeSpan time = (TimeSpan)value;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00}:{1:00}",
                    time.Hours,
                    time.Minutes);
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture);
            }

            TimeSpan parsedTime;

            if (TimeSpan.TryParse(
                value.ToString(),
                CultureInfo.InvariantCulture,
                out parsedTime))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00}:{1:00}",
                    parsedTime.Hours,
                    parsedTime.Minutes);
            }

            return string.Empty;
        }

        public override object ParseFormattedValue(
            object formattedValue,
            DataGridViewCellStyle cellStyle,
            TypeConverter formattedValueTypeConverter,
            TypeConverter valueTypeConverter)
        {
            if (formattedValue == null || formattedValue == DBNull.Value)
            {
                return null;
            }

            if (formattedValue is TimeSpan)
            {
                return (TimeSpan)formattedValue;
            }

            if (formattedValue is DateTime)
            {
                return ((DateTime)formattedValue).TimeOfDay;
            }

            string text = Convert.ToString(
                formattedValue,
                CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            TimeSpan result;

            if (TimeSpan.TryParseExact(
                text,
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                TimeSpanStyles.None,
                out result))
            {
                return result;
            }

            return base.ParseFormattedValue(
                formattedValue,
                cellStyle,
                formattedValueTypeConverter,
                valueTypeConverter);
        }
    }

    public class DataGridViewTimeEditingControl :
        DateTimePicker,
        IDataGridViewEditingControl
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int MK_LBUTTON = 0x0001;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam);

        private DataGridView dataGridView;
        private bool valueChanged;
        private int rowIndex;
        private bool isNull = true;
        private bool initializing;

        public DataGridViewTimeEditingControl()
        {
            Format = DateTimePickerFormat.Custom;
            CustomFormat = " ";
            ShowUpDown = true;
            Value = DateTime.Today;
        }

        public object EditingControlFormattedValue
        {
            get
            {
                return isNull
                    ? string.Empty
                    : Value.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
            set
            {
                SetNullableTime(value);
            }
        }

        public object GetEditingControlFormattedValue(
            DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        public void ApplyCellStyleToEditingControl(
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            CalendarForeColor = dataGridViewCellStyle.ForeColor;
            CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        public int EditingControlRowIndex
        {
            get { return rowIndex; }
            set { rowIndex = value; }
        }

        public bool EditingControlWantsInputKey(
            Keys key,
            bool dataGridViewWantsInputKey)
        {
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Delete:
                case Keys.Back:
                    return true;

                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        /// <summary>
        /// Được DataGridView gọi ngay khi editor bắt đầu hoạt động.
        /// Nếu cell đang rỗng thì tạo 00:00 ngay lập tức.
        /// Việc chọn riêng phần giờ được thực hiện bởi FocusHourPart().
        /// </summary>
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            if (isNull)
            {
                initializing = true;

                try
                {
                    isNull = false;
                    Value = DateTime.Today; // 00:00
                    UpdateCustomFormat();
                }
                finally
                {
                    initializing = false;
                }

                NotifyDataGridViewOfValueChange();
            }
        }

        /// <summary>
        /// Đưa keyboard focus vào DateTimePicker và bôi đen field giờ (HH).
        ///
        /// DateTimePicker (win32 SysDateTimePick32) không có API quản lý để
        /// chọn một field cụ thể, nên cách chuẩn để làm việc này là giả lập
        /// một cú click chuột vào đúng toạ độ field HH bằng PostMessage.
        ///
        /// QUAN TRỌNG: hàm này CHỈ được gọi từ một điểm an toàn — cụ thể là
        /// thông qua Control.BeginInvoke() từ sự kiện EditingControlShowing
        /// của DataGridView (xem UC_LoiDungMay_EditingControlShowing).
        /// BeginInvoke đưa lệnh gọi vào hàng đợi message và trì hoãn thực thi
        /// cho đến khi vòng lặp message hiện tại (xử lý click chuột gốc của
        /// DataGridView) đã hoàn tất. Nhờ vậy PostMessage bên dưới không còn
        /// bị lồng (reentrant) vào quá trình xử lý WM_LBUTTONDOWN/UP gốc của
        /// lưới nữa — đây chính là nguyên nhân gây treo (đơ) form ở bản trước,
        /// KHÔNG PHẢI do bản thân kỹ thuật PostMessage.
        ///
        /// TUYỆT ĐỐI không gọi hàm này trực tiếp trong CellMouseDown/CellMouseUp
        /// của DataGridView.
        /// </summary>
        public void FocusHourPart()
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (!Focused)
            {
                Focus();
            }

            // Đặt điểm click vào giữa field HH thay vì sát mép trái.
            // TextRenderer giúp vị trí ổn định hơn khi font thay đổi.
            int hourTextWidth = TextRenderer.MeasureText(
                "00",
                Font,
                Size.Empty,
                TextFormatFlags.NoPadding).Width;

            int x = 4 + (hourTextWidth / 2);
            int y = Math.Max(1, ClientSize.Height / 2);

            // Bảo đảm tọa độ luôn nằm trong client area.
            x = Math.Max(4, Math.Min(x, Math.Max(4, ClientSize.Width - 2)));

            IntPtr lParam = MakeLParam(x, y);

            PostMessage(
                Handle,
                WM_LBUTTONDOWN,
                new IntPtr(MK_LBUTTON),
                lParam);

            PostMessage(
                Handle,
                WM_LBUTTONUP,
                IntPtr.Zero,
                lParam);
        }

        private static IntPtr MakeLParam(int x, int y)
        {
            int value = (y << 16) | (x & 0xFFFF);
            return new IntPtr(value);
        }

        public bool RepositionEditingControlOnValueChange
        {
            get { return false; }
        }

        public DataGridView EditingControlDataGridView
        {
            get { return dataGridView; }
            set { dataGridView = value; }
        }

        public bool EditingControlValueChanged
        {
            get { return valueChanged; }
            set { valueChanged = value; }
        }

        public Cursor EditingPanelCursor
        {
            get { return base.Cursor; }
        }

        public void SetNullableTime(object value)
        {
            initializing = true;

            try
            {
                if (value == null ||
                    value == DBNull.Value ||
                    string.IsNullOrWhiteSpace(Convert.ToString(value)))
                {
                    isNull = true;
                    Value = DateTime.Today;
                }
                else if (value is TimeSpan)
                {
                    TimeSpan time = (TimeSpan)value;
                    isNull = false;
                    Value = DateTime.Today.Add(time);
                }
                else if (value is DateTime)
                {
                    isNull = false;
                    Value = DateTime.Today.Add(((DateTime)value).TimeOfDay);
                }
                else
                {
                    TimeSpan parsed;

                    if (TimeSpan.TryParse(
                        Convert.ToString(value, CultureInfo.InvariantCulture),
                        out parsed))
                    {
                        isNull = false;
                        Value = DateTime.Today.Add(parsed);
                    }
                    else
                    {
                        isNull = true;
                        Value = DateTime.Today;
                    }
                }

                UpdateCustomFormat();
            }
            finally
            {
                initializing = false;
                valueChanged = false;
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);

            if (initializing)
            {
                return;
            }

            isNull = false;
            UpdateCustomFormat();
            NotifyDataGridViewOfValueChange();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Delete / Backspace đưa cell thời gian về trạng thái trống.
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                isNull = true;
                UpdateCustomFormat();
                NotifyDataGridViewOfValueChange();

                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (isNull && IsTimeInputKey(e.KeyCode))
            {
                isNull = false;
                Value = DateTime.Today;
                UpdateCustomFormat();
                NotifyDataGridViewOfValueChange();
            }

            base.OnKeyDown(e);
        }

        private static bool IsTimeInputKey(Keys keyCode)
        {
            return (keyCode >= Keys.D0 && keyCode <= Keys.D9)
                || (keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9)
                || keyCode == Keys.Up
                || keyCode == Keys.Down
                || keyCode == Keys.Left
                || keyCode == Keys.Right;
        }

        private void UpdateCustomFormat()
        {
            CustomFormat = isNull ? " " : "HH:mm";
        }

        private void NotifyDataGridViewOfValueChange()
        {
            valueChanged = true;

            if (dataGridView != null)
            {
                dataGridView.NotifyCurrentCellDirty(true);
            }
        }
    }
}
