using System.Collections.Generic;

public static class ErrorStore
{
    public static Dictionary<int, string> ErrorNVL = new Dictionary<int, string>()
    {
        { 0, "" },
        { 1, "Thiếu thông tin về Nguyên Liệu" },
        { 2, "Thiếu khối lượng còn lại" },
        { 3, "Thiếu chiều dài còn lại" },
        { 4, "Kiểm tra dữ liệu đo của NVL" },
        { 5, "Khối lượng còn lại phải nhỏ hơn khối lượng bắt đầu" },
        { 6, "Chiều dài còn lại phải nhỏ hơn chiều dài bắt đầu" }
    };

    public static Dictionary<int, string> ErrorCaLamViec = new Dictionary<int, string>()
    {
        { 1, "Máy chưa được chọn" },
        { 2, "Người thực hiện đang bị trống" }
    };

    public static Dictionary<int, string> ErrorTP = new Dictionary<int, string>()
    {
        { 1, "Lỗi bất thường, liên hệ Mr.Linh để xử lý!" },
        { 2, "Kiểm tra lại mã Bin" },
        { 3, "Thiếu khối lượng Thành phẩm" },
        { 4, "Thiếu chiều dài Thành phẩm" },
    };
}
