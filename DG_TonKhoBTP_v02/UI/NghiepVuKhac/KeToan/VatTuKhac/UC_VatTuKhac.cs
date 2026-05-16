using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan;
using System;
using System.Data;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_VatTuKhac : UserControl
    {
        private DataTable _dsKho = new DataTable();

        public UC_VatTuKhac()
        {
            InitializeComponent();
            _dsKho = GetDSKho();
        }

        private DataTable GetDSKho()
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

        private void UC_VatTuKhac_Load(object sender, EventArgs e)
        {
            ShowMuaVatTu();
        }

        private void ShowMuaVatTu(int kieuForm = 1)
        {
            ShowMediaControls(new UC_MuaVatTu(kieuForm));
        }

        private void deNghiDichVuToolStrip_Click(object sender, EventArgs e)
        {
            ShowMuaVatTu(2);
        }

        private void xacNhanDichVuToolStrip_Click(object sender, EventArgs e)
        {
            ShowMediaControls(new UC_NhapXuatVatTu(true, 2, _dsKho));
        }

        private void deNghiVatTuToolStrip_Click(object sender, EventArgs e)
        {
            ShowMuaVatTu(1);
        }

        private void nhapKhoVatTuToolStrip_Click(object sender, EventArgs e)
        {
            ShowMediaControls(new UC_NhapXuatVatTu(true, 1, _dsKho));
        }

        private void xuatKhoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowMediaControls(new UC_NhapXuatVatTu(false, 1, _dsKho));
        }

        private void timKiemToolStrip_Click(object sender, EventArgs e)
        {
            ShowMediaControls(new UC_BaoCao(_dsKho));
        }

        private void ShowMediaControls(Control uc)
        {
            uc.Dock = DockStyle.Fill;

            pnMainVatTuPhu.SuspendLayout();
            pnMainVatTuPhu.Controls.Clear();
            pnMainVatTuPhu.Controls.Add(uc);
            pnMainVatTuPhu.ResumeLayout(true);
        }
    }
}