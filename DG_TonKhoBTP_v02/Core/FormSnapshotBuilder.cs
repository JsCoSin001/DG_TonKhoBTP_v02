// File: Core/FormSnapshotBuilder.cs
// Mục đích: Duyệt cây control, lấy tất cả IFormSection -> nhét vào FormSnapshot.
// [Luồng 4] Được gọi khi btnLuu click.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Core
{
    public static class FormSnapshotBuilder
    {
        /// <summary>
        /// [Luồng 4] Duyệt toàn bộ Form để gom dữ liệu từ các section (IFormSection).
        /// </summary>
        public static FormSnapshot Capture(Form hostForm)
        {
            var snap = new FormSnapshot();

            foreach (var section in EnumerateSections(hostForm))
            {
                var data = section.GetData(); // Mỗi UC tự chịu trách nhiệm map UI -> DTO
                // Tên section duy nhất, ví dụ: "UC_TTNVL", "UC_CaiDatMay", ...
                snap.Sections[section.SectionName] = data;
            }

            return snap;
        }

        private static IEnumerable<IFormSection> EnumerateSections(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is IFormSection fs)
                    yield return fs;

                foreach (var fsChild in EnumerateSections(c))
                    yield return fsChild;
            }
        }
    }
}
