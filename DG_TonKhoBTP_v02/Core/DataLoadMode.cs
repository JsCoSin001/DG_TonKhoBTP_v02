#nullable enable

namespace DG_TonKhoBTP_v02.Core
{
    /// <summary>
    /// Phân biệt mục đích nạp dữ liệu vào các IDataReceiver.
    /// Giữ nguyên các giá trị 0/1/2 đang được dự án sử dụng và bổ sung Draft = 3.
    /// </summary>
    public enum DataLoadMode
    {
        New = 0,
        Copy = 1,
        OfficialEdit = 2,
        Draft = 3
    }
}
