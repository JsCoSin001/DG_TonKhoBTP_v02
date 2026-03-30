using DocumentFormat.OpenXml.Office2010.PowerPoint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    public partial class FrmVatTuKhac : Form
    {

        List<string> khoList = new List<string>
        {
            "Kho nguyên vật liệu",
            "Kho văn phòng phẩm NM Đông Giang",
            "Kho Công cụ dụng cụ NM Đông Giang",
            "Kho khuôn",
            "Kho phế liệu",
            "Kho kéo đại",
            "Kho kéo trung",
            "Kho bện tao lần 1",
            "Kho kéo đa đường",
            "Kho bện nhóm, bện tao",
            "Kho bện đồng",
            "Kho bọc cách điện",
            "Kho bện ghép lõi",
            "Kho bọc lót",
            "Kho quấn băng thép,băng nhôm",
            "Kho quấn Mica",
            "Kho bọc thành phẩm",
            "Kho TP Đông Giang",
            "Kho NVL gia công",
            "Kho TP gia công"
        };


        public FrmVatTuKhac()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }

        private void FrmVatTuPhu_Load(object sender, EventArgs e) => showMuaVatTu();
        private void muaVậtTưToolStripMenuItem_Click(object sender, EventArgs e) => showMuaVatTu();

        private void showMuaVatTu(int kieuForm = 1) => ShowMediaControls(new UC_MuaVatTu(kieuForm));

        private void xuấtKhoToolStripMenuItem_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(false, 1, khoList));

        private void deNghiDichVuToolStrip_Click(object sender, EventArgs e) => showMuaVatTu(2); 
        private void deNghiVatTuToolStrip_Click(object sender, EventArgs e) => showMuaVatTu(1);
        private void nhapKhoVatTuToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(true,1, khoList));

        private void xacNhanDichVuToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(true, 2, khoList));

        private void xuấtKhoToolStripMenuItem1_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(false, 1, khoList));

        private void timKiemToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_BaoCao(khoList));

        private void ShowMediaControls(Control uc)
        {
            uc.Dock = DockStyle.Fill;
            pnMainVatTuPhu.Controls.Clear();
            pnMainVatTuPhu.Controls.Add(uc);
        }

    }
}
