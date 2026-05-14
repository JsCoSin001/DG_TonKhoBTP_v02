using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public static class LabelConstants
    {
        // ─── Kích thước tờ A4 (mm) ───────────────────────────────────────────
        public const float A4WidthMm = 210f;
        public const float A4HeightMm = 297f;

        // ─── Kích thước 1 tem A6 (mm) ────────────────────────────────────────
        public const float LabelWidthMm = 105f;
        public const float LabelHeightMm = 148f;

        // ─── Padding bên trong tem (mm) ───────────────────────────────────────
        public const float PaddingMm = 2f;

        // ─── Chiều cao các vùng (mm) ─────────────────────────────────────────
        public const float HeaderHeightMm = 16f;   // logo + tên công ty + ĐT
        public const float SubHeaderHeightMm = 4f;    // đường kẻ phân cách
        public const float ProductAreaHeightMm = 22f;   // sản phẩm + dự án + KCS
        public const float FooterHeightMm = 9f;    // GOLDCUP | WIRE AND CABLE
        public const float RowHeightMm = 7f;    // chiều cao mỗi dòng data

        // ─── QR Code (mm) ────────────────────────────────────────────────────
        public const float QrSizeMm = 28f;   // chiều rộng = chiều cao QR
        public const float QrRightGapMm = 2f;   // khoảng cách QR với mép phải

        // ─── Watermark ───────────────────────────────────────────────────────
        public const float WatermarkOpacityDefault = 0.07f;   // 7% opacity
        public const float WatermarkSizeMm = 55f;     // logo lớn giữa tem

        // ─── Font names ──────────────────────────────────────────────────────
        public const string FontDefault = "Arial";
        public const string FontCompany = "Arial";

        // ─── Font sizes (point) ──────────────────────────────────────────────
        public const float FontSizeCompanyName = 10f;
        public const float FontSizeSubTitle = 6f;
        public const float FontSizeProductName = 7.5f;
        public const float FontSizeLabel = 6.5f;   // nhãn cột trái ("Chiều dài:")
        public const float FontSizeValue = 8f;     // giá trị cột phải
        public const float FontSizeProductCode = 8f;     // mã sản phẩm trong box
        public const float FontSizeFooter = 9f;
        public const float FontSizeBhCode = 5.5f;   // BH-QC-13-58-02

        // ─── Màu sắc (grayscale — in trắng đen) ─────────────────────────────
        public static readonly Color ColorBlack = Color.Black;
        public static readonly Color ColorWhite = Color.White;
        public static readonly Color ColorFooterBg = Color.Black;   // nền footer
        public static readonly Color ColorBorder = Color.Black;
        public static readonly Color ColorLightGray = Color.FromArgb(200, 200, 200);

        // ─── Nội dung cố định ────────────────────────────────────────────────
        public const string BhCode = "BH-QC-13-58-02";
        public const string CompanyName = "CÔNG TY CP ĐÔNG GIANG";
        public const string FactoryAddress = "Nhà máy: KCN Phố Nối A, xã Như Quỳnh, T. Hưng Yên";
        public const string PhoneFax = "ĐT: 0221.982.088     FAX: 0221.982.218     Website: www.goldcup.com.vn";
        public const string MadeIn = "Sản xuất tại Việt Nam";
        public const string FooterLeft = "GOLDCUP";
        public const string FooterRight = "WIRE AND CABLE  -  ISO 9001 : 2015";

        // ─── Nhãn các dòng dữ liệu ───────────────────────────────────────────
        public const string LblProduct = "Sản phẩm:";
        public const string LblProject = "Dự án:";
        public const string LblProductCode = "Mã sản phẩm:";
        public const string LblLength = "Chiều dài:";
        public const string LblCableWeight = "Khối lượng cáp:";
        public const string LblTotalWeight = "Khối lượng tổng:";
        public const string LblInspectionDate = "Ngày kiểm tra:";
        public const string LblInspector = "Người kiểm tra:";
        public const string LblQuality = "Đánh giá chất lượng:";
        public const string LblStandard = "Tiêu chuẩn sản xuất:";

        // ─── Số tem trên 1 tờ A4 ─────────────────────────────────────────────
        public const int LabelsPerRow = 2;
        public const int LabelsPerCol = 2;
        public const int LabelsPerPage = LabelsPerRow * LabelsPerCol; // = 4
    }
}
