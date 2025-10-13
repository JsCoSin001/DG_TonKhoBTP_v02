using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Validator = DG_TonKhoBTP_v02.Helper.Validator;

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
            #region Lấy thông tin từ các section
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
            #endregion

            //Tuỳ chọn: Lưu tạm
            DG_TonKhoBTP_v02.Core.StateStore.CurrentSnapshot = snap;

            #region Kiểm tra tính hợp lệ của thongTinCaLamViec
            ThongTinCaLamViec thongTinCaLamViec = (ThongTinCaLamViec) snap.Sections["UC_TTCaLamViec"];

            if (!Validator.TTCaLamViec(thongTinCaLamViec))
            {
                MessageBox.Show("Thông tin ở ca làm việc đang thiếu dữ liệu", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            #endregion

            #region Kiểm tra tính hợp lệ dữ liệu của uc NVL
            var list_TTNVL = snap.Sections["UC_TTNVL"] as List<TTNVL>;

            if (!Validator.TTNVL(list_TTNVL))
            {
                MessageBox.Show("Thông tin NGUYÊN LIỆU chưa hợp lệ", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            #endregion

            #region Kiểm tra tính hợp lệ của dữ liệu thành phẩm công đoạn
            TTThanhPham thongTinThanhPham = (TTThanhPham) snap.Sections["UC_TTThanhPham"];

            if (!Validator.TTThanhPham(thongTinThanhPham))
            {
                MessageBox.Show("Thiếu THÔNG TIN TP của CÔNG ĐOẠN", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            #endregion

            #region Kiểm tra tính hợp lệ dữ liệu của chi tiết các công đoạn
            List<object> chiTietCD = Validator.KiemTraChiTietCongDoan(snap);

            if (chiTietCD[0] == null)
            {
                MessageBox.Show("Chi tiết công đoạn chưa hợp lệ", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            #endregion

            #region Lưu và thông báo trạng thái lưu
            bool isSaved = DatabaseHelper.SaveDataSanPham(thongTinCaLamViec, thongTinThanhPham, list_TTNVL, chiTietCD);

            if(isSaved)
            {
                MessageBox.Show("Lưu dữ liệu thành công", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ControlCleaner.ClearAll(host);
            }
            else
            {
                MessageBox.Show("Lưu dữ liệu thất bại", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion

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
