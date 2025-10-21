using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer
{
    public class TextLineSpec
    {
        // Vị trí cột cho CONTENT (nếu null thì in nối sau prefix; nếu không có prefix -> dùng startX)
        public int? X { get; set; }

        // Cấu hình chung (fallback)
        public string Content { get; set; } = "";
        public string FontCmd { get; set; } = "M";
        public string Enlarge { get; set; } = "0101";
        public int Pitch { get; set; } = 5;

        // Nhãn (prefix)
        public string Prefix { get; set; } = "";
        public int? PrefixX { get; set; }                  // nếu null -> dùng startX
        public string PrefixFontCmd { get; set; } = "M";   // mặc định M
        public string PrefixEnlarge { get; set; }
        public int? PrefixPitch { get; set; }

        // Nội dung (content)
        public string ContentFontCmd { get; set; } = "XM"; // mặc định XM
        public string ContentEnlarge { get; set; }
        public int? ContentPitch { get; set; }
    }




}
