using System;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    internal class PrintPreviewForm : Form
    {
        private readonly PrintPreviewControl _preview;
        private readonly Button _btnPrint;
        private readonly PrintDocument _printDocument;

        public PrintPreviewForm(PrintDocument printDocument)
        {
            _printDocument = printDocument;

            Text = "Xem trước khi in";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;

            // Toolbar panel
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = System.Drawing.SystemColors.Control,
                Padding = new Padding(6, 4, 6, 4)
            };

            _btnPrint = new Button
            {
                Text = "🖨  In",
                Width = 90,
                Height = 34,
                Location = new System.Drawing.Point(6, 5),
                FlatStyle = FlatStyle.System,
                Font = new System.Drawing.Font("Segoe UI", 10f)
            };
            _btnPrint.Click += BtnPrint_Click;

            toolbar.Controls.Add(_btnPrint);

            _preview = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                Document = _printDocument,
                Zoom = 1.0
            };

            Controls.Add(_preview);
            Controls.Add(toolbar);
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            using (var dlg = new PrintDialog())
            {
                dlg.Document = _printDocument;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Đăng ký EndPrint để đóng form ngay sau khi máy in nhận xong lệnh
                    _printDocument.EndPrint += OnEndPrint;
                    _printDocument.Print();
                }
            }
        }

        private void OnEndPrint(object sender, PrintEventArgs e)
        {
            _printDocument.EndPrint -= OnEndPrint;   // hủy đăng ký tránh gọi lại lần sau

            // EndPrint có thể fire từ thread khác → dùng Invoke cho an toàn
            if (InvokeRequired)
                Invoke(new Action(Close));
            else
                Close();
        }
    }
}