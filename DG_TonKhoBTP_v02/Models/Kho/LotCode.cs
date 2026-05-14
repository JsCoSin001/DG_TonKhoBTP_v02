using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models.Kho
{
    public class LotCode
    { 
        /// <summary>Số m của lô (ký tự chính - vị trí A)</summary>
        public string SoM { get; set; } = string.Empty;

        /// <summary>Chiều cao lô (subscript bên trái của SoM - vị trí B)</summary>
        public string ChieuCao { get; set; } = string.Empty;

        /// <summary>Chỉ số đầu (subscript bên phải của SoM - vị trí C)</summary>
        public string SoDau { get; set; } = string.Empty;

        /// <summary>Số thứ tự của lô so với KHSX (phần đầu superscript - vị trí D)</summary>
        public string SoThuTu { get; set; } = string.Empty;

        /// <summary>Tên khách (phần giữa superscript - vị trí E)</summary>
        public string TenKhach { get; set; } = string.Empty;

        /// <summary>Chỉ số ngoài (phần trong ngoặc của superscript - vị trí F)</summary>
        public string SoCuoi { get; set; } = string.Empty;

        /// <summary>Kế hoạch sản xuất (subscript của dấu đóng ngoặc - vị trí G)</summary>
        public string KHSX { get; set; } = string.Empty;

        /// <summary>Màu sắc (superscript của dấu đóng ngoặc - vị trí H)</summary>
        public string MauSac { get; set; } = string.Empty;
    }
}
