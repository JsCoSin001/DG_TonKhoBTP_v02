// File: Core/StateStore.cs
// Mục đích: Biến toàn cục lưu snapshot cuối cùng khi bấm Lưu.
// [Luồng 5] Sau khi capture, UC_SubmitForm gán vào đây.

namespace DG_TonKhoBTP_v02.Core
{
    public static class StateStore
    {
        public static FormSnapshot CurrentSnapshot { get; set; }
    }
}
