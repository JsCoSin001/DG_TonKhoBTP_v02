// File: Core/ControlCleaner.cs
// Mục đích: Hàm dọn sạch input cho toàn form hoặc từng UC.
// [Luồng 6] Được gọi khi btnClear click.

using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Core
{
    public static class ControlCleaner
    {
        /// <summary>
        /// [Luồng 6.1] Dọn sạch tất cả control nhập liệu quen thuộc.
        /// </summary>
        public static void ClearAll(Control root)
        {
            foreach (Control c in root.Controls)
            {
                switch (c)
                {
                    case TextBox tb:
                        tb.Clear();
                        break;
                    case RichTextBox rtb:
                        rtb.Clear();
                        break;
                    case ComboBox cb:
                        cb.SelectedIndex = -1;
                        cb.Text = string.Empty;
                        break;
                    case NumericUpDown nud:
                        nud.Value = 0;
                        break;
                    case DateTimePicker dtp:
                        // Tuỳ nhu cầu: đặt về Today
                        dtp.Value = System.DateTime.Today;
                        break;
                    case DataGridView dgv:
                        // Xoá hết dòng nhập
                        DataGridViewUtils.ClearSmart(dgv);
                        break;
                }

                // Đệ quy
                if (c.HasChildren)
                    ClearAll(c);
            }
        }
    }
}
