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

            var snap = DG_TonKhoBTP_v02.Core.FormSnapshotBuilder.Capture(host);

            // lấy UC_TTSanPham ở đâu đó trong form
            var ucSanPham = Helper.Helper.FindControlRecursive<UC_TTSanPham>(host);
            if (ucSanPham != null)
            {
                // TỰ-ĐỘNG gom tất cả providers hiện có
                var extra = ucSanPham.GetAggregateSections();
                foreach (var kv in extra)
                    snap.Sections[kv.Key] = kv.Value; // vd "CaiDatCDBoc", "CD_BenRuot", "CD_BocLot"...
            }

            DG_TonKhoBTP_v02.Core.StateStore.CurrentSnapshot = snap;


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
