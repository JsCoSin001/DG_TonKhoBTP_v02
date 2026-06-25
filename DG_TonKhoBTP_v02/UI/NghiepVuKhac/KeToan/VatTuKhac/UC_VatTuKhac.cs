using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
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
            UC_MuaVatTu_v2 uc = new UC_MuaVatTu_v2(1);
            ShowMediaControls(uc);

            //ShowMuaVatTu();
        }

        private void ShowMuaVatTu(int kieuForm = 1)
        {
            ShowMediaControls(new UC_MuaVatTu(kieuForm));
        }

        private void deNghiDichVuToolStrip_Click(object sender, EventArgs e)
        {
            UC_MuaVatTu_v2 uc = new UC_MuaVatTu_v2(2);
            ShowMediaControls(uc);

            //ShowMuaVatTu(2);
        }

        private void xacNhanDichVuToolStrip_Click(object sender, EventArgs e)
        {
            //ShowMediaControls(new UC_NhapXuatVatTu(true, 2, _dsKho));

            KieuNhapXuat_Model md = new KieuNhapXuat_Model
            {
                Id = 5,
                Ten = TenKieuNhapXuat.DICH_VU,
                IsNhap = false,
                IsDichVu = true,
                IsKhac = true
            };

            UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(md);

            ShowMediaControls(uc);

        }

        private void deNghiVatTuToolStrip_Click(object sender, EventArgs e)
        {
            UC_MuaVatTu_v2 uc = new UC_MuaVatTu_v2(1);
            ShowMediaControls(uc);

            //ShowMuaVatTu(1);
        }

        private void nhapKhoVatTuToolStrip_Click(object sender, EventArgs e)
        {
            //ShowMediaControls(new UC_NhapXuatVatTu(true, 1, _dsKho));

            //UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(isNhapKhac: false, isNhapHang:true,isDichVu:false);
            //ShowMediaControls(uc);
        }

        private void xuatKhoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //ShowMediaControls(new UC_NhapXuatVatTu(false, 1, _dsKho));

            //UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(isNhapKhac: false, isNhapHang: false, isDichVu: false);
            //ShowMediaControls(uc);
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

        private void theoĐơnĐềNghịToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KieuNhapXuat_Model md = new KieuNhapXuat_Model
            {
                Id = 1,
                Ten = TenKieuNhapXuat.NHAP_KHO_THEO_DON,
                IsNhap = true,
                IsDichVu = false,
                IsKhac = false
            };

            UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(md);
            ShowMediaControls(uc);
        }

        private void khácToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KieuNhapXuat_Model md = new KieuNhapXuat_Model
            {
                Id = 2,
                Ten = TenKieuNhapXuat.NHAP_KHO_KHAC,
                IsNhap = true,
                IsDichVu = false,
                IsKhac = true
            };

            UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(md);
            ShowMediaControls(uc);
        }

        private void đơnĐềNghịToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KieuNhapXuat_Model md = new KieuNhapXuat_Model
            {
                Id = 3,
                Ten = TenKieuNhapXuat.XUAT_THEO_DON,
                IsNhap = false,
                IsDichVu = false,
                IsKhac = false
            };

            UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(md);
            ShowMediaControls(uc);
        }

        private void khácToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            KieuNhapXuat_Model md = new KieuNhapXuat_Model
            {
                Id = 4,
                Ten = TenKieuNhapXuat.XUAT_KHAC,
                IsNhap = false,
                IsDichVu = false,
                IsKhac = true
            };

            UC_NhapXuatVatTu_v2 uc = new UC_NhapXuatVatTu_v2(md);
            
            ShowMediaControls(uc);
        }
    }
}