
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02
{
    public partial class MainForm : Form
    {
        private string _URL = "D:\\Database\\QLSX_v02.db";
        //UC_TTCaLamViec uc_caLamViec;

        public MainForm()
        {
            InitializeComponent();
            DatabaseHelper.SetDatabasePath(_URL);
        }

        #region Hàm log cấu trúc control
        public static void LogControlsTree(Control root, Action<string> log = null)
        {
            if (root == null) return;

            // KHẮC PHỤC: không gán trực tiếp Debug.WriteLine; dùng lambda hoặc chọn Trace/Console
            if (log == null)
            {
#if DEBUG
                log = s => Debug.WriteLine(s);    // Chạy khi build Debug
#else
            log = s => Trace.WriteLine(s);    // Hoặc Console.WriteLine(s);
#endif
            }

            log($"Root {NodeText(root)}");
            Dump(root, log, "");
        }

        private static void Dump(Control parent, Action<string> log, string prefix)
        {
            int count = parent.Controls.Count;
            for (int i = 0; i < count; i++)
            {
                var child = parent.Controls[i];
                bool isLast = i == count - 1;
                string connector = isLast ? "└─" : "├─";
                log($"{prefix}{connector} {NodeText(child)}");

                string childPrefix = prefix + (isLast ? "   " : "│  ");
                if (child.HasChildren)
                    Dump(child, log, childPrefix);
            }
        }

        private static string NodeText(Control c)
        {
            string name = string.IsNullOrWhiteSpace(c.Name) ? "(no name)" : c.Name;
            return $"{name} : {c.GetType().Name} [{c.Left},{c.Top},{c.Width}x{c.Height}]";
        }
        #endregion

        #region Hiển thị UI theo công đoạn
        private void btnKeoRut_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.KeoRut;
            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDKeoRut()
            );            

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD, rawMaterial: true);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnBenRuot_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.BenRuot;

            List<ColumnDefinition> columns = thongTinCD.Columns;


            var ucSanPham = new UC_TTSanPham(
                new UC_CDBenRuot()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

            this.ShowUI(thongTinCD, pnBottom);
        }

        private void btnGhepLoiQB_Click(object sender, EventArgs e)
        {
            CongDoan thongTinCD = ThongTinChungCongDoan.GhepLoi_QB;

            List<ColumnDefinition> columns = thongTinCD.Columns;

            var ucSanPham = new UC_TTSanPham(
                new UC_CDGhepLoiQB()
            );

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

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

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

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

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

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

            Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

            this.ShowUI(thongTinCD, pnBottom);
        }
        #endregion

        #region Tạo panel UI theo công đoạn
        private Panel UI_TopPanel(CongDoan cd)
        {
            Panel pnTop = new Panel();
            pnTop.Dock = DockStyle.Top;
            pnTop.AutoSize = true;

            UC_TTCaLamViec uc_caLamViec = new UC_TTCaLamViec(cd.DanhSachMay, _URL, cd.TenCongDoan);
            uc_caLamViec.Dock = DockStyle.Top;

            UC_TTThanhPham uc_TTThanhPham = new UC_TTThanhPham(cd);
            uc_TTThanhPham.Dock = DockStyle.Top;

            uc_caLamViec.Event_ChonMay += (value) => uc_TTThanhPham.ChonMay(value); ;

            pnTop.Controls.Add(uc_TTThanhPham);
            pnTop.Controls.Add(uc_caLamViec);

            return pnTop;
        }

        private Panel UI_BottomPanel(List<ColumnDefinition> columns, Control productInfoControl, CongDoan cd, bool rawMaterial = false)
        {
            Panel pnBottom = new Panel();
            pnBottom.Dock = DockStyle.Fill;
            pnBottom.AutoSize = false;

            // pn Bottom - Left
            #region Tạo UI panel ở dưới - bên trái  - Đặt size = 800
            Panel pnLeft = UI_BottomLeftPanel(columns, rawMaterial);
            #endregion

            // pn Bottom - Right
            #region Tạo UI panel ở dưới - bên phải - Đặt full kích thước
            Panel pnRight = UI_BottomRightPanel(productInfoControl,cd);
            #endregion

            pnBottom.Controls.Add(pnRight);
            pnBottom.Controls.Add(pnLeft);

            return pnBottom;
        }

        private Panel UI_BottomRightPanel(Control productInfoControl, CongDoan cd)
        {
            var pnRight = new Panel { Dock = DockStyle.Fill };

            productInfoControl.Dock = DockStyle.Top;

            // Top: SubmitForm
            var uC_SubmitForm = new UC_SubmitForm { Dock = DockStyle.Top };

            // Edit/Report
            Panel pnEdit_Report = UI_Edit_Report(cd);

            pnRight.Controls.Add(pnEdit_Report);
            pnRight.Controls.Add(uC_SubmitForm);
            pnRight.Controls.Add(productInfoControl);

            return pnRight;
        }

        private Panel UI_Edit_Report(CongDoan cd)
        {
            Panel pnEdit_Report = new Panel();
            // Đặt pnEdit_Report lên Top
            pnEdit_Report.Dock = DockStyle.Top;
            pnEdit_Report.Height = 120;

            UC_Edit uC_Edit = new UC_Edit();
            // Đặt Form Sửa số liệu bên trái panel pnEdit_Report
            uC_Edit.Dock = DockStyle.Left;
            uC_Edit.Width = 500;

            UC_Report uC_Report = new UC_Report(cd);
            // Đặt Form báo cáo toàn panel pnEdit_Report
            uC_Report.Dock = DockStyle.Fill;

            pnEdit_Report.Controls.Add(uC_Report);
            pnEdit_Report.Controls.Add(uC_Edit);

            return pnEdit_Report;
        }

        private Panel UI_BottomLeftPanel(List<ColumnDefinition> columns, bool rawMaterial)
        {
            Panel pnLeft = new Panel();
            pnLeft.Dock = DockStyle.Left;
            pnLeft.AutoSize = false;
            pnLeft.Width = 800;

            UC_TTNVL uC_TTNVL = new UC_TTNVL(columns);
            uC_TTNVL.Dock = DockStyle.Fill;
            uC_TTNVL.SetStatusRawMaterial(rawMaterial);
            pnLeft.Controls.Add(uC_TTNVL);
            return pnLeft;
        }
        #endregion

        #region Hiển thị Giao diện
        private void ShowUI(CongDoan cd, Panel pnBottom)
        {
            pnShow.Controls.Clear();
            Panel pnTop = this.UI_TopPanel(cd);
            pnShow.Controls.Add(pnBottom);
            pnShow.Controls.Add(pnTop);
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            // Log cấu trúc control sau khi ShowUI
            LogControlsTree(pnShow, s => Console.WriteLine(s));
        }
    }
}
