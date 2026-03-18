using System.Drawing.Printing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    public class A4Config
    {
        public class PrintManager<T>
        {
            private readonly T _data;
            private readonly IPrintRenderer<T> _renderer;
            private readonly PrintDocument _printDocument;

            public PrintManager(T data, IPrintRenderer<T> renderer)
            {
                _data = data;
                _renderer = renderer;

                _printDocument = new PrintDocument();
                _printDocument.DefaultPageSettings.Landscape = false;
                _printDocument.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                _printDocument.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);

                _printDocument.PrintPage += (s, e) => _renderer.Render(e.Graphics, e.MarginBounds, e, _data);
                _printDocument.BeginPrint += (s, e) => _renderer.Reset();
            }

            public void ShowPreview(IWin32Window owner = null)
            {
                using (var frm = new PrintPreviewForm(_printDocument))
                {
                    if (owner == null) frm.ShowDialog();
                    else frm.ShowDialog(owner);
                }
            }

            public void Print() => _printDocument.Print();
        }
    }
}