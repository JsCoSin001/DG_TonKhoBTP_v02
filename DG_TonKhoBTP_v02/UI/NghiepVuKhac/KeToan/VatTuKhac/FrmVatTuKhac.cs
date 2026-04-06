using DG_TonKhoBTP_v02.Database;
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

        private DataTable _dsKho = new DataTable();

        private DataTable getDSKho()
        {
            const string sql = @"
                SELECT 
                    id,
                    KiHieu,
                    TenKho,
                    GhiChu
                FROM DanhSachKho
                ORDER BY id ASC;";

            return DatabaseHelper.GetData(sql);
        }


        public FrmVatTuKhac()
        {
            InitializeComponent();
            _dsKho = getDSKho();

            this.WindowState = FormWindowState.Maximized;
        }



        private void FrmVatTuPhu_Load(object sender, EventArgs e) => showMuaVatTu();
        private void muaVậtTưToolStripMenuItem_Click(object sender, EventArgs e) => showMuaVatTu();

        private void showMuaVatTu(int kieuForm = 1) => ShowMediaControls(new UC_MuaVatTu(kieuForm));

        private void xuấtKhoToolStripMenuItem_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(false, 1, _dsKho));

        private void deNghiDichVuToolStrip_Click(object sender, EventArgs e) => showMuaVatTu(2); 
        private void deNghiVatTuToolStrip_Click(object sender, EventArgs e) => showMuaVatTu(1);
        private void nhapKhoVatTuToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(true,1, _dsKho));

        private void xacNhanDichVuToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(true, 2, _dsKho));

        private void xuấtKhoToolStripMenuItem1_Click(object sender, EventArgs e) => ShowMediaControls(new UC_NhapXuatVatTu(false, 1, _dsKho));

        private void timKiemToolStrip_Click(object sender, EventArgs e) => ShowMediaControls(new UC_BaoCao(_dsKho));

        private void ShowMediaControls(Control uc)
        {
            uc.Dock = DockStyle.Fill;
            pnMainVatTuPhu.Controls.Clear();
            pnMainVatTuPhu.Controls.Add(uc);
        }

    }
}
