using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Helper;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTSanPham : UserControl
    {
        public UC_TTSanPham(params Control[] controls)
        {
            InitializeComponent();
            this.StartForm(controls);
        }

        private void StartForm(params Control[] controls)
        {
            foreach (var ctrl in controls)
            {
                ctrl.Dock = DockStyle.Top;
                this.panel1.Controls.Add(ctrl);
            }

        }

        #region AI generated
        // Danh sách provider “cắm” vào (theo thứ tự ưu tiên khi gộp)
        //private readonly List<ISectionProvider<CaiDatCDBoc>> _caiDatProviders = new List<ISectionProvider<CaiDatCDBoc>>();
        // Constructor gốc của bạn (đã kéo thả sẵn UI)
        // Ví dụ hiện tại: new UC_TTSanPham(new UC_CDBocLot(), new UC_DieuKienBoc(), new UC_CaiDatMay())
        //public UC_TTSanPham(UC_CDBocLot ucBocLot, UC_DieuKienBoc ucDieuKien, UC_CaiDatMay ucCaiDat)
        //{
        //    InitializeComponent();

        // [Luồng 4.1] Đăng ký các provider theo thứ tự ưu tiên
        //_caiDatProviders.Add(ucCaiDat);   // Ưu tiên 1
        //_caiDatProviders.Add(ucDieuKien); // Ưu tiên 2
        //_caiDatProviders.Add(ucBocLot);   // Ưu tiên 3

        // (Tuỳ ý) đặt các UC con vào panel của bạn nếu cần bằng code; 
        // còn nếu đã kéo-thả sẵn thì bỏ qua phần add Controls.
        //}

        /// <summary>
        /// [Luồng 4.2] Lấy bản gộp CaiDatCDBoc cho “sản phẩm”.
        /// Hàm này có thể được gọi bởi SubmitForm (sau khi Capture) hoặc ở nơi bạn cần.
        /// </summary>
        //public CaiDatCDBoc GetCaiDatBocGop()
        //{
        //    return SectionMerger.MergeParts(_caiDatProviders);
        //}

        // [Phụ] Nếu sau này thay UC khác:
        // - Truyền UC mới implements ISectionProvider<CaiDatCDBoc> vào ctor.
        // - Thứ tự _caiDatProviders quyết định độ ưu tiên giá trị.


        /// <summary>
        /// Trả về các section đã tổng hợp tự-động từ mọi ISectionProvider&lt;T&gt; bên trong UC_TTSanPham.
        /// Key mặc định = tên lớp T (vd "CaiDatCDBoc", "CD_BenRuot"...).
        /// </summary>
        public IDictionary<string, object> GetAggregateSections()
        {
            // Quét từ *chính UC_TTSanPham* trở xuống.
            return ProviderAggregator.AggregateSectionsFrom(this);
        }

        #endregion


    }
}
