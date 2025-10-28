using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class FrmWaiting : Form
    {
        public FrmWaiting() : this("Đang xử lý, vui lòng đợi...")
        {
        }
        public FrmWaiting(string message)
        {
            InitializeComponent();
            lblMessage.Text = message;
        }

        // Cho phép đổi nội dung khi đang chạy (tuỳ chọn)
        public string MessageText
        {
            get => lblMessage.Text;
            set => lblMessage.Text = value;
        }

        // Đóng form an toàn từ luồng nền
        public void SafeClose()
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(new Action(Close));
            else Close();
        }
    }
}
