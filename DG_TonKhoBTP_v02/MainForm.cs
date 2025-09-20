
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02
{
    public partial class MainForm : Form
    {
        private string _URL = "D:\\Database\\QLSX_DG_v2.db"; 
        public MainForm()
        {
            InitializeComponent();  
        }


        private Panel UI_TopPanel(CongDoan cd)
        {
            Panel pnTop = new Panel();
            pnTop.Dock = DockStyle.Top;
            pnTop.AutoSize = true;
                        
            UC_TTCaLamViec uc_caLamViec = new UC_TTCaLamViec(cd.DanhSachMay, _URL, cd.TenCongDoan);
            uc_caLamViec.Dock = DockStyle.Top;

            UC_TTThanhPham uC_TTThanhPham = new UC_TTThanhPham();
            uC_TTThanhPham.Dock = DockStyle.Top;

            pnTop.Controls.Add(uC_TTThanhPham);
            pnTop.Controls.Add(uc_caLamViec);

            return pnTop;
        }

        private Panel UI_BottomPanel(List<ColumnDefinition> columns, Control productInfoControl)
        {
            Panel pnBottom = new Panel();
            pnBottom.Dock = DockStyle.Fill;
            pnBottom.AutoSize = false;

            // pn Bottom - Left
            #region Tạo UI panel ở dưới - bên trái  - Đặt size = 800
            Panel pnLeft = UI_BottomLeftPanel(columns);
            #endregion

            // pn Bottom - Right
            #region Tạo UI panel ở dưới - bên phải - Đặt full kích thước
            Panel pnRight = UI_BottomRightPanel(productInfoControl);
            #endregion

            pnBottom.Controls.Add(pnRight);
            pnBottom.Controls.Add(pnLeft);

            return pnBottom;
        }

        private Panel UI_BottomRightPanel(Control productInfoControl)
        {
            var pnRight = new Panel { Dock = DockStyle.Fill };

            productInfoControl.Dock = DockStyle.Top;

            // Top: SubmitForm
            var uC_SubmitForm = new UC_SubmitForm { Dock = DockStyle.Top };

            // Edit/Report
            Panel pnEdit_Report = UI_Edit_Report();

            pnRight.Controls.Add(pnEdit_Report);
            pnRight.Controls.Add(uC_SubmitForm);
            pnRight.Controls.Add(productInfoControl);

            return pnRight;
        }


        private Panel UI_Edit_Report()
        {
            Panel pnEdit_Report = new Panel();
            // Đặt pnEdit_Report lên Top
            pnEdit_Report.Dock = DockStyle.Top;
            pnEdit_Report.Height = 120;

            UC_Edit uC_Edit = new UC_Edit();
            // Đặt Form Sửa số liệu bên trái panel pnEdit_Report
            uC_Edit.Dock = DockStyle.Left;
            uC_Edit.Width = 500;

            UC_Report uC_Report = new UC_Report();
            // Đặt Form báo cáo toàn panel pnEdit_Report
            uC_Report.Dock = DockStyle.Fill;

            pnEdit_Report.Controls.Add(uC_Report);
            pnEdit_Report.Controls.Add(uC_Edit);

            return pnEdit_Report;
        }

        private Panel UI_BottomLeftPanel(List<ColumnDefinition> columns)
        {
            Panel pnLeft = new Panel();
            pnLeft.Dock = DockStyle.Left;
            pnLeft.AutoSize = false;
            pnLeft.Width = 800;

            UC_TTNVL uC_TTNVL = new UC_TTNVL(columns);
            uC_TTNVL.Dock = DockStyle.Fill;
            pnLeft.Controls.Add(uC_TTNVL);
            return pnLeft;
        }

        private void ShowUI(CongDoan cd, Panel pnBottom)
        {
            pnShow.Controls.Clear();
            Panel pnTop = this.UI_TopPanel(cd);
            pnShow.Controls.Add(pnBottom);
            pnShow.Controls.Add(pnTop);
        }

        private void btnKeoRut_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.KeoRut;
            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDKeoRut()
            );            

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnBenRuot_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.BenRuot;

            List<ColumnDefinition> columns = thongTinCD.Columns;


            var ucSanPham = new UC_TTSanPham(
                new UC_CDBenRuot()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnGhepLoiQB_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.GhepLoi_QB;

            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDGhepLoiQB()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnBocLot_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.BocLot;

            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDBocLot(),
                new UC_DieuKienBoc(),
                new UC_CaiDatMay()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnBocMach_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.BocMach;

            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDBocMach(),
                new UC_DieuKienBoc(),
                new UC_CaiDatMay()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnBocVo_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.BocVo;

            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDBocVo(),
                new UC_DieuKienBoc(),
                new UC_CaiDatMay()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham);

            this.ShowUI(thongTinCD, pnBottom);
        }
    }
}
