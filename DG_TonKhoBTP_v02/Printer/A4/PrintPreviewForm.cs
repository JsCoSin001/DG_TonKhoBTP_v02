using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    internal class PrintPreviewForm : Form
    {
        private readonly PrintPreviewControl _preview;
        private readonly Button _btnPrint;
        private readonly Button _btnPrev;
        private readonly Button _btnNext;
        private readonly Label _lblPage;
        private readonly PrintDocument _printDocument;

        private int _currentPageIndex = 0;
        private int _pageCount = 1;

        public PrintPreviewForm(PrintDocument printDocument)
        {
            _printDocument = printDocument;

            Text = "Xem trước khi in";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;

            _pageCount = GetPreviewPageCount(_printDocument);
            if (_pageCount <= 0)
                _pageCount = 1;

            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = SystemColors.Control,
                Padding = new Padding(6, 4, 6, 4)
            };

            _btnPrint = new Button
            {
                Text = "In",
                Width = 90,
                Height = 34,
                Location = new Point(6, 5),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 10f)
            };
            _btnPrint.Click += BtnPrint_Click;

            _btnPrev = new Button
            {
                Text = "< Trước",
                Width = 90,
                Height = 34,
                Location = new Point(106, 5),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 10f)
            };
            _btnPrev.Click += BtnPrev_Click;

            _btnNext = new Button
            {
                Text = "Sau >",
                Width = 90,
                Height = 34,
                Location = new Point(202, 5),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 10f)
            };
            _btnNext.Click += BtnNext_Click;

            _lblPage = new Label
            {
                AutoSize = false,
                Width = 130,
                Height = 34,
                Location = new Point(302, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f)
            };

            toolbar.Controls.Add(_btnPrint);
            toolbar.Controls.Add(_btnPrev);
            toolbar.Controls.Add(_btnNext);
            toolbar.Controls.Add(_lblPage);

            _preview = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                Document = _printDocument,
                AutoZoom = true,
                Rows = 1,
                Columns = 1,
                StartPage = 0,
                UseAntiAlias = true
            };

            Controls.Add(_preview);
            Controls.Add(toolbar);

            UpdatePageButtons();
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (_currentPageIndex <= 0)
                return;

            _currentPageIndex--;
            _preview.StartPage = _currentPageIndex;
            _preview.Invalidate();
            UpdatePageButtons();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_currentPageIndex >= _pageCount - 1)
                return;

            _currentPageIndex++;
            _preview.StartPage = _currentPageIndex;
            _preview.Invalidate();
            UpdatePageButtons();
        }

        private void UpdatePageButtons()
        {
            _lblPage.Text = $"Trang {_currentPageIndex + 1} / {_pageCount}";
            _btnPrev.Enabled = _currentPageIndex > 0;
            _btnNext.Enabled = _currentPageIndex < _pageCount - 1;
        }

        private int GetPreviewPageCount(PrintDocument document)
        {
            PrintController oldController = document.PrintController;

            try
            {
                var previewController = new PreviewPrintController();
                document.PrintController = previewController;
                document.Print();

                PreviewPageInfo[] pages = previewController.GetPreviewPageInfo();
                return pages == null || pages.Length == 0 ? 1 : pages.Length;
            }
            catch
            {
                return 1;
            }
            finally
            {
                document.PrintController = oldController;
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            using (var dlg = new PrintDialog())
            {
                dlg.Document = _printDocument;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _printDocument.EndPrint += OnEndPrint;
                    _printDocument.Print();
                }
            }
        }

        private void OnEndPrint(object sender, PrintEventArgs e)
        {
            _printDocument.EndPrint -= OnEndPrint;

            if (InvokeRequired)
                Invoke(new Action(Close));
            else
                Close();
        }
    }
}
