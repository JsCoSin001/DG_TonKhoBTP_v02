using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    // ═══════════════════════════════════════════════════════════════════════════
    // PrintPadding — padding nội dung bên trong vùng margin của trang in
    // Đơn vị: 1/100 inch  (ví dụ: 50 ≈ 0.5 inch ≈ 13 mm)
    // ═══════════════════════════════════════════════════════════════════════════
    public struct PrintPadding
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        /// <summary>Tất cả 4 cạnh bằng nhau.</summary>
        public PrintPadding(int all) : this(all, all, all, all) { }

        public PrintPadding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Không có padding thêm (mặc định).</summary>
        public static PrintPadding None => new PrintPadding(0);
    }

    public class A4Config
    {
        public class PrintManager<T>
        {
            private readonly T _data;
            private readonly IPrintRenderer<T> _renderer;
            private readonly PrintDocument _printDocument;

            // ── Padding nội dung (bên trong margin) ─────────────────────────
            // Thay đổi giá trị này để điều chỉnh khoảng cách nội dung với mép giấy.
            // Đơn vị: 1/100 inch.  50 ≈ 13 mm,  20 ≈ 5 mm,  0 = không thêm.
            //
            //   new PrintPadding(left, top, right, bottom)
            //   new PrintPadding(30)          ← 4 cạnh bằng nhau = ~8 mm
            public static PrintPadding ContentPadding = new PrintPadding(
                left: 30,   // ~8 mm
                top: 20,   // ~5 mm
                right: 30,   // ~8 mm
                bottom: 20    // ~5 mm
            );

            public PrintManager(T data, IPrintRenderer<T> renderer)
            {
                _data = data;
                _renderer = renderer;

                _printDocument = new PrintDocument();
                _printDocument.DefaultPageSettings.Landscape = false;
                _printDocument.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                _printDocument.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);

                _printDocument.PrintPage += (s, e) =>
                {
                    // Thu nhỏ marginBounds theo ContentPadding trước khi truyền vào Renderer
                    var pad = ContentPadding;
                    var original = e.MarginBounds;
                    var paddedBounds = new Rectangle(
                        original.Left + pad.Left,
                        original.Top + pad.Top,
                        original.Width - pad.Left - pad.Right,
                        original.Height - pad.Top - pad.Bottom
                    );
                    _renderer.Render(e.Graphics, paddedBounds, e, _data);
                };

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