using DG_TonKhoBTP_v02.Core;
using System;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.ThanhPhamCD
{
    public partial class Frm_PheLieu : Form
    {
        public PheLieuData PheLieu { get; private set; }

        // Constructor rỗng để WinForms Designer vẫn mở được form.
        public Frm_PheLieu() : this(null)
        {
        }

        public Frm_PheLieu(PheLieuData data)
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            SetupNumericSelectAll();

            // Luôn làm việc trên một bản clone. Nếu người dùng đóng bằng nút X,
            // dữ liệu draft ở màn hình chính không bị thay đổi.
            PheLieu = ClonePheLieu(data);
            LoadPheLieuToControls(PheLieu);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            FocusAndSelectAll(nbrDayPhe_NL);
        }

        /// <summary>
        /// Chỉ áp dụng chọn toàn bộ cho 6 NumericUpDown.
        /// Các ô ghi chú vẫn giữ hành vi click/focus bình thường.
        /// </summary>
        private void SetupNumericSelectAll()
        {
            NumericUpDown[] numericInputs =
            {
                nbrDayPhe_NL,
                nbrNhuaPhe_NL,
                nbrDongPhe_NL,
                rtbDayPhe_TP,
                rtbNhuaPhe_TP,
                rtbDongPhe_TP
            };

            foreach (NumericUpDown input in numericInputs)
            {
                input.Enter += NumericInput_SelectAll;
                input.Click += NumericInput_SelectAll;
            }
        }

        private void NumericInput_SelectAll(object sender, EventArgs e)
        {
            NumericUpDown numeric = sender as NumericUpDown;
            if (numeric == null) return;

            BeginInvoke(new Action(() =>
            {
                if (!numeric.IsDisposed && numeric.Enabled)
                    numeric.Select(0, numeric.Text.Length);
            }));
        }

        private void FocusAndSelectAll(NumericUpDown numeric)
        {
            if (numeric == null || numeric.IsDisposed || !numeric.Enabled)
                return;

            BeginInvoke(new Action(() =>
            {
                if (!numeric.IsDisposed && numeric.Enabled)
                {
                    numeric.Focus();
                    numeric.Select(0, numeric.Text.Length);
                }
            }));
        }

        private static PheLieuData ClonePheLieu(PheLieuData source)
        {
            if (source == null) return new PheLieuData();

            return new PheLieuData
            {
                DayPhe_NL = source.DayPhe_NL,
                NhuaPhe_NL = source.NhuaPhe_NL,
                DongPhe_NL = source.DongPhe_NL,
                GhiChuDayPhe_NL = source.GhiChuDayPhe_NL ?? string.Empty,
                GhiChuNhuaPhe_NL = source.GhiChuNhuaPhe_NL ?? string.Empty,
                GhiChuDongPhe_NL = source.GhiChuDongPhe_NL ?? string.Empty,
                DayPhe_TP = source.DayPhe_TP,
                NhuaPhe_TP = source.NhuaPhe_TP,
                DongPhe_TP = source.DongPhe_TP,
                GhiChuDayPhe_TP = source.GhiChuDayPhe_TP ?? string.Empty,
                GhiChuNhuaPhe_TP = source.GhiChuNhuaPhe_TP ?? string.Empty,
                GhiChuDongPhe_TP = source.GhiChuDongPhe_TP ?? string.Empty
            };
        }

        private static decimal ToNumericValue(double value, NumericUpDown control)
        {
            decimal converted;
            try
            {
                converted = Convert.ToDecimal(value);
            }
            catch
            {
                converted = 0m;
            }

            if (converted < control.Minimum) return control.Minimum;
            if (converted > control.Maximum) return control.Maximum;
            return converted;
        }

        private void LoadPheLieuToControls(PheLieuData data)
        {
            data = data ?? new PheLieuData();

            // Phế nguyên liệu
            nbrDayPhe_NL.Value = ToNumericValue(data.DayPhe_NL, nbrDayPhe_NL);
            nbrNhuaPhe_NL.Value = ToNumericValue(data.NhuaPhe_NL, nbrNhuaPhe_NL);
            nbrDongPhe_NL.Value = ToNumericValue(data.DongPhe_NL, nbrDongPhe_NL);
            rtbGhiChuDayPhe_NL.Text = data.GhiChuDayPhe_NL ?? string.Empty;
            rtbGhiChuNhuaPhe_NL.Text = data.GhiChuNhuaPhe_NL ?? string.Empty;
            rtbGhiChuDongPhe_NL.Text = data.GhiChuDongPhe_NL ?? string.Empty;

            // Ba control tên rtb... bên dưới thực tế là NumericUpDown.
            rtbDayPhe_TP.Value = ToNumericValue(data.DayPhe_TP, rtbDayPhe_TP);
            rtbNhuaPhe_TP.Value = ToNumericValue(data.NhuaPhe_TP, rtbNhuaPhe_TP);
            rtbDongPhe_TP.Value = ToNumericValue(data.DongPhe_TP, rtbDongPhe_TP);
            rtbGhiChuDayPhe_TP.Text = data.GhiChuDayPhe_TP ?? string.Empty;
            rtbGhiChuNhuaPhe_TP.Text = data.GhiChuNhuaPhe_TP ?? string.Empty;
            rtbGhiChuDongPhe_TP.Text = data.GhiChuDongPhe_TP ?? string.Empty;
        }

        private PheLieuData ReadPheLieuFromControls()
        {
            return new PheLieuData
            {
                DayPhe_NL = (double)nbrDayPhe_NL.Value,
                NhuaPhe_NL = (double)nbrNhuaPhe_NL.Value,
                DongPhe_NL = (double)nbrDongPhe_NL.Value,
                GhiChuDayPhe_NL = rtbGhiChuDayPhe_NL.Text ?? string.Empty,
                GhiChuNhuaPhe_NL = rtbGhiChuNhuaPhe_NL.Text ?? string.Empty,
                GhiChuDongPhe_NL = rtbGhiChuDongPhe_NL.Text ?? string.Empty,
                DayPhe_TP = (double)rtbDayPhe_TP.Value,
                NhuaPhe_TP = (double)rtbNhuaPhe_TP.Value,
                DongPhe_TP = (double)rtbDongPhe_TP.Value,
                GhiChuDayPhe_TP = rtbGhiChuDayPhe_TP.Text ?? string.Empty,
                GhiChuNhuaPhe_TP = rtbGhiChuNhuaPhe_TP.Text ?? string.Empty,
                GhiChuDongPhe_TP = rtbGhiChuDongPhe_TP.Text ?? string.Empty
            };
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            PheLieu = ReadPheLieuFromControls();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            // Chỉ xóa dữ liệu đang nhập trên form; không đóng form.
            LoadPheLieuToControls(new PheLieuData());
            FocusAndSelectAll(nbrDayPhe_NL);
        }
    }
}
