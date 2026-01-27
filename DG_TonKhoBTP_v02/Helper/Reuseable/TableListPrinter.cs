using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

public class DSPhatHanhKHSX
{
    public string Ten { get; set; }
    public string Lot { get; set; }
    public string GhiChuKH { get; set; } = "";
    public string GhiChuSX { get; set; } = "";
    public int TrangThai { get;set; } = 0;
    public bool Rut { get; set; } = false;
    public bool Ben { get; set; } = false;
    public bool QB { get; set; } = false;
    public bool BocLot { get; set; } = false;
    public bool BocMach { get; set; } = false;
    public bool BocVo { get; set; } = false;
}

/// <summary>
/// In danh sách dạng bảng, tự xuống dòng trong ô, tự sang trang, tuỳ chọn khổ giấy + hướng in,
/// nhận độ rộng cột theo mm. Có TITLE (tiêu đề) và footer 2 dòng (canh trái) ở cuối.
/// </summary>
public sealed class TableListPrinter<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly string[] _headers;
    private readonly Func<T, string>[] _selectors;
    private readonly float[] _colWidthsMm;

    private int _index;
    private string[] _footerLines;
    private string _title;

    public TableListPrinter(
        IReadOnlyList<T> items,
        string[] headers,
        Func<T, string>[] selectors,
        float[] colWidthsMm)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        _selectors = selectors ?? throw new ArgumentNullException(nameof(selectors));
        _colWidthsMm = colWidthsMm ?? throw new ArgumentNullException(nameof(colWidthsMm));

        if (_headers.Length == 0) throw new ArgumentException("headers rỗng.");
        if (_headers.Length != _selectors.Length || _headers.Length != _colWidthsMm.Length)
            throw new ArgumentException("headers / selectors / colWidthsMm phải cùng số lượng phần tử.");
    }

    /// <summary>Set title (tiêu đề) in phía trên bảng</summary>
    public TableListPrinter<T> SetTitle(string title)
    {
        _title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        return this;
    }

    /// <summary>Set footer lines (ví dụ: "Người lập", "Nguyễn Văn A")</summary>
    public TableListPrinter<T> SetFooterLines(params string[] lines)
    {
        _footerLines = lines?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return this;
    }

    /// <summary>
    /// In bảng với tuỳ chọn khổ giấy + hướng in.
    /// paperName: ví dụ "A5", "A4", hoặc đúng tên PaperSize trên driver.
    /// landscape: true = ngang, false = dọc
    /// </summary>
    public bool Print(
    string printerName = null,
    string paperName = "A5",
    bool landscape = true,
    Margins margins = null,
    Font titleFont = null,
    Font headerFont = null,
    Font cellFont = null)
    {
        if (_items.Count == 0) return false;

        using var doc = new PrintDocument();

        if (!string.IsNullOrWhiteSpace(printerName))
            doc.PrinterSettings.PrinterName = printerName;

        var ps = FindPaperSize(doc.PrinterSettings, paperName);
        if (ps == null)
            throw new Exception($"Không tìm thấy khổ giấy '{paperName}' trong driver máy in.");

        doc.DefaultPageSettings.PaperSize = ps;
        doc.DefaultPageSettings.Landscape = landscape;
        doc.DefaultPageSettings.Margins = margins ?? MmMargins(8, 8, 8, 8);

        _index = 0;

        titleFont ??= new Font("Arial", 12f, FontStyle.Bold);
        headerFont ??= new Font("Arial", 10f, FontStyle.Bold);
        cellFont ??= new Font("Arial", 9.5f, FontStyle.Regular);

        bool canceled = false;
        doc.EndPrint += (s, e) => { if (e.Cancel) canceled = true; };

        doc.PrintPage += (s, e) =>
        {
            e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var area = e.MarginBounds;

            using var pen = new Pen(Color.Black, 1);
            Brush brush = Brushes.Black; // KHÔNG dispose Brushes.Black

            var sf = new StringFormat(StringFormatFlags.LineLimit) { Trimming = StringTrimming.Word };
            int paddingPx = 4;

            float[] colWpx = BuildColumnWidthsPx(e.Graphics, area.Width, _colWidthsMm);

            float x = area.Left;
            float y = area.Top;

            // TITLE
            if (!string.IsNullOrWhiteSpace(_title))
            {
                var titleSf = new StringFormat(StringFormatFlags.NoClip)
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisWord
                };

                float titleGap = 6f;
                float titleH = MeasureTextBlockHeight(e.Graphics, _title, titleFont, area.Width);

                if (y + titleH + titleGap > area.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                var titleRect = new RectangleF(area.Left, y, area.Width, titleH);
                e.Graphics.DrawString(_title, titleFont, brush, titleRect, titleSf);
                y += titleH + titleGap;
            }

            // Header
            float headerH = MeasureRowHeight(e.Graphics, headerFont, sf, _headers, colWpx, paddingPx);
            if (y + headerH > area.Bottom)
            {
                e.HasMorePages = true;
                return;
            }
            DrawRow(e.Graphics, pen, headerFont, brush, sf, paddingPx, x, y, headerH, colWpx, _headers);
            y += headerH;

            // Rows
            while (_index < _items.Count)
            {
                var rowTexts = new string[_selectors.Length];
                for (int i = 0; i < _selectors.Length; i++)
                    rowTexts[i] = _selectors[i](_items[_index]) ?? "";

                float rowH = MeasureRowHeight(e.Graphics, cellFont, sf, rowTexts, colWpx, paddingPx);

                if (y + rowH > area.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                DrawRow(e.Graphics, pen, cellFont, brush, sf, paddingPx, x, y, rowH, colWpx, rowTexts);

                y += rowH;
                _index++;
            }

            // Footer (chỉ trang cuối)
            if (_index >= _items.Count && _footerLines != null && _footerLines.Length > 0)
            {
                using var footerFont = new Font("Arial", 10f, FontStyle.Regular);

                float lineH = footerFont.GetHeight(e.Graphics);
                float spacing = 2f;
                float footerH = _footerLines.Length * lineH + (_footerLines.Length - 1) * spacing;

                float gap = 8f;

                if (y + gap + footerH > area.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                y += gap;

                float fx = area.Left;
                foreach (var line in _footerLines)
                {
                    e.Graphics.DrawString(line, footerFont, Brushes.Black, fx, y);
                    y += lineH + spacing;
                }
            }

            e.HasMorePages = false;
        };

        try
        {
            doc.Print();
            return !canceled; // không cancel => success
        }
        catch
        {
            return false;     // throw => fail
        }
    }


    private static float MeasureTextBlockHeight(Graphics g, string text, Font font, float maxWidthPx)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var sf = new StringFormat(StringFormatFlags.LineLimit) { Trimming = StringTrimming.Word };
        var size = g.MeasureString(text, font, new SizeF(Math.Max(1, maxWidthPx), 10000f), sf);
        return size.Height;
    }

    private static PaperSize FindPaperSize(PrinterSettings settings, string paperName)
    {
        if (settings == null) return null;
        paperName = (paperName ?? "").Trim();
        if (paperName.Length == 0) return null;

        // 1) match theo tên
        var byName = settings.PaperSizes.Cast<PaperSize>()
            .FirstOrDefault(p => string.Equals(p.PaperName, paperName, StringComparison.OrdinalIgnoreCase)
                              || p.PaperName.IndexOf(paperName, StringComparison.OrdinalIgnoreCase) >= 0);
        if (byName != null) return byName;

        // 2) match theo PaperKind
        if (Enum.TryParse<PaperKind>(paperName, true, out var kind))
        {
            var byKind = settings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == kind);
            if (byKind != null) return byKind;
        }

        // 3) map nhanh A5/A4
        if (paperName.Equals("A5", StringComparison.OrdinalIgnoreCase))
            return settings.PaperSizes.Cast<PaperSize>()
                .FirstOrDefault(p => p.Kind == PaperKind.A5 || p.PaperName.ToUpper().Contains("A5"));

        if (paperName.Equals("A4", StringComparison.OrdinalIgnoreCase))
            return settings.PaperSizes.Cast<PaperSize>()
                .FirstOrDefault(p => p.Kind == PaperKind.A4 || p.PaperName.ToUpper().Contains("A4"));

        return null;
    }

    private static float[] BuildColumnWidthsPx(Graphics g, int availableWidthPx, float[] colMm)
    {
        float[] w = colMm.Select(mm => MmToPxX(g, mm)).ToArray();
        float total = w.Sum();

        if (total <= 0) throw new ArgumentException("Tổng độ rộng cột phải > 0.");

        if (total > availableWidthPx)
        {
            // scale xuống giữ tỉ lệ để vừa vùng in
            float scale = availableWidthPx / total;
            for (int i = 0; i < w.Length; i++) w[i] *= scale;
        }
        else
        {
            // dồn dư cho cột cuối cùng
            w[w.Length - 1] += (availableWidthPx - total);
        }

        return w;
    }

    private static void DrawRow(
        Graphics g, Pen pen, Font font, Brush brush, StringFormat sf, int padding,
        float x, float y, float rowH, float[] colW,
        string[] cells)
    {
        float cx = x;

        for (int i = 0; i < cells.Length; i++)
        {
            var rect = new RectangleF(cx, y, colW[i], rowH);

            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            var textRect = new RectangleF(
                rect.X + padding,
                rect.Y + padding,
                Math.Max(1, rect.Width - padding * 2),
                Math.Max(1, rect.Height - padding * 2));

            g.DrawString(cells[i] ?? "", font, brush, textRect, sf);

            cx += colW[i];
        }
    }

    private static float MeasureRowHeight(
        Graphics g, Font font, StringFormat sf, string[] cells, float[] colW, int padding)
    {
        float maxH = 0f;

        for (int i = 0; i < cells.Length; i++)
        {
            var size = g.MeasureString(
                cells[i] ?? "",
                font,
                new SizeF(Math.Max(1, colW[i] - padding * 2), 10000f),
                sf);

            float h = size.Height + padding * 2;
            if (h > maxH) maxH = h;
        }

        float minH = font.GetHeight(g) + padding * 2;
        return Math.Max(maxH, minH);
    }

    private static Margins MmMargins(int leftMm, int rightMm, int topMm, int bottomMm)
    {
        int ToHundredthsInch(int mm) => (int)Math.Round(mm / 25.4 * 100.0);
        return new Margins(
            ToHundredthsInch(leftMm),
            ToHundredthsInch(rightMm),
            ToHundredthsInch(topMm),
            ToHundredthsInch(bottomMm));
    }

    private static float MmToPxX(Graphics g, float mm) => mm * g.DpiX / 25.4f;
}

public static class KhsxPrintService
{
    // Tên hiển thị cho từng loại (có thể đổi dễ bằng dictionary)
    private static readonly Dictionary<string, string> TitleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Rut"] = "Kế hoạch sản xuất Rút",
        ["Ben"] = "Kế hoạch sản xuất Bện",
        ["QB"] = "Kế hoạch sản xuất Ghép lõi - Quấn Băng",
        ["BocLot"] = "Kế hoạch sản xuất Bóc Lót",
        ["BocMach"] = "Kế hoạch sản xuất Bóc Mạch",
        ["BocVo"] = "Kế hoạch sản xuất Bóc Vỏ",
    };

    /// <summary>
    /// In 1 danh sách DSPhatHanhKHSX theo bảng (có Title).
    /// </summary>
    public static bool PrintDsPhatHanh(
    List<DSPhatHanhKHSX> data,
    float[] colWidthsMm,
    string printerName = null,
    string paperName = "A5",
    bool landscape = true,
    string title = null,
    string nguoiLap = "Người lập",
    string tenNguoiLap = "")
    {
        if (data == null || data.Count == 0) return false;

        var headers = new[]
        {
        "Tên SP",
        "Mã KH",
        "Ghi Chú SX",
        "Ghi Chú Kế Hoạch"
    };

        var selectors = new Func<DSPhatHanhKHSX, string>[]
        {
        x => x?.Ten ?? "",
        x => x?.Lot ?? "",
        x => x?.GhiChuSX ?? "",
        x => x?.GhiChuKH ?? ""
        };

        var printer = new TableListPrinter<DSPhatHanhKHSX>(data, headers, selectors, colWidthsMm);

        printer.SetTitle(title)
               .SetFooterLines(nguoiLap, tenNguoiLap);

        return printer.Print(
            printerName: printerName,
            paperName: paperName,
            landscape: landscape
        );
    }


    /// <summary>
    /// Tách danhSach thành 6 list theo cờ (Rut/Ben/QB/BocLot/BocMach/BocVo),
    /// rồi in từng list (mỗi list = 1 print job / 1 file PDF nếu dùng Print to PDF).
    /// </summary>
    public static HashSet<(string Lot, int TrangThai, string Ten)> PrintDsPhatHanh_ByFlags(
        List<DSPhatHanhKHSX> danhSach,
        float[] colWidthsMm,
        string printerName = null,
        string paperName = "A5",
        bool landscape = true,
        string nguoiLap = "Người lập",
        string tenNguoiLap = "",
        bool isEdit = false
    )
    {
        var printed = new HashSet<(string Lot, int TrangThai, string Ten)>();

        if (danhSach == null || danhSach.Count == 0) return printed;

        // ✅ Nếu đang edit: không in, không check flags, nhưng vẫn trả HashSet
        if (isEdit)
        {
            foreach (var x in danhSach)
            {
                var lot = (x?.Lot ?? "").Trim();
                if (lot.Length == 0) continue;

                var ten = (x?.Ten ?? "").Trim();
                printed.Add((lot, x?.TrangThai ?? 0, ten));
            }
            return printed;
        }

        // --- logic cũ: lọc theo flags + in ---
        var groups = new List<(string Key, Func<DSPhatHanhKHSX, bool> Pred)>
    {
        ("Rut",     x => x != null && x.Rut),
        ("Ben",     x => x != null && x.Ben),
        ("QB",      x => x != null && x.QB),
        ("BocLot",  x => x != null && x.BocLot),
        ("BocMach", x => x != null && x.BocMach),
        ("BocVo",   x => x != null && x.BocVo),
    };

        foreach (var (key, pred) in groups)
        {
            var list = danhSach.Where(pred).ToList();
            if (list.Count == 0) continue;

            var title = TitleMap.TryGetValue(key, out var t) ? t : $"Kế hoạch sản xuất {key}";

            bool ok = PrintDsPhatHanh(
                data: list,
                colWidthsMm: colWidthsMm,
                printerName: printerName,
                paperName: paperName,
                landscape: landscape,
                title: title,
                nguoiLap: nguoiLap,
                tenNguoiLap: tenNguoiLap
            );

            if (ok)
            {
                foreach (var x in list)
                {
                    var lot = (x?.Lot ?? "").Trim();
                    if (lot.Length == 0) continue;

                    var ten = (x?.Ten ?? "").Trim();
                    printed.Add((lot, x?.TrangThai ?? 0, ten));
                }
            }
        }

        return printed;
    }




}
