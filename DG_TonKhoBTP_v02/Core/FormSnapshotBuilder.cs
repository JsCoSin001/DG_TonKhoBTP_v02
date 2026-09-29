// File: Core/FormSnapshotBuilder.cs
// Mục đích: Duyệt cây control, lấy tất cả IFormSection -> nhét vào FormSnapshot.
// [Luồng 4] Được gọi khi btnLuu click.

using DG_TonKhoBTP_v02.UI;
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
            return CaptureInternal(hostForm, useDraftData: false);
        }

        /// <summary>
        /// Thu thập dữ liệu dành riêng cho Lưu tạm.
        /// Section nào hỗ trợ IDraftFormSection sẽ dùng GetDraftData();
        /// các section còn lại giữ nguyên GetData() như hiện tại.
        /// </summary>
        public static FormSnapshot CaptureDraft(Form hostForm)
        {
            return CaptureInternal(hostForm, useDraftData: true);
        }

        private static FormSnapshot CaptureInternal(Form hostForm, bool useDraftData)
        {
            var snap = new FormSnapshot();

            foreach (var section in EnumerateSections(hostForm))
            {
                try
                {
                    object data;
                    if (useDraftData && section is IDraftFormSection draftSection)
                        data = draftSection.GetDraftData();
                    else
                        data = section.GetData();

                    snap.Sections[section.SectionName] = data;
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Lỗi khi thu thập dữ liệu từ section: {section.SectionName}");
                    throw new System.InvalidOperationException(
                        $"Không thể lấy dữ liệu từ {section.SectionName}.", ex);
                }
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
