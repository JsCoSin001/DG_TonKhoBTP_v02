using System.Drawing;
using System.Drawing.Printing;

namespace DG_TonKhoBTP_v02.Printer.A4
{
    public interface IPrintRenderer<T>
    {
        void Reset();
        void Render(Graphics g, Rectangle marginBounds, PrintPageEventArgs e, T data);
    }
}