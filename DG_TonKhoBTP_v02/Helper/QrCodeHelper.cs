using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Danh sách cố định các loại mã QR/MaBin mà hệ thống hỗ trợ.
    /// Khi hệ thống có thêm loại mã mới, bổ sung tại đây và mapping trong ParseType().
    /// </summary>
    public enum QrCodeType
    {
        Unknown = 0,
        CuonTP = 1,
        Lot = 2
    }

    /// <summary>
    /// Kết quả sau khi phân tích chuỗi QR/MaBin.
    /// </summary>
    public sealed class QrParseResult
    {
        public bool IsValid { get; set; }
        public QrCodeType Type { get; set; }
        public string SearchValue { get; set; }
        public List<string> ExtraValues { get; set; }
        public string RawValue { get; set; }
        public string ErrorMessage { get; set; }

        public QrParseResult()
        {
            Type = QrCodeType.Unknown;
            SearchValue = string.Empty;
            ExtraValues = new List<string>();
            RawValue = string.Empty;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Helper dùng chung để phân loại và tách dữ liệu QR/MaBin.
    ///
    /// Format có cấu trúc:
    ///     [loai];[gia-tri-tim-kiem];[noi-dung-khac...]
    ///
    /// Ví dụ:
    ///     cuontp;MB000123;LOT01
    ///     lot;LOT000123
    ///
    /// Với chuỗi không có cấu trúc, ví dụ:
    ///     MB000123
    /// nơi gọi phải truyền defaultType để xác định chuỗi đó thuộc loại nào.
    ///
    /// Helper này chỉ parse/validate chuỗi, KHÔNG truy cập database.
    /// </summary>
    public static class QrCodeHelper
    {
        public const char Separator = ';';

        /// <summary>
        /// Phân tích chuỗi QR/MaBin.
        ///
        /// Nếu chuỗi có cấu trúc, type trong chuỗi luôn được ưu tiên.
        /// Nếu chuỗi không có cấu trúc, helper sử dụng defaultType.
        /// </summary>
        public static QrParseResult Parse(string rawValue, QrCodeType defaultType)
        {
            QrParseResult result = new QrParseResult();
            result.RawValue = rawValue ?? string.Empty;

            string input = (rawValue ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                result.ErrorMessage = "Mã QR/MaBin không được để trống.";
                return result;
            }

            // Không có dấu ';' => chuỗi không có cấu trúc.
            // Type được quyết định bởi nơi gọi thông qua defaultType.
            if (input.IndexOf(Separator) < 0)
            {
                if (defaultType == QrCodeType.Unknown)
                {
                    result.ErrorMessage = "Không xác định được loại mã. Hãy truyền defaultType cho chuỗi không có cấu trúc.";
                    return result;
                }

                result.IsValid = true;
                result.Type = defaultType;
                result.SearchValue = input;
                return result;
            }

            string[] parts = input.Split(new[] { Separator }, StringSplitOptions.None);

            string typeText = parts.Length > 0
                ? (parts[0] ?? string.Empty).Trim()
                : string.Empty;

            string searchValue = parts.Length > 1
                ? (parts[1] ?? string.Empty).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(typeText))
            {
                result.ErrorMessage = "Mã QR không có loại mã ở phần tử đầu tiên.";
                return result;
            }

            QrCodeType type = ParseType(typeText);
            if (type == QrCodeType.Unknown)
            {
                result.ErrorMessage = "Loại mã QR không được hỗ trợ: " + typeText;
                return result;
            }

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                result.ErrorMessage = "Mã QR không có giá trị tìm kiếm ở phần tử thứ hai.";
                return result;
            }

            result.Type = type;
            result.SearchValue = searchValue;

            // Từ phần tử thứ 3 trở đi là dữ liệu bổ sung.
            for (int i = 2; i < parts.Length; i++)
            {
                result.ExtraValues.Add((parts[i] ?? string.Empty).Trim());
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Overload tiện dụng khi chỉ muốn parse QR có cấu trúc.
        /// Chuỗi không có cấu trúc sẽ không hợp lệ vì không có defaultType.
        /// </summary>
        public static QrParseResult Parse(string rawValue)
        {
            return Parse(rawValue, QrCodeType.Unknown);
        }

        /// <summary>
        /// Chuyển phần loại trong QR sang enum.
        /// Không phân biệt chữ hoa/chữ thường.
        /// </summary>
        private static QrCodeType ParseType(string typeText)
        {
            string normalized = (typeText ?? string.Empty).Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "cuontp":
                    return QrCodeType.CuonTP;

                case "lot":
                    return QrCodeType.Lot;

                default:
                    return QrCodeType.Unknown;
            }
        }
    }
}
