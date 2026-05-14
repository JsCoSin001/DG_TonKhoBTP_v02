using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Printer.TemXuatHang
{
    public class LabelData
    {
        /// <summary>
        /// Tên/loại sản phẩm.
        /// Ví dụ: "Cáp ngầm nhôm 0.6/1kV\nAL/XLPE/PVC/DSTA/PVC 4x150mm2"
        /// (có thể chứa \n để xuống dòng trong vùng in)
        /// </summary>
        public string ProductType { get; set; } = string.Empty;

        /// <summary>
        /// Mã sản phẩm — đồng thời là nội dung được encode vào QR Code.
        /// Ví dụ: "E10-261743/7-01"
        /// </summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Chiều dài cáp, bao gồm đơn vị.
        /// Ví dụ: "180 m"
        /// </summary>
        public string Length { get; set; } = string.Empty;

        /// <summary>
        /// Dải chiều dài (mét đầu ~ mét cuối).
        /// Ví dụ: "( 0000 ~ 0180 )"
        /// </summary>
        public string LengthRange { get; set; } = string.Empty;

        /// <summary>
        /// Khối lượng riêng của cáp.
        /// Ví dụ: "730 Kg"
        /// </summary>
        public string CableWeight { get; set; } = string.Empty;

        /// <summary>
        /// Khối lượng tổng (cáp + tang).
        /// Ví dụ: "945 Kg"
        /// </summary>
        public string TotalWeight { get; set; } = string.Empty;

        /// <summary>
        /// Ngày kiểm tra chất lượng.
        /// Ví dụ: "23/04/2026"
        /// </summary>
        public string InspectionDate { get; set; } = string.Empty;

        /// <summary>
        /// Tên + mã người kiểm tra.
        /// Ví dụ: "Nguyễn Huy Toàn-DG112"
        /// </summary>
        public string Inspector { get; set; } = string.Empty;

        /// <summary>
        /// Kết quả đánh giá chất lượng.
        /// Ví dụ: "Đạt"
        /// </summary>
        public string QualityResult { get; set; } = string.Empty;

        /// <summary>
        /// Tiêu chuẩn sản xuất.
        /// Ví dụ: "IEC 60502-1"
        /// </summary>
        public string Standard { get; set; } = string.Empty;

        /// <summary>
        /// Tên dự án. Có thể để rỗng.
        /// </summary>
        public string Project { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LABEL PRINT CONFIG — cấu hình dùng chung cho toàn bộ lần in
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cấu hình tĩnh dùng chung cho toàn bộ lần in:
    /// đường dẫn ảnh, ngày ban hành, mức độ mờ watermark...
    /// Truyền vào cùng với danh sách <see cref="LabelData"/>.
    /// </summary>
    public class LabelPrintConfig
    {
        // ── Đường dẫn ảnh ────────────────────────────────────────────────────

        /// <summary>
        /// Đường dẫn ảnh logo Goldcup nhỏ — hiển thị ở góc trái header.
        /// Đây là ảnh số 4 trong yêu cầu.
        /// Ví dụ: @"C:\Images\goldcup_logo.png"
        /// </summary>
        public string LogoSmallPath { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn ảnh chứng nhận ISO/QUACERT — hiển thị góc phải header.
        /// Đây là ảnh số 5 trong yêu cầu.
        /// Ví dụ: @"C:\Images\iso_badge.png"
        /// </summary>
        public string CertLogoPath { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn ảnh badge "KCS PASSED" — hiển thị góc phải vùng sản phẩm.
        /// Đây là ảnh số 6 trong yêu cầu.
        /// Ví dụ: @"C:\Images\kcs_passed.png"
        /// </summary>
        public string KcsLogoPath { get; set; } = string.Empty;

        // ── Watermark ─────────────────────────────────────────────────────────

        /// <summary>
        /// Mức độ mờ của logo watermark lớn in giữa tem.
        /// Giá trị từ 0.0 (trong suốt) đến 1.0 (đục hoàn toàn).
        /// Mặc định: 0.07 (7%) — đủ thấy nhưng không che nội dung.
        /// </summary>
        public float WatermarkOpacity { get; set; } = 0.07f;

        // ── Thông tin cố định ─────────────────────────────────────────────────

        /// <summary>
        /// Ngày ban hành — giá trị cố định, không tính theo ngày in.
        /// Hiển thị ở góc phải trên cùng của tem.
        /// Ví dụ: "Ban hành: 13/12/2025"
        /// </summary>
        public string PublishedDate { get; set; } = string.Empty;

        // ── Tùy chọn in ───────────────────────────────────────────────────────

        /// <summary>
        /// Tên máy in. Nếu để rỗng sẽ dùng máy in mặc định của hệ thống.
        /// </summary>
        public string PrinterName { get; set; } = string.Empty;

        /// <summary>
        /// Hiển thị PrintPreviewDialog trước khi in thật.
        /// Mặc định: true.
        /// </summary>
        public bool ShowPreview { get; set; } = true;

        // ── Factory method tạo config mặc định ───────────────────────────────

        /// <summary>
        /// Tạo config với các giá trị mặc định — tiện cho việc khởi tạo nhanh.
        /// </summary>
        public static LabelPrintConfig CreateDefault(
            string logoSmallPath,
            string certLogoPath,
            string kcsLogoPath,
            string publishedDate)
        {
            return new LabelPrintConfig
            {
                LogoSmallPath = logoSmallPath,
                CertLogoPath = certLogoPath,
                KcsLogoPath = kcsLogoPath,
                PublishedDate = publishedDate,
                WatermarkOpacity = 0.07f,
                ShowPreview = true
            };
        }
    }
}
