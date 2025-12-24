using System.Collections.Generic;

public static class EnumStore
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

    public static class Icon
    {
        public const string Success = "ok";
        public const string Warning = "warning";
        public const string NoneLogin = "noneLogin";
        public const string LoginSuccess = "loginSuccess";
    }

    public static Dictionary<string, string> TransferPermissionName = new Dictionary<string, string>()
    {
        { "CAN_READ", "Chỉ đọc" },
        { "CAN_WRITE", "Thêm mới" },
        { "CAN_UPDATE", "Chỉnh sửa" },
        { "CAN_DELETE", "Xoá" },
        { "CAN_PERMISSION", "Phân Quyền" },
        { "CAN_SET_DB", "Đặt đường dẫn" },
        { "CAN_STOP_SOFTWARE", "Dừng chương trình" },
        { "CAN_SET_PRINTER", "Cài đặt máy in" },
    };

    public static Dictionary<string, string> Group = new Dictionary<string, string>
    {
        {"ShowCongCu","Acc" },
        {"Admin","Admin" },
        {"KiemTraBaoCaoSX","Pro" },
        {"CapNhatTonKho","Wh" },
        {"Chung","Other" },
    };

    public static class ThongBao
    {
        public const string YeuCauDangNhap = "Bạn cần đăng nhập để sử dụng chức năng này";
        public const string YeuCauCapQuyen = "Bạn cần cấp quyền để sử dụng chức năng này";
    }


}
