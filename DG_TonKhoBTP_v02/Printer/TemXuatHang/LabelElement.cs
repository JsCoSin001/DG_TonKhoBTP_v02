using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kiểu style chữ.
    /// </summary>
    public enum FontStyle
    {
        Normal = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3
    }

    /// <summary>
    /// Căn chỉnh nội dung theo chiều ngang.
    /// </summary>
    public enum TextAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// Loại phần tử trên tem.
    /// </summary>
    public enum ElementType
    {
        Text,       // văn bản thuần
        Image,      // ảnh (logo, badge)
        QrCode,     // mã QR được sinh động
        Line,       // đường kẻ ngang/dọc
        Rectangle   // hình chữ nhật (viền box mã SP, nền footer...)
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MODEL CHÍNH
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mô tả một phần tử đơn lẻ trên tem nhãn.
    /// Tọa độ X, Y tính từ góc trên-trái của tem (đơn vị: mm).
    /// </summary>
    public class LabelElement
    {
        // ── Định danh ─────────────────────────────────────────────────────────

        /// <summary>Tên gợi nhớ, dùng để debug/log. Ví dụ: "ProductCode", "QrCode".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Loại phần tử.</summary>
        public ElementType ElementType { get; set; } = ElementType.Text;

        // ── Nội dung ──────────────────────────────────────────────────────────

        /// <summary>
        /// Nội dung hiển thị.
        /// - Với Text/Rectangle: nội dung chữ (có thể rỗng với Rectangle).
        /// - Với Image: đường dẫn file ảnh.
        /// - Với QrCode: chuỗi cần encode thành QR.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        // ── Vị trí & kích thước (mm, tương đối với góc trái-trên của tem) ────

        /// <summary>Tọa độ X (mm) tính từ mép trái tem.</summary>
        public float X { get; set; }

        /// <summary>Tọa độ Y (mm) tính từ mép trên tem.</summary>
        public float Y { get; set; }

        /// <summary>Chiều rộng vùng vẽ (mm). 0 = tự động theo nội dung.</summary>
        public float Width { get; set; }

        /// <summary>Chiều cao vùng vẽ (mm). 0 = tự động theo font.</summary>
        public float Height { get; set; }

        // ── Typography ────────────────────────────────────────────────────────

        /// <summary>Tên font. Mặc định: Play.</summary>
        public string FontName { get; set; } = LabelConstants.FontDefault;

        /// <summary>Cỡ chữ (point).</summary>
        public float FontSize { get; set; } = 8f;

        /// <summary>Kiểu chữ: Normal / Bold / Italic / BoldItalic.</summary>
        public FontStyle Style { get; set; } = FontStyle.Normal;

        /// <summary>Căn chỉnh text trong vùng Width.</summary>
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;

        // ── Màu sắc ───────────────────────────────────────────────────────────

        /// <summary>Màu chữ / màu đường kẻ / màu viền. Mặc định: đen.</summary>
        public Color ForeColor { get; set; } = Color.Black;

        /// <summary>Màu nền (dùng cho Rectangle có fill, hoặc Text có highlight).</summary>
        public Color BackColor { get; set; } = Color.Transparent;

        /// <summary>Có tô màu nền không (Rectangle/Text).</summary>
        public bool HasBackground { get; set; } = false;

        // ── Viền (dùng cho Rectangle và Text box) ────────────────────────────

        /// <summary>Có vẽ viền không.</summary>
        public bool HasBorder { get; set; } = false;

        /// <summary>Độ dày viền (pixel). Mặc định: 1.</summary>
        public float BorderWidth { get; set; } = 1f;

        // ── Tuỳ chọn đặc biệt ────────────────────────────────────────────────

        /// <summary>
        /// Opacity khi vẽ ảnh (0.0 = trong suốt, 1.0 = đục hoàn toàn).
        /// Dùng cho watermark logo lớn giữa tem.
        /// </summary>
        public float Opacity { get; set; } = 1f;

        /// <summary>
        /// Padding bên trong text box (mm). Dùng khi HasBorder = true.
        /// Ví dụ: box mã sản phẩm cần padding trái/phải để chữ không sát viền.
        /// </summary>
        public float InnerPaddingMm { get; set; } = 1f;

        // ── Helper factory methods ────────────────────────────────────────────

        /// <summary>Tạo nhanh element kiểu Text thông thường.</summary>
        public static LabelElement CreateText(
            string name, string content,
            float x, float y, float width, float height,
            float fontSize, FontStyle style = FontStyle.Normal,
            TextAlignment alignment = TextAlignment.Left)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.Text,
                Content = content,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                Style = style,
                Alignment = alignment
            };
        }

        /// <summary>Tạo nhanh element kiểu Text có viền box (dùng cho mã SP).</summary>
        public static LabelElement CreateBoxedText(
            string name, string content,
            float x, float y, float width, float height,
            float fontSize, FontStyle style = FontStyle.Normal)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.Text,
                Content = content,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                Style = style,
                Alignment = TextAlignment.Center,
                HasBorder = true,
                BorderWidth = 1f,
                InnerPaddingMm = 1.5f
            };
        }

        /// <summary>Tạo nhanh element kiểu Image.</summary>
        public static LabelElement CreateImage(
            string name, string imagePath,
            float x, float y, float width, float height,
            float opacity = 1f)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.Image,
                Content = imagePath,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Opacity = opacity
            };
        }

        /// <summary>Tạo nhanh element QR Code.</summary>
        public static LabelElement CreateQrCode(
            string name, string qrContent,
            float x, float y, float size)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.QrCode,
                Content = qrContent,
                X = x,
                Y = y,
                Width = size,
                Height = size
            };
        }

        /// <summary>Tạo nhanh đường kẻ ngang.</summary>
        public static LabelElement CreateHLine(
            string name, float x, float y, float width,
            float borderWidth = 0.5f)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.Line,
                X = x,
                Y = y,
                Width = width,
                Height = 0,
                BorderWidth = borderWidth
            };
        }

        /// <summary>Tạo nhanh Rectangle có nền đặc (dùng cho footer).</summary>
        public static LabelElement CreateFilledRect(
            string name,
            float x, float y, float width, float height,
            Color backColor, string text = "",
            Color? foreColor = null,
            float fontSize = 9f, FontStyle style = FontStyle.Bold,
            TextAlignment alignment = TextAlignment.Center)
        {
            return new LabelElement
            {
                Name = name,
                ElementType = ElementType.Rectangle,
                Content = text,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                HasBackground = true,
                BackColor = backColor,
                ForeColor = foreColor ?? Color.White,
                FontSize = fontSize,
                Style = style,
                Alignment = alignment
            };
        }
    }
}
