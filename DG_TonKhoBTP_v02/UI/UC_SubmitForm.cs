using DG_TonKhoBTP_v02.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_SubmitForm : UserControl
    {
        public UC_SubmitForm()
        {
            InitializeComponent();
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            var host = this.FindForm();
            if (host == null) return;

            var snap = FormSnapshotBuilder.Capture(host);

            // [Luồng 5] Hậu xử lý cho CaiDatCDBoc: hỏi UC_TTSanPham để lấy bản gộp
            var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
            if (ucSanPham != null)
            {
                var merged = ucSanPham.GetCaiDatBocGop();
                // Ghi đè/đặt 1 key duy nhất cho CaiDatCDBoc trong snapshot
                snap.Sections["CaiDatCDBoc"] = merged;
                // (Optional) bỏ các key lẻ như UC_CaiDatMay/UC_DieuKienBoc/UC_CDBocLot nếu bạn đã thêm trước đó
            }

            StateStore.CurrentSnapshot = snap;

            // Lấy CaiDatCDBoc đã gộp
            var caiDat = StateStore.CurrentSnapshot.GetSection<CaiDatCDBoc>("CaiDatCDBoc");

            // Lấy danh sách NVL
            var listNVL = StateStore.CurrentSnapshot.GetSection<List<TTNVL>>("UC_TTNVL");

            // Lấy thông tin ca làm việc
            var ca = StateStore.CurrentSnapshot.GetSection<ThongTinCaLamViec>("UC_TTCaLamViec");


            MessageBox.Show("Đã lưu snapshot dữ liệu (đã gộp CaiDatCDBoc).", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            var host = this.FindForm();
            if (host == null) return;

            ControlCleaner.ClearAll(host);

            // Ngoài ra gọi ClearInputs() của từng section, nếu cần tinh chỉnh riêng
            foreach (Control c in host.Controls)
                ClearSectionRecursive(c);

        }

        private void ClearSectionRecursive(Control root)
        {
            if (root is IFormSection fs) fs.ClearInputs();
            foreach (Control child in root.Controls)
                ClearSectionRecursive(child);
        }
    }
}
