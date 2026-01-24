using System;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    public partial class GetUserInputValue_Simple : Form
    {
        private bool _confirmed = false;
        private bool _touched = false;

        public decimal TongDongThuaValue => nbrTongDongThua.Value;

        public GetUserInputValue_Simple()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += GetUserInputValue_Simple_FormClosing;

        }

        private void GetUserInputValue_Simple_Load(object sender, EventArgs e)
        {
            // OK là nút mặc định khi bấm Enter
            this.AcceptButton = btnOk;

            // Không có nút X
            //this.ControlBox = false;

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (nbrTongDongThua.Value <= 0)
            {
                FrmWaiting.ShowGifAlert("Giá trị phải > 0");

                nbrTongDongThua.Focus();
                nbrTongDongThua.Select(0, nbrTongDongThua.Text.Length);
                return;
            }
            else
            {

                _confirmed = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

        }

        private void GetUserInputValue_Simple_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // ✅ Chặn user đóng bằng Alt+F4/taskbar close... nếu chưa bấm OK
            //if (!_confirmed && e.CloseReason == CloseReason.UserClosing)
            //{
            //    e.Cancel = true;
            //}
        }

    }
}
