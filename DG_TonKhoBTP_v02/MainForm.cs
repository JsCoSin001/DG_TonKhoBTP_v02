
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using DG_TonKhoBTP_v02.UI.Actions;
using DG_TonKhoBTP_v02.UI.Authentication;
using DG_TonKhoBTP_v02.UI.Setting;
using DocumentFormat.OpenXml.Drawing;
using QLDuLieuTonKho_BTP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02
{
    public partial class MainForm : Form
    {
        private string _URL = Properties.Settings.Default.URL;
        private string _ver = "2.0";


        public MainForm()
        {
            InitializeComponent();
            DatabaseHelper.SetDatabasePath(_URL);
            lblAuthor.Text = $"Được phát triển bởi Linh - Version: {_ver} - All rights reserved.";

            DatabasehelperVer01.SetDatabasePath(_URL);

            SetThongTinUser();

            ShowHomePage();
        }

        


        #region Hàm log cấu trúc control
        private void button1_Click(object sender, EventArgs e)
        {
            // Log cấu trúc control sau khi ShowUI
            LogControlsTree(pnShow, s => Console.WriteLine(s));
        }

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
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnKeoRut.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.KeoRut;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(new UC_CDKeoRut());
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD, rawMaterial: true);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert("Lỗi khởi tạo giao diện kéo rút: " + ex.Message);
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnKeoRut.Enabled = true;
                }
            }
        }

        private void btnBenRuot_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnBenRuot.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.BenRuot;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(new UC_CDBenRuot());
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert("Lỗi khởi tạo giao diện bẻn ruột: " + ex.Message);
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnBenRuot.Enabled = true;
                }
            }
        }

        private void btnGhepLoi_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnGhepLoi.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.GhepLoi;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(new UC_CDGhepLoiQB());
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert("Lỗi khởi tạo giao diện ghép lõi: " + ex.Message);
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnGhepLoi.Enabled = true;
                }
            }
        }

        private void btnQuanBang_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnQuanBang.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.QuanBang;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(new UC_CDGhepLoiQB());
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện quấn băng: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnQuanBang.Enabled = true;
                }
            }
        }

        private void btnMica_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnMica.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.Mica;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(new UC_CDGhepLoiQB());
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện mica: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnMica.Enabled = true;
                }
            }
        }

        private void btnBocLot_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnBocLot.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.BocLot;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(
                        new UC_CDBocLot(),
                        new UC_DieuKienBoc(),
                        new UC_CaiDatMay()
                    );
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện bọc lót: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnBocLot.Enabled = true;
                }
            }
        }

        private void btnBocMach_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnBocMach.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.BocMach;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(
                        new UC_CDBocMach(),
                        new UC_DieuKienBoc(),
                        new UC_CaiDatMay()
                    );
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện bọc mạch: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnBocMach.Enabled = true;
                }
            }
        }

        private void btnBocVo_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnBocVo.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    CongDoan thongTinCD = ThongTinChungCongDoan.BocVo;
                    List<ColumnDefinition> columns = thongTinCD.Columns;
                    var ucSanPham = new UC_TTSanPham(
                        new UC_CDBocVo(),
                        new UC_DieuKienBoc(),
                        new UC_CaiDatMay()
                    );
                    Panel pnBottom = UI_BottomPanel(columns, ucSanPham, thongTinCD);

                    ShowUI(thongTinCD, pnBottom);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện bọc vỏ: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnBocVo.Enabled = true;
                }
            }
        }

        private void btnCapNhatMaHang_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnCapNhatMaHang.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    pnShow.Controls.Clear();
                    var uc = new UC_CapNhatSP
                    {
                        Dock = DockStyle.Fill
                    };
                    pnShow.Controls.Add(uc);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện cập nhật mã hàng: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnCapNhatMaHang.Enabled = true;
                }
            }
        }

        private void btnBaoCaoTonKho_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnBaoCaoTonKho.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    pnShow.Controls.Clear();
                    var uc = new UC_TonKho
                    {
                        Dock = DockStyle.Fill
                    };
                    pnShow.Controls.Add(uc);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện báo cáo tồn kho: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnBaoCaoTonKho.Enabled = true;
                }
            }
        }

        #endregion

        #region Tạo panel UI theo công đoạn
        private Panel UI_TopPanel(CongDoan cd)
        {
            Panel pnTop = new Panel { Dock = DockStyle.Top, AutoSize = true };

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

            var ucEdit = FindChild<UC_Edit>(pnEdit_Report);
            if (ucEdit != null)
            {
                // Đăng ký theo instance vừa render
                ucEdit.DataTableSubmitted += (s, dt) =>
                {
                    // Broadcast dt tới TẤT CẢ control implements IDataReceiver trong pnShow
                    BroadcastToReceivers(pnShow, dt);
                };
            }

            return pnRight;
        }

        private Panel UI_Edit_Report(CongDoan cd)
        {
            Panel pnEdit_Report = new Panel();
            // Đặt pnEdit_Report lên Top
            pnEdit_Report.Dock = DockStyle.Top;
            pnEdit_Report.Height = 120;

            UC_Edit uC_Edit = new UC_Edit(cd);
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

        private void BroadcastToReceivers(Control root, DataTable dt)
        {
            if (root == null || dt == null) return;

            foreach (var receiver in FindAll<IDataReceiver>(root))
            {
                try
                {
                    receiver.LoadData(dt);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadData error on {receiver.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static IEnumerable<T> FindAll<T>(Control parent)
        {
            if (parent == null) yield break;

            foreach (Control child in parent.Controls)
            {
                if (child is T t) yield return t;

                foreach (var sub in FindAll<T>(child))
                    yield return sub;
            }
        }

        private static TControl FindChild<TControl>(Control parent) where TControl : Control
        {
            if (parent == null) return null;

            foreach (Control child in parent.Controls)
            {
                if (child is TControl t) return t;

                var found = FindChild<TControl>(child);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region Hiển thị Giao diện Main
        private void ShowUI(CongDoan cd, Panel pnBottom)
        {
            pnShow.Controls.Clear();
            Panel pnTop = this.UI_TopPanel(cd);
            pnShow.Controls.Add(pnBottom);
            pnShow.Controls.Add(pnTop);
        }
        #endregion

        private void btnTruyVetDL_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            btnTruyVetDL.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    pnShow.Controls.Clear();
                    var uc = new UC_TruyVetDuLieu
                    {
                        Dock = DockStyle.Fill
                    };
                    pnShow.Controls.Add(uc);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện truy vết dữ liệu: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    btnTruyVetDL.Enabled = true;
                }
            }
        }

        private async void setiingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Setting settingForm = new Setting();
            settingForm.StartPosition = FormStartPosition.CenterParent;

            // Load dữ liệu với waiting form trước khi show dialog
            await settingForm.LoadDataAsync();

            settingForm.ShowDialog();
        }

        private void ShowHomePage()
        {
            // Xóa các control cũ trong panel
            pnShow.Controls.Clear();
            Uc_HomePage homePage = new Uc_HomePage();

            pnShow.Dock = DockStyle.Fill;
            pnShow.Controls.Add(homePage);
            homePage.Dock = DockStyle.Fill;
            homePage.lblVersion.Text = "Phiên bản: v" + _ver;
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHomePage();
        }

        private void imgLogo_Click(object sender, EventArgs e)
        {
            ShowHomePage();
        }

        private void lblTenCty_Click(object sender, EventArgs e)
        {
            ShowHomePage();
        }

        private void BtnKiemTraBc_Click(object sender, EventArgs e)
        {
            if (Helper.Helper.KiemTraEmpty(_URL))
                return;

            HighlightMenuButton((Button)sender);
            BtnKiemTraBc.Enabled = false;

            using (var waiting = new FrmWaiting("ĐANG KHỞI TẠO GIAO DIỆN..."))
            {
                try
                {
                    waiting.ShowAndRefresh();

                    pnShow.SuspendLayout();
                    pnShow.Visible = false;

                    pnShow.Controls.Clear();
                    var uc = new UC_KTraBC
                    {
                        Dock = DockStyle.Fill
                    };
                    pnShow.Controls.Add(uc);
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert($"Lỗi khởi tạo giao diện kiểm tra BC: {ex.Message}");
                }
                finally
                {
                    pnShow.Visible = true;
                    pnShow.ResumeLayout(true);
                    waiting?.CloseAndDispose();
                    BtnKiemTraBc.Enabled = true;
                }
            }
        }

        #region Tô màu nút

        private void ResetMenuButtons(Control container)
        {
            foreach (Control ctl in container.Controls)
            {
                if (ctl is Button btn)
                {
                    btn.BackColor = SystemColors.Control;
                    btn.ForeColor = SystemColors.ControlText;
                }

                // Nếu control hiện tại còn chứa controls con → đệ quy tiếp
                if (ctl.HasChildren)
                {
                    ResetMenuButtons(ctl);
                }
            }
        }

        private void SetActiveButton(Button btn)
        {
            btn.BackColor = Color.Gainsboro;  // màu nút được chọn
            btn.ForeColor = Color.Black;
        }

        private void HighlightMenuButton(Button activeButton)
        {
            ResetMenuButtons(fpnButton);
            SetActiveButton(activeButton);
        }

        #endregion

        
        private void lblUserName_MouseLeave(object sender, EventArgs e)
        { 
            ThayDoiMau(false);
        }

        private void ThayDoiMau(bool flg)
        {
            tbUser.BackColor = flg ? Color.LightGray : SystemColors.Control;
        }

        private void lblUserName_MouseEnter(object sender, EventArgs e)
        {
            ThayDoiMau(true);
        }

        private void pdropdown_MouseEnter(object sender, EventArgs e)
        {
            ThayDoiMau(true);
        }

        private void pdropdown_MouseLeave(object sender, EventArgs e)
        {
            ThayDoiMau(false);
        }

        private ContextMenuStrip userMenu;
        private ToolStripMenuItem mnuLogout;

        private void MnuLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn có chắc muốn logout?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            UserContext.Clear();
            SetThongTinUser();
        }


        private void InitUserMenu()
        {
            userMenu = new ContextMenuStrip();
            userMenu.Font = new Font("Tahoma", 10F);

            mnuLogout = new ToolStripMenuItem("Đăng xuất");

            mnuLogout.Font = new Font("Tahoma", 10F, FontStyle.Bold);

            mnuLogout.Click += MnuLogout_Click;

            userMenu.Items.Add(new ToolStripSeparator());
            userMenu.Items.Add(mnuLogout);
        }


        private void pdropdown_Click(object sender, EventArgs e)
        {
            runEventClickUser();
        }

        private void lblUserName_Click(object sender, EventArgs e)
        {
            runEventClickUser();
        }

        private void lblChucDanh_Click(object sender, EventArgs e)
        {
            runEventClickUser();
        }

        private void avatar_Click(object sender, EventArgs e)
        {
            runEventClickUser();
        }


        private void runEventClickUser()
        {
            if (!UserContext.IsAuthenticated)
                ShowLoginForm();
            else
            {
                System.Drawing.Point mousePos = Cursor.Position;
                InitUserMenu();
                userMenu.Show(
                    mousePos.X,
                    mousePos.Y
                );
            }
        }



        private void ShowLoginForm()
        {
            using (Login fmLogin = new Login())
            {
                fmLogin.StartPosition = FormStartPosition.CenterParent;

                var result = fmLogin.ShowDialog(this);

                // 👇 Sau khi LoginForm đóng
                if (result == DialogResult.OK && UserContext.IsAuthenticated)
                {
                    SetThongTinUser();   // cập nhật UI theo user
                }
                else
                {
                    // login thất bại hoặc user đóng form
                    // có thể không làm gì
                }
            }
        }

        private void SetThongTinUser()
        {
            string iconAvatar = UserContext.IsAuthenticated ? EnumStore.Icon.LoginSuccess : EnumStore.Icon.NoneLogin;
            lblUserName.Text = UserContext.Name == null? "Đăng nhập" : UserContext.Name;
            //lblChucDanh.Text = UserContext.Roles == null ? "Chưa đăng nhập" : UserContext.Roles[0];
            avatar.Image = Image.FromFile(@"Assets\" + iconAvatar + ".ico");
            PhanQuyen(true);
            //PhanQuyen(UserContext.IsAuthenticated
            //);
        }

        private void PhanQuyen(bool quyen)
        {
            userRegistration.Visible = quyen;
            grbCongCu.Visible = quyen;
        }


        private void đăngKýToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FmDangKy fmDangKy = new FmDangKy();
            {
                fmDangKy.StartPosition = FormStartPosition.CenterParent;
                fmDangKy.ShowDialog();
            }
        }

        private void lblChucDanh_MouseEnter(object sender, EventArgs e)
        {
            ThayDoiMau(true);
        }

        private void lblChucDanh_MouseLeave(object sender, EventArgs e)
        {
            ThayDoiMau(false);
        }

        private void avatar_MouseEnter(object sender, EventArgs e)
        {
            ThayDoiMau(true);
        }

        private void avatar_MouseLeave(object sender, EventArgs e)
        {
            ThayDoiMau(false);
        }

    }
}
