
using DG_TonKhoBTP_v02.Dictionary;
using System;
using System.Collections.Generic;

public enum StoreKeyKeHoach
{
    MucDoUuTien,
    TrangThaiBanHanhKH,
    TrangThaiThucHienTheoKH,
    // sau này thêm nữa thì add vào đây
}

public static class PermissionCodes
{
    // Các mã quyền được định nghĩa ở đây
    public const string NhapDuLieuCongDoan = "NHAP_DU_LIEU_CD";
    public const string BaoCaoTonKho = "BC_TON_KHO";
    public const string QuanDocKiemTra = "QD_KIEM_TRA";
    public const string TruyVetDuLieu = "TRUY_VET_DU_LIEU";
    public const string UpdateMaHang = "UPDATE_MA_HANG";
    public const string TachBin = "TACH_BIN";
    public const string GopBinHanNoi = "GOP_BIN_HAN_NOI";
    public const string HaLoNhapLai = "HA_LO_NHAP_LAI";
    public const string VatTu = "VAT_TU";
    public const string KiemKe = "KIEM_KE";
    public const string LapKeHoach = "LAP_KE_HOACH";
    public const string TrienKhaiKeHoach = "TRIEN_KHAI_KH";
    public const string CauHinhUngDung = "CAU_HINH_UNG_DUNG";
    public const string UserPermission = "USER_PERMISSION";
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Pro = "Pro";
    public const string Plan = "Plan";
    public const string Acc = "Acc";
    public const string Wh = "Wh";
    public const string Other = "Other";
}

public static class EnumStore
{

    private static List<string> May(params string[] codes)
           => new List<string>(codes);

    public static Dictionary<int, string> ErrorNVL = new Dictionary<int, string>()
    {
        { 0, "" },
        { 1, "Thiếu thông tin về Nguyên Liệu" },
        { 2, "Thiếu khối lượng còn lại" },
        { 3, "Thiếu chiều dài còn lại" },
        { 4, "Kiểm tra dữ liệu đo của NVL" },
        { 5, "Khối lượng còn lại phải nhỏ hơn khối lượng bắt đầu" },
        { 6, "Chiều dài còn lại phải nhỏ hơn chiều dài bắt đầu" },
        { 7, "QC chưa được nhập" }
    };


    public static Dictionary<int, string> ErrorCaLamViec = new Dictionary<int, string>()
    {
        { 1, "Máy chưa được chọn" },
        { 2, "Người thực hiện đang bị trống" }
    };

    public static readonly Dictionary<string, List<string>> MayTheoCongDoan =
        new Dictionary<string, List<string>>
        {
            { "KeoRut", May("R6", "R10", "R12", "MD16A4") },
            { "Ben_CU_AL", May("B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10", "B13", "B14", "B15", "B16", "B17") },
            { "BocCachDien", May("E1","E2", "E3", "E4", "E5", "E6","E7", "E8", "E9", "E11", "E12", "E13") },
            { "BocLot", May("E1", "E4", "E7", "E10","E13", "E14", "E15") },
            { "BocVo", May("E1", "E4", "E6", "E7", "E8", "E9", "E10", "E13", "E14", "E15") },
            { "GhepLoi", May("P1", "P2", "P3", "P4", "P5", "P6", "B6", "B10", "B13", "B14", "B15", "B16") },
            { "QB_AL_Cu", May("T1", "T2", "T5","B10", "B13", "B14", "B15", "B16", "B17") },
            { "QB_Mica", May("T3", "T4", "T5", "T6") },
        };


    public static class TieuDeFormVatTu
    {
        public const string DON_DE_NGHI_VAT_TU = "ĐƠN ĐỀ NGHỊ MUA VẬT TƯ";
        public const string DON_DE_NGHI_DICH_VU = "ĐƠN ĐỀ NGHỊ MUA DỊCH VỤ";
        public const string DON_XAC_NHAN_DICH_VU = "ĐƠN XÁC NHẬN DỊCH VỤ";
        public const string NHAP_VAT_TU = "ĐƠN NHẬP VẬT TƯ";
        public const string XUAT_VAT_TU = "ĐƠN XUẤT VẬT TƯ";
    }

    public static Dictionary<int, string> ErrorTP = new Dictionary<int, string>()
    {
        { 1, "Lỗi bất thường, liên hệ Mr.Linh để xử lý!" },
        { 2, "Kiểm tra lại mã Bin" },
        { 3, "Thiếu khối lượng Thành phẩm" },
        { 4, "Thiếu chiều dài Thành phẩm" },
    };


    //public static string[] dsTenMayBoQuaKiemTraKhoiLuongConLai = { "MD16A4", "R6", "R10", "R12" , "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10", "B13", "B14", "B15", "B16", "B17" };

    //public static string[] dsTenMayTuDongTinhKLConLai = { "B9" , "B10" };

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


    // Là tình trạng thực hiện kế hoạch của sản xuất: cbxTrangThaiThucHienKH : TrangThaiSXKH_Text
    public static Dictionary<int, string> TrangThaiThucHienTheoKH = new Dictionary<int, string>()
    {
        { -1, "-- Toàn bộ --" },
        { 0, "Chưa ban hành" },
        { 1, "Đang thực hiện" },
        { 2, "Đã xong" },
        { 3, "Đã huỷ" }
    };

    // Là tình trạng của kế hoạch đưa xuống dưới sản xuất: cbxTinhTrangKH: TinhTrangKH_Text
    public static Dictionary<int, string> TrangThaiBanHanhKH = new Dictionary<int, string>()
    {
        { -1, "-- Toàn bộ --" },
        { 0, "Huỷ" },
        { 1, "Đã ban hành" },
        { 2, "Tạm thời" }
    };

    // Là tình trạng của đơn hàng theo yêu cầu của các bên: TinhTrangDonKH_Text
    public static Dictionary<int, string> MucDoUuTien = new Dictionary<int, string>()
    {
        { -1, "-- Toàn bộ --" },
        { 0, "Gấp" },
        { 1, "Đúng kế hoạch"},
        { 2, "Bình thường" }
    };



    public static Dictionary<int, string> Get(StoreKeyKeHoach key)
    {
        switch (key)
        {
            case StoreKeyKeHoach.MucDoUuTien:
                return MucDoUuTien;

            case StoreKeyKeHoach.TrangThaiBanHanhKH:
                return TrangThaiBanHanhKH;

            case StoreKeyKeHoach.TrangThaiThucHienTheoKH:
                return TrangThaiThucHienTheoKH;

            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, "StoreKey không hợp lệ");
        }
    }




}
