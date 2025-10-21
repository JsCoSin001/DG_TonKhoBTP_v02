using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer
{
    public static class Qr
    {
        public static string GeneratedQr(
            string data,
            int? posX = null,    // cho phép null
            int? posY = null,    // cho phép null
            int cellSize = 8,
            char ec = 'H',
            int mode = 0,
            int rotation = 0)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("QR data rỗng.");

            const char ESC = (char)0x1B;

            // chỉ thêm lệnh X/Y nếu có giá trị
            string H = posX.HasValue ? $"{ESC}H{posX.Value:0000}" : "";
            string V = posY.HasValue ? $"{ESC}V{posY.Value:0000}" : "";

            string setup = $"{ESC}2D30,{ec},{cellSize:00},{mode},{rotation}";
            int len = Encoding.ASCII.GetByteCount(data);
            string dn = $"{ESC}DN{len:0000},{data}";

            var sb = new StringBuilder();
            sb.Append(H).Append(V).Append(setup).Append(dn);
            return sb.ToString();
        }


    }
}
