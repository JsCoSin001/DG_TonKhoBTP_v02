using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer
{
    public static class SbplTextBuilder
    {
        private const char ESC = (char)0x1B;

        public static string BuildMultiLineTextBlock(
            List<TextLineSpec> lines,
            int startX = 10,
            int startY = 10,
            int lineSpacing = 70)  
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                int currentY = startY + i * lineSpacing;

                // Cùng Y cho prefix và content
                sb.Append($"{ESC}V{currentY:0000}");

                // --- PREFIX ---
                if (!string.IsNullOrEmpty(line.Prefix))
                {
                    int px = line.PrefixX ?? startX;
                    sb.Append($"{ESC}H{px:0000}");
                    sb.Append($"{ESC}P{(line.PrefixPitch ?? line.Pitch)}");
                    sb.Append($"{ESC}L{(line.PrefixEnlarge ?? line.Enlarge)}");
                    sb.Append($"{ESC}{(line.PrefixFontCmd ?? line.FontCmd)}{line.Prefix}");
                }

                // --- CONTENT ---
                if (line.X.HasValue)
                    sb.Append($"{ESC}H{line.X.Value:0000}");
                else if (string.IsNullOrEmpty(line.Prefix))
                    sb.Append($"{ESC}H{startX:0000}");

                sb.Append($"{ESC}P{(line.ContentPitch ?? line.Pitch)}");
                sb.Append($"{ESC}L{(line.ContentEnlarge ?? line.Enlarge)}");
                sb.Append($"{ESC}{(line.ContentFontCmd ?? line.FontCmd)}{(line.Content ?? string.Empty)}");
            }
            

            return sb.ToString();
        }


    }
}
