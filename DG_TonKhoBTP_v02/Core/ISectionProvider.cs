// File: Core/ISectionProvider.cs
// [Luồng 1] Chuẩn hoá “UC cung cấp dữ liệu model T”.

using System;

namespace DG_TonKhoBTP_v02.Core
{
    public interface ISectionProvider<out T>
    {
        /// <summary>
        /// [Luồng 2] Trả về phần dữ liệu (có thể "partial") của model T.
        /// Chỉ điền những trường UC này quản lý; phần còn lại để null/0/empty.
        /// </summary>
        T GetSectionData();

        /// <summary>
        /// [Phụ] Tên khu vực (để debug/log).
        /// </summary>
        string SectionName { get; }
    }
}
