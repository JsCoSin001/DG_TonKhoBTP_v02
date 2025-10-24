// File: Core/Interfaces.cs
// Mục đích: Định nghĩa hợp đồng cho các UC để thu gom/clear dữ liệu.
// [Luồng 1] UC_SubmitForm sẽ tìm tất cả control thực thi IFormSection để lấy dữ liệu.

using System;
using System.Data;

namespace DG_TonKhoBTP_v02.Core
{
    public interface IFormSection
    {
        /// <summary>
        /// [Luồng 2] Trả về dữ liệu section (DTO/Model bất kỳ). 
        /// Nên là class POCO hoặc List&lt;T&gt;.
        /// </summary>
        object GetData();

        /// <summary>
        /// [Luồng 6] Dọn sạch input người dùng trong section.
        /// </summary>
        void ClearInputs();

        /// <summary>
        /// [Phụ] Tên section để gắn key trong snapshot.
        /// </summary>
        string SectionName { get; }
    }
    public interface IDataReceiver
    {
        void LoadData(DataTable dt);
    }
}
