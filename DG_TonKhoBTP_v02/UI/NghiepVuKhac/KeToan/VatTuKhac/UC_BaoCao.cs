using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan.VatTuKhac;
using DG_TonKhoBTP_v02.UI.Helper;          // WaitingHelper (có sẵn)
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuKhac
{
    public partial class UC_BaoCao : UserControl
    {
        // ─── Fields ────────────────────────────────────────────────────────────
        private readonly DataGridView grvBaoCao = new DataGridView();

        // ─── Filter fields (giống UC_MonthyReport) ─────────────────────────────
        private Panel _filterPanel;
        private TextBox[] _filterBoxes;
        private DataView _dataView;

        private int? GetSelectedKhoId()
        {
            if (cbxdsKho.SelectedIndex <= 0)
                return null;

            if (cbxdsKho.SelectedValue == null ||
                cbxdsKho.SelectedValue == DBNull.Value)
                return null;

            return Convert.ToInt32(cbxdsKho.SelectedValue);
        }

        private const string COL_CHECK = "colChon";
        private const string COL_UPDATE = "colUpdate";
        private const string COL_DELETE = "colDelete";

        // ─── Constructor ───────────────────────────────────────────────────────
        public UC_BaoCao(DataTable khoList)
        {
            InitializeComponent();
            InitComboBoxes(khoList);

            // Đăng ký sự kiện một lần duy nhất tại đây
            grvBaoCao.CellContentClick += GrvBaoCao_CellContentClick;

            // Hiển thị các button chỉnh trạng thái
            bool isAcess =
                !UserContext.IsAuthenticated ||
                (!UserContext.HasRole(RoleNames.Acc) &&
                 !UserContext.HasRole(RoleNames.Admin));

            cbxAll.Visible = !isAcess;
            btnChinhTrangThai.Visible = !isAcess;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════════════

        private void InitComboBoxes(DataTable khoList)
        {
            cbxThoiGian.SelectedIndex = 0;
            cbxLoaiYC.SelectedIndex = 0;

            LoadNguoiThucHien();

            // Clone cấu trúc + copy dữ liệu
            var newList = khoList.Copy();

            // Thêm dòng "Không cần" vào đầu
            DataRow newRow = newList.NewRow();
            newRow["id"] = 0;
            newRow["KiHieu"] = "";
            newRow["TenKho"] = "Không cần";
            newRow["GhiChu"] = DBNull.Value;

            newList.Rows.InsertAt(newRow, 0);

            cbxdsKho.DataSource = newList;
            cbxdsKho.DisplayMember = "TenKho";
            cbxdsKho.ValueMember = "ID";
            cbxdsKho.SelectedIndex = 0;

            cbxKieu.SelectedIndex = 0;
        }

        private void LoadNguoiThucHien()
        {
            DataTable dt = BaoCao_DB.GetNguoiThucHien();

            // Tạo dòng mới và chèn vào vị trí 0
            DataRow row = dt.NewRow();
            row["username"] = "Không cần";
            dt.Rows.InsertAt(row, 0);

            // Đổ dữ liệu vào ComboBox
            cbxNguoiThucHien.DataSource = dt;
            cbxNguoiThucHien.DisplayMember = "username";
            cbxNguoiThucHien.ValueMember = "username";

            // Chọn mặc định dòng đầu tiên
            cbxNguoiThucHien.SelectedIndex = 0;
        }

        private async void btnIn_Out_Click(object sender, EventArgs e)
        {
            cbxAll.Enabled = false;
            lblTieuDe.Text = "BÁO CÁO XUẤT - NHẬP - TỒN";

            SetToolbarEnabled(false);

            DataTable dt = null;

            try
            {
                int? khoId = GetSelectedKhoId();

                DateTime? ngayBatDau =
                    cbxThoiGian.SelectedIndex == 0
                        ? null
                        : (DateTime?)dtBatDau.Value;

                DateTime? ngayKetThuc =
                    cbxThoiGian.SelectedIndex == 0
                        ? null
                        : (DateTime?)dtKetThuc.Value;

                await WaitingHelper.RunWithWaiting(
                    async () =>
                    {
                        dt = await Task.Run(
                            () => BaoCao_DB.TinhTonKho(
                                ngayBatDau,
                                ngayKetThuc,
                                khoId));
                    },
                    "ĐANG TẢI LỊCH SỬ XUẤT NHẬP...");

                if (cbxExportExcel.Checked)
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert(
                            "Không có dữ liệu để xuất.",
                            "Export",
                            EnumStore.Icon.Warning);

                        return;
                    }

                    string filePath = null;

                    using (var sfd = new SaveFileDialog
                    {
                        Title = "Xuất báo cáo Excel",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName =
                            $"BaoCao_XuatNhapTon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                    })
                    {
                        if (sfd.ShowDialog() != DialogResult.OK)
                        {
                            FrmWaiting.ShowGifAlert(
                                "Huỷ quá trình xuất Excel",
                                "Export",
                                EnumStore.Icon.Warning);

                            return;
                        }

                        filePath = sfd.FileName;
                    }

                    await WaitingHelper.RunWithWaiting(
                        () => ExcelExporter.ExportToPath(
                            dt,
                            filePath,
                            cbxXuatTCVN.Checked
                                ? ExcelExportTextFormat.TCVN
                                : ExcelExportTextFormat.Unicode),
                        "ĐANG XUẤT FILE EXCEL...");

                    FrmWaiting.ShowGifAlert(
                        "Đã xuất Excel thành công!",
                        "Export",
                        EnumStore.Icon.Success);

                    return;
                }

                // ── Bind + format (có waiting) ──
                await WaitingHelper.RunWithWaiting(
                    async () =>
                    {
                        // Nhường frame cho waiting render
                        await Task.Delay(30);

                        if (cbxMoCuaSo.Checked)
                        {
                            MoCuaSoMoi(
                                dt,
                                applyFormatTimDL: false,
                                tieuDe: "BÁO CÁO XUẤT - NHẬP - TỒN");
                        }
                        else
                        {
                            HienThiLenLuoi(
                                dt,
                                applyFormatTimDL: false);
                        }
                    },
                    "ĐANG HIỂN THỊ DỮ LIỆU...");
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(
                        ex,
                        "BÁO CÁO XUẤT - NHẬP - TỒN"),
                    "LỖI TẢI DỮ LIỆU");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        private async void btnTimDL_Click(object sender, EventArgs e)
        {
            cbxAll.Enabled = true;
            cbxAll.Checked = false;

            lblTieuDe.Text = "BÁO CÁO";

            int kho = cbxdsKho.SelectedIndex;

            string nguoiThucHien =
                cbxNguoiThucHien.SelectedIndex == 0
                    ? ""
                    : cbxNguoiThucHien.SelectedItem.ToString();

            int tinhTrang = cbxLoaiYC.SelectedIndex;
            int kieu = cbxKieu.SelectedIndex;
            bool exportExcel = cbxExportExcel.Checked;

            SetToolbarEnabled(false);

            DataTable dt = null;

            DateTime? ngayBatDau =
                cbxThoiGian.SelectedIndex == 0
                    ? null
                    : (DateTime?)dtBatDau.Value;

            DateTime? ngayKetThuc =
                cbxThoiGian.SelectedIndex == 0
                    ? null
                    : (DateTime?)dtKetThuc.Value;

            try
            {
                // ── Bước 1: Query DB + chuẩn bị dữ liệu ──
                await WaitingHelper.RunWithWaiting(
                    async () =>
                    {
                        dt = await Task.Run(
                            () =>
                            {
                                switch (kieu)
                                {
                                    case 1:
                                        return BaoCao_DB.GetBaoCaoDatHang(
                                            nguoiThucHien,
                                            ngayBatDau,
                                            ngayKetThuc);

                                    case 2:
                                        return BaoCao_DB.GetBaoCaoLichSuXuatNhap(
                                            kho,
                                            nguoiThucHien,
                                            true,
                                            ngayBatDau,
                                            ngayKetThuc);

                                    case 3:
                                        return BaoCao_DB.GetBaoCaoLichSuXuatNhap(
                                            kho,
                                            nguoiThucHien,
                                            false,
                                            ngayBatDau,
                                            ngayKetThuc);

                                    default:
                                        return BaoCao_DB.GetBaoCaoDatHang(
                                            nguoiThucHien,
                                            ngayBatDau,
                                            ngayKetThuc);
                                }
                            });
                    },
                    "ĐANG TẢI DỮ LIỆU BÁO CÁO...");

                // ── Export Excel ──
                if (exportExcel)
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert(
                            "Không có dữ liệu để xuất.",
                            "Export",
                            EnumStore.Icon.Warning);

                        return;
                    }

                    string filePath = null;

                    using (var sfd = new SaveFileDialog
                    {
                        Title = "Xuất báo cáo Excel",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName = $"BaoCao_{DateTime.Now:yyyyMMdd_HHmm}"
                    })
                    {
                        if (sfd.ShowDialog() != DialogResult.OK)
                        {
                            FrmWaiting.ShowGifAlert(
                                "Huỷ quá trình xuất Excel",
                                "Export",
                                EnumStore.Icon.Warning);

                            return;
                        }

                        filePath = sfd.FileName;
                    }

                    DataTable exportTable = CreateExportTable(dt, kieu);

                    await WaitingHelper.RunWithWaiting(
                        () => ExcelExporter.ExportToPath(
                            exportTable,
                            filePath,
                            cbxXuatTCVN.Checked
                                ? ExcelExportTextFormat.TCVN
                                : ExcelExportTextFormat.Unicode),
                        "ĐANG XUẤT FILE EXCEL...");

                    FrmWaiting.ShowGifAlert(
                        "Đã xuất Excel thành công!",
                        "Export",
                        EnumStore.Icon.Success);

                    return;
                }

                // ── Bước 2: Bind DataSource lên grid ──
                await WaitingHelper.RunWithWaiting(
                    async () =>
                    {
                        // Yield 1 frame để WaitingHelper render xong
                        await Task.Delay(30);

                        if (cbxMoCuaSo.Checked)
                        {
                            MoCuaSoMoi(
                                dt,
                                applyFormatTimDL: true,
                                tieuDe: "BÁO CÁO",
                                kieu: kieu);
                        }
                        else
                        {
                            HienThiLenLuoi(
                                dt,
                                applyFormatTimDL: true,
                                kieu);
                        }
                    },
                    "ĐANG HIỂN THỊ DỮ LIỆU...");
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "BÁO CÁO"),
                    "LỖI TẢI DỮ LIỆU");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái CanEdit theo checkbox.
        /// </summary>
        private async void btnChinhTrangThai_Click(
            object sender,
            EventArgs e)
        {
            SetToolbarEnabled(false);

            try
            {
                List<BaoCao_Model.CanEdit> items =
                    GetCanEditItems(grvBaoCao, COL_CHECK);

                await WaitingHelper.RunWithWaiting(
                    () => BaoCao_DB.UpdateCanEdit(items),
                    "ĐANG CẬP NHẬT TRẠNG THÁI...");

                FrmWaiting.ShowGifAlert(
                    "Cập nhật thành công!",
                    "THÔNG BÁO",
                    EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(
                        ex,
                        "CẬP NHẬT TRẠNG THÁI"),
                    "LỖI");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LOAD & HIỂN THỊ DỮ LIỆU
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gán DataTable vào lưới rồi định dạng cột.
        /// applyFormatTimDL = false → ApplyFormatInOut
        /// applyFormatTimDL = true  → ApplyFormatTimDL
        /// </summary>
        private void HienThiLenLuoi(
            DataTable dt,
            bool applyFormatTimDL,
            int kieu = 0)
        {
            pnBaoCao.Controls.Clear();

            // ── Tạo filterPanel ──
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.WhiteSmoke
            };

            grvBaoCao.Dock = DockStyle.Fill;

            // Add Fill trước, Top sau
            pnBaoCao.Controls.Add(grvBaoCao);
            pnBaoCao.Controls.Add(_filterPanel);

            grvBaoCao.DataSource = null;
            grvBaoCao.Columns.Clear();

            if (dt == null || dt.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu");
                return;
            }

            _dataView = dt.DefaultView;

            grvBaoCao.AutoGenerateColumns = true;
            grvBaoCao.DataSource = _dataView;

            // Đồng bộ filterBox khi scroll hoặc thay đổi độ rộng cột
            grvBaoCao.Scroll -= GrvBaoCao_Scroll;
            grvBaoCao.Scroll += GrvBaoCao_Scroll;

            grvBaoCao.ColumnWidthChanged -=
                GrvBaoCao_ColumnWidthChanged;

            grvBaoCao.ColumnWidthChanged +=
                GrvBaoCao_ColumnWidthChanged;

            if (applyFormatTimDL)
            {
                ApplyFormatTimDL(grvBaoCao, kieu);
            }
            else
            {
                ApplyFormatInOut(grvBaoCao);
            }

            BuildFilterBoxes(dt);
        }

        private void GrvBaoCao_Scroll(
            object sender,
            ScrollEventArgs e)
        {
            AlignFilterBoxes(
                grvBaoCao,
                _filterPanel,
                _filterBoxes);
        }

        private void GrvBaoCao_ColumnWidthChanged(
            object sender,
            DataGridViewColumnEventArgs e)
        {
            AlignFilterBoxes(
                grvBaoCao,
                _filterPanel,
                _filterBoxes);
        }

        private void BuildFilterBoxes(DataTable table)
        {
            if (_filterPanel == null)
                return;

            _filterPanel.Controls.Clear();
            _filterBoxes = new TextBox[table.Columns.Count];

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var tb = new TextBox
                {
                    Tag = table.Columns[i].ColumnName,
                    Font = new Font("Segoe UI", 9F),
                    BorderStyle = BorderStyle.FixedSingle
                };

                tb.TextChanged += (s, e) => ApplyFilter();

                _filterPanel.Controls.Add(tb);
                _filterBoxes[i] = tb;
            }

            // Căn vị trí sau khi grid đã render
            grvBaoCao.ColumnAdded -= GrvBaoCao_ColumnAdded;
            grvBaoCao.ColumnAdded += GrvBaoCao_ColumnAdded;

            // Căn ngay sau khi form/control hiển thị
            if (ParentForm != null)
            {
                ParentForm.Shown -= ParentForm_Shown;
                ParentForm.Shown += ParentForm_Shown;
            }

            // Căn ngay lập tức nếu đã visible
            if (IsHandleCreated)
            {
                BeginInvoke(
                    new Action(
                        () => AlignFilterBoxes(
                            grvBaoCao,
                            _filterPanel,
                            _filterBoxes)));
            }
        }

        private void GrvBaoCao_ColumnAdded(
            object sender,
            DataGridViewColumnEventArgs e)
        {
            AlignFilterBoxes(
                grvBaoCao,
                _filterPanel,
                _filterBoxes);
        }

        private void ParentForm_Shown(
            object sender,
            EventArgs e)
        {
            AlignFilterBoxes(
                grvBaoCao,
                _filterPanel,
                _filterBoxes);
        }

        private static void AlignFilterBoxes(
            DataGridView dgr,
            Panel filterPanel,
            TextBox[] filterBoxes)
        {
            if (filterBoxes == null || filterPanel == null)
                return;

            for (int i = 0; i < filterBoxes.Length; i++)
            {
                if (filterBoxes[i] == null)
                    continue;

                string colName = filterBoxes[i].Tag?.ToString();

                if (colName == null ||
                    !dgr.Columns.Contains(colName))
                {
                    filterBoxes[i].Visible = false;
                    continue;
                }

                DataGridViewColumn col = dgr.Columns[colName];

                Rectangle rect =
                    dgr.GetColumnDisplayRectangle(
                        col.Index,
                        true);

                filterBoxes[i].SetBounds(
                    dgr.Left + rect.Left,
                    2,
                    rect.Width,
                    filterPanel.Height - 4);

                filterBoxes[i].Visible = rect.Width > 0;
            }
        }

        private void ApplyFilter()
        {
            if (_dataView == null || _filterBoxes == null)
                return;

            var conditions = new List<string>();

            foreach (TextBox tb in _filterBoxes)
            {
                if (tb == null ||
                    string.IsNullOrWhiteSpace(tb.Text))
                {
                    continue;
                }

                string colName = tb.Tag.ToString();
                string value = tb.Text.Replace("'", "''");

                conditions.Add(
                    $"CONVERT([{colName}], System.String) LIKE '%{value}%'");
            }

            _dataView.RowFilter =
                string.Join(" AND ", conditions);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MỞ CỬA SỔ MỚI
        // ═══════════════════════════════════════════════════════════════════════

        private void MoCuaSoMoi(
            DataTable dt,
            bool applyFormatTimDL,
            string tieuDe,
            int kieu = 0)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu");
                return;
            }

            var frm = new Form
            {
                Text = tieuDe,
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1200,
                Height = 700,
                MinimumSize = new Size(800, 400),
                Font = Font
            };

            var pnTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38
            };

            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = tieuDe,
                Font = new Font(
                    "Tahoma",
                    12.75f,
                    FontStyle.Bold),
                TextAlign = ContentAlignment.BottomCenter
            };

            pnTitle.Controls.Add(lblTitle);

            // ── Filter panel cho cửa sổ mới ──
            var localFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.WhiteSmoke
            };

            var dgr = new DataGridView
            {
                Dock = DockStyle.Fill
            };

            dgr.CellContentClick +=
                GrvBaoCao_CellContentClick;

            var pnGrid = new Panel
            {
                Dock = DockStyle.Fill
            };

            // Fill trước
            pnGrid.Controls.Add(dgr);

            // Top sau
            pnGrid.Controls.Add(localFilterPanel);

            frm.Controls.Add(pnGrid);
            frm.Controls.Add(pnTitle);

            var localDataView =
                dt.DefaultView.Table.Copy().DefaultView;

            dgr.AutoGenerateColumns = true;
            dgr.DataSource = localDataView;

            if (applyFormatTimDL)
            {
                ApplyFormatTimDL(dgr, kieu);
            }
            else
            {
                ApplyFormatInOut(dgr);
            }

            // Xây dựng filter boxes cho cửa sổ mới
            TextBox[] localFilterBoxes =
                new TextBox[dt.Columns.Count];

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var tb = new TextBox
                {
                    Tag = dt.Columns[i].ColumnName,
                    Font = new Font("Segoe UI", 9F),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var capturedDv = localDataView;
                var capturedBoxes = localFilterBoxes;

                tb.TextChanged +=
                    (s, e) =>
                    {
                        var conditions = new List<string>();

                        foreach (TextBox box in capturedBoxes)
                        {
                            if (box == null ||
                                string.IsNullOrWhiteSpace(box.Text))
                            {
                                continue;
                            }

                            string col = box.Tag.ToString();
                            string val =
                                box.Text.Replace("'", "''");

                            conditions.Add(
                                $"CONVERT([{col}], System.String) LIKE '%{val}%'");
                        }

                        capturedDv.RowFilter =
                            string.Join(" AND ", conditions);
                    };

                localFilterPanel.Controls.Add(tb);
                localFilterBoxes[i] = tb;
            }

            dgr.Scroll +=
                (s, e) => AlignFilterBoxes(
                    dgr,
                    localFilterPanel,
                    localFilterBoxes);

            dgr.ColumnWidthChanged +=
                (s, e) => AlignFilterBoxes(
                    dgr,
                    localFilterPanel,
                    localFilterBoxes);

            frm.Shown +=
                (s, e) => AlignFilterBoxes(
                    dgr,
                    localFilterPanel,
                    localFilterBoxes);

            frm.Show();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ĐỊNH DẠNG LƯỚI — btnIn_Out
        // ═══════════════════════════════════════════════════════════════════════

        private void ApplyFormatInOut(DataGridView dgr)
        {
            dgr.Tag = null;

            dgr.CellFormatting -= DgrBaoCao_CellFormatting;
            dgr.CellFormatting -= DgrXuatNhapTon_CellFormatting;
            dgr.CellFormatting += DgrXuatNhapTon_CellFormatting;

            dgr.AllowUserToAddRows = false;
            dgr.RowHeadersVisible = false;
            dgr.AutoGenerateColumns = true;
            dgr.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None;

            dgr.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgr.ColumnHeadersHeight = 30;
            dgr.RowTemplate.Height = 30;

            foreach (DataGridViewColumn col in dgr.Columns)
            {
                if (col.Name == COL_CHECK)
                {
                    col.ReadOnly = false;
                    continue;
                }

                if (col.Name == COL_UPDATE ||
                    col.Name == COL_DELETE)
                {
                    continue;
                }

                col.ReadOnly = true;

                col.AutoSizeMode =
                    DataGridViewAutoSizeColumnMode.None;

                col.DefaultCellStyle.WrapMode =
                    DataGridViewTriState.False;

                switch (col.Name)
                {
                    case "Tên Kho":
                        col.AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.AllCells;
                        break;

                    case "Tên Vật Tư":
                        col.AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.Fill;
                        break;

                    case "Mã Vật Tư":
                        col.Width = 120;
                        break;

                    case "Tồn Đầu Kỳ":
                    case "Tổng Nhập":
                    case "Tổng Xuất":
                    case "Tồn Cuối Kỳ":
                        col.Width = 120;
                        col.DefaultCellStyle.Format = "N2";

                        col.DefaultCellStyle.Alignment =
                            DataGridViewContentAlignment.MiddleRight;
                        break;

                    default:
                        col.Width = 90;
                        break;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ĐỊNH DẠNG LƯỚI — btnTimDL
        // ═══════════════════════════════════════════════════════════════════════

        internal static class NativeMethods
        {
            public const int WM_SETREDRAW = 0x000B;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern IntPtr SendMessage(
                IntPtr hWnd,
                int msg,
                bool wParam,
                int lParam);
        }

        private void ApplyFormatTimDL(
            DataGridView dgr,
            int kieu = 0)
        {
            const string COL_CAN_EDIT = "canEdit";

            // Lưu loại báo cáo trên grid để CellFormatting
            // biết khi nào cần hiển thị số lượng xuất dưới dạng số dương.
            dgr.Tag = kieu;

            dgr.CellFormatting -= DgrXuatNhapTon_CellFormatting;
            dgr.CellFormatting -= DgrBaoCao_CellFormatting;
            dgr.CellFormatting += DgrBaoCao_CellFormatting;

            NativeMethods.SendMessage(
                dgr.Handle,
                NativeMethods.WM_SETREDRAW,
                false,
                0);

            dgr.SuspendLayout();

            try
            {
                dgr.AllowUserToAddRows = false;
                dgr.RowHeadersVisible = false;
                dgr.AutoGenerateColumns = true;

                dgr.ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgr.ColumnHeadersHeight = 30;
                dgr.RowTemplate.Height = 30;

                // Cho phép người dùng resize cột
                dgr.AllowUserToResizeColumns = true;

                // Thêm cột checkbox nếu chưa có
                if (!dgr.Columns.Contains(COL_CHECK) &&
                    kieu > 1)
                {
                    dgr.Columns.Insert(
                        0,
                        new DataGridViewCheckBoxColumn
                        {
                            Name = COL_CHECK,
                            HeaderText = "Khóa",
                            Width = 50,
                            Frozen = true,
                            ReadOnly = false,
                            FalseValue = false,
                            TrueValue = true,
                            IndeterminateValue = false,
                            AutoSizeMode =
                                DataGridViewAutoSizeColumnMode.None,
                            Resizable =
                                DataGridViewTriState.True
                        });
                }

                // Format cột
                foreach (DataGridViewColumn col in dgr.Columns)
                {
                    if (col.Name == COL_CHECK)
                    {
                        col.ReadOnly = false;
                        col.Visible = true;

                        col.AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.None;

                        col.Resizable =
                            DataGridViewTriState.True;

                        continue;
                    }

                    if (col.Name == COL_UPDATE ||
                        col.Name == COL_DELETE)
                    {
                        continue;
                    }

                    if (col.Name.EndsWith(
                            "_ID",
                            StringComparison.OrdinalIgnoreCase) ||
                        col.Name == "LoaiDon" ||
                        col.Name.Equals(
                            COL_CAN_EDIT,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        col.Visible = false;
                    }
                    else
                    {
                        col.Visible = true;

                        col.AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.None;

                        col.Resizable =
                            DataGridViewTriState.True;

                        // Chỉ set width mặc định nếu quá nhỏ
                        if (col.Width < 80)
                            col.Width = 120;
                    }
                }

                bool hasCanEdit =
                    dgr.Columns.Contains(COL_CAN_EDIT);

                bool hasCheck =
                    dgr.Columns.Contains(COL_CHECK);

                // Set row height + giá trị checkbox
                foreach (DataGridViewRow row in dgr.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    row.Height = 30;

                    if (!hasCanEdit || !hasCheck)
                        continue;

                    int canEdit = 0;
                    object val =
                        row.Cells[COL_CAN_EDIT].Value;

                    if (val != null &&
                        val != DBNull.Value)
                    {
                        int.TryParse(
                            val.ToString(),
                            out canEdit);
                    }

                    // canEdit = 0 => checked
                    // canEdit = 1 => unchecked
                    row.Cells[COL_CHECK].Value =
                        canEdit == 0;
                }

                AddActionColumns(dgr);

                if (dgr.Columns.Contains(COL_UPDATE))
                {
                    dgr.Columns[COL_UPDATE].AutoSizeMode =
                        DataGridViewAutoSizeColumnMode.None;

                    dgr.Columns[COL_UPDATE].Resizable =
                        DataGridViewTriState.True;
                }

                if (dgr.Columns.Contains(COL_DELETE))
                {
                    dgr.Columns[COL_DELETE].AutoSizeMode =
                        DataGridViewAutoSizeColumnMode.None;

                    dgr.Columns[COL_DELETE].Resizable =
                        DataGridViewTriState.True;
                }
            }
            finally
            {
                dgr.ResumeLayout();

                NativeMethods.SendMessage(
                    dgr.Handle,
                    NativeMethods.WM_SETREDRAW,
                    true,
                    0);

                dgr.Refresh();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  XỬ LÝ CLICK NÚT SỬA / XÓA TRÊN LƯỚI
        // ═══════════════════════════════════════════════════════════════════════

        private void GrvBaoCao_CellContentClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            //if (e.RowIndex < 0) return;

            //var dgr = sender as DataGridView;
            //if (dgr == null) return;

            //string colName = dgr.Columns[e.ColumnIndex].Name;

            //bool isLocked =
            //    dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value != null &&
            //    Convert.ToBoolean(
            //        dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value);

            //if (isLocked &&
            //    (colName == COL_UPDATE ||
            //     colName == COL_DELETE))
            //{
            //    FrmWaiting.ShowGifAlert(
            //        "Dòng này đang bị khóa, không thể sửa/xóa!",
            //        "THÔNG BÁO",
            //        EnumStore.Icon.Warning);

            //    return;
            //}

            //if (colName == COL_UPDATE)
            //    _ = UpdateByCurrentKieuAsync(
            //        dgr,
            //        e.RowIndex);
            //else if (colName == COL_DELETE)
            //    _ = DeleteByCurrentKieuAsync(
            //        dgr,
            //        e.RowIndex);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  XÓA
        // ═══════════════════════════════════════════════════════════════════════

        private async Task DeleteByCurrentKieuAsync(
            DataGridView dgr,
            int rowIndex)
        {
            try
            {
                if (rowIndex < 0 ||
                    rowIndex >= dgr.Rows.Count)
                {
                    return;
                }

                DataGridViewRow row =
                    dgr.Rows[rowIndex];

                int kieu =
                    cbxKieu.SelectedIndex;

                string ten =
                    dgr.Columns.Contains("TenVatTu")
                        ? row.Cells["TenVatTu"].Value?.ToString() ?? ""
                        : "";

                if (kieu == 1)
                {
                    if (!dgr.Columns.Contains("dh_id"))
                    {
                        throw new Exception(
                            "Không tìm thấy cột dh_id.");
                    }

                    int id =
                        Convert.ToInt32(
                            row.Cells["dh_id"].Value);

                    string tenHien =
                        string.IsNullOrWhiteSpace(ten)
                            ? $"ID {id}"
                            : ten;

                    DialogResult confirm =
                        MessageBox.Show(
                            $"Thao tác này sẽ xóa toàn bộ lịch sử xuất/nhập của đơn {tenHien}",
                            "Xác nhận xóa",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2);

                    if (confirm != DialogResult.Yes)
                        return;

                    SetToolbarEnabled(false);

                    await WaitingHelper.RunWithWaiting(
                        () => BaoCao_DB.DeleteDanhSachDatHang(id),
                        "ĐANG XÓA...");
                }
                else if (kieu == 2 || kieu == 3)
                {
                    if (!dgr.Columns.Contains("lsxn_id"))
                    {
                        throw new Exception(
                            "Không tìm thấy cột lsxn_id.");
                    }

                    int id =
                        Convert.ToInt32(
                            row.Cells["lsxn_id"].Value);

                    string tenHien =
                        string.IsNullOrWhiteSpace(ten)
                            ? $"ID {id}"
                            : ten;

                    DialogResult confirm =
                        MessageBox.Show(
                            $"Đơn {tenHien} sẽ bị xóa lịch sử xuất/nhập?",
                            "Xác nhận xóa",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2);

                    if (confirm != DialogResult.Yes)
                        return;

                    SetToolbarEnabled(false);

                    await WaitingHelper.RunWithWaiting(
                        () => BaoCao_DB.DeleteLichSuXuatNhap(id),
                        "ĐANG XÓA...");
                }
                else
                {
                    FrmWaiting.ShowGifAlert(
                        "Kiểu báo cáo hiện tại không hỗ trợ xóa.",
                        "THÔNG BÁO",
                        EnumStore.Icon.Warning);

                    return;
                }

                dgr.Rows.RemoveAt(rowIndex);

                FrmWaiting.ShowGifAlert(
                    "Đã xóa thành công!",
                    "THÀNH CÔNG",
                    EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "XÓA"),
                    "LỖI XÓA");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SỬA
        // ═══════════════════════════════════════════════════════════════════════

        private async Task UpdateByCurrentKieuAsync(
            DataGridView dgr,
            int rowIndex)
        {
            try
            {
                if (rowIndex < 0 ||
                    rowIndex >= dgr.Rows.Count)
                {
                    return;
                }

                DataGridViewRow row =
                    dgr.Rows[rowIndex];

                int kieu =
                    cbxKieu.SelectedIndex;

                if (kieu == 1)
                {
                    BaoCao_Model.ThongTinDatHangUpdate model =
                        GetThongTinDatHangUpdateModel(row);

                    SetToolbarEnabled(false);

                    await WaitingHelper.RunWithWaiting(
                        () => BaoCao_DB.UpdateThongTinDatHang(model),
                        "ĐANG CẬP NHẬT...");

                    FrmWaiting.ShowGifAlert(
                        "Đã cập nhật ThongTinDatHang thành công!",
                        "THÀNH CÔNG",
                        EnumStore.Icon.Success);
                }
                else if (kieu == 2 || kieu == 3)
                {
                    BaoCao_Model.LichSuXuatNhapUpdate model =
                        GetLichSuXuatNhapUpdateModel(row);

                    SetToolbarEnabled(false);

                    await WaitingHelper.RunWithWaiting(
                        () => BaoCao_DB.UpdateLichSuXuatNhap(model),
                        "ĐANG CẬP NHẬT...");

                    FrmWaiting.ShowGifAlert(
                        "Đã cập nhật LichSuXuatNhap thành công!",
                        "THÀNH CÔNG",
                        EnumStore.Icon.Success);
                }
                else
                {
                    FrmWaiting.ShowGifAlert(
                        "Kiểu báo cáo hiện tại không hỗ trợ cập nhật.",
                        "THÔNG BÁO",
                        EnumStore.Icon.Warning);
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(
                        ex,
                        "CẬP NHẬT"),
                    "LỖI CẬP NHẬT");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TOOLBAR
        // ═══════════════════════════════════════════════════════════════════════

        private void SetToolbarEnabled(bool enabled)
        {
            btnIn_Out.Enabled = enabled;
            btnTimDL.Enabled = enabled;
            btnChinhTrangThai.Enabled = enabled;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  THÊM CỘT SỬA / XÓA
        // ═══════════════════════════════════════════════════════════════════════

        private void AddActionColumns(DataGridView dgr)
        {
            if (!dgr.Columns.Contains(COL_UPDATE))
            {
                //dgr.Columns.Add(
                //    new DataGridViewButtonColumn
                //    {
                //        Name = COL_UPDATE,
                //        HeaderText = "",
                //        Text = "Sửa",
                //        UseColumnTextForButtonValue = true,
                //        Width = 75
                //    });
            }

            if (!dgr.Columns.Contains(COL_DELETE))
            {
                //dgr.Columns.Add(
                //    new DataGridViewButtonColumn
                //    {
                //        Name = COL_DELETE,
                //        HeaderText = "",
                //        Text = "Xóa",
                //        UseColumnTextForButtonValue = true,
                //        Width = 75
                //    });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ĐỌC MODEL TỪ ROW
        // ═══════════════════════════════════════════════════════════════════════

        private BaoCao_Model.LichSuXuatNhapUpdate
            GetLichSuXuatNhapUpdateModel(
                DataGridViewRow row)
        {
            decimal soLuong = 0;

            if (row.DataGridView.Columns.Contains("SoLuong") &&
                row.Cells["SoLuong"].Value != DBNull.Value)
            {
                soLuong =
                    Convert.ToDecimal(
                        row.Cells["SoLuong"].Value);
            }

            int? danhSachKhoId = null;

            if (row.DataGridView.Columns.Contains("DanhSachKho_ID") &&
                row.Cells["DanhSachKho_ID"].Value != null &&
                row.Cells["DanhSachKho_ID"].Value != DBNull.Value)
            {
                danhSachKhoId =
                    Convert.ToInt32(
                        row.Cells["DanhSachKho_ID"].Value);
            }

            return new BaoCao_Model.LichSuXuatNhapUpdate
            {
                Id = Convert.ToInt32(
                    row.Cells["lsxn_id"].Value),

                SoLuong = soLuong,

                NguoiGiaoNhan =
                    row.DataGridView.Columns.Contains("NguoiGiaoNhan")
                        ? row.Cells["NguoiGiaoNhan"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                DanhSachKhoId = danhSachKhoId,

                LyDo =
                    row.DataGridView.Columns.Contains("LyDo")
                        ? row.Cells["LyDo"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                Ngay =
                    row.DataGridView.Columns.Contains("NgayXuatNhap")
                        ? row.Cells["NgayXuatNhap"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                TenPhieu =
                    row.DataGridView.Columns.Contains("TenPhieu")
                        ? row.Cells["TenPhieu"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                GhiChu =
                    row.DataGridView.Columns.Contains("GhiChu")
                        ? row.Cells["GhiChu"]
                            .Value?.ToString()?.Trim() ?? ""
                        : ""
            };
        }

        private BaoCao_Model.ThongTinDatHangUpdate
            GetThongTinDatHangUpdateModel(
                DataGridViewRow row)
        {
            decimal soLuongMua = 0;

            if (row.DataGridView.Columns.Contains("SoLuongMua") &&
                row.Cells["SoLuongMua"].Value != DBNull.Value)
            {
                soLuongMua =
                    Convert.ToDecimal(
                        row.Cells["SoLuongMua"].Value);
            }
            else if (
                row.DataGridView.Columns.Contains("SL_YeuCau") &&
                row.Cells["SL_YeuCau"].Value != DBNull.Value)
            {
                soLuongMua =
                    Convert.ToDecimal(
                        row.Cells["SL_YeuCau"].Value);
            }

            decimal donGia = 0;

            if (row.DataGridView.Columns.Contains("tt_DonGia") &&
                row.Cells["tt_DonGia"].Value != DBNull.Value)
            {
                donGia =
                    Convert.ToDecimal(
                        row.Cells["tt_DonGia"].Value);
            }
            else if (
                row.DataGridView.Columns.Contains("DonGia") &&
                row.Cells["DonGia"].Value != DBNull.Value)
            {
                donGia =
                    Convert.ToDecimal(
                        row.Cells["DonGia"].Value);
            }

            return new BaoCao_Model.ThongTinDatHangUpdate
            {
                Id = Convert.ToInt32(
                    row.Cells["ThongTinDatHang_ID"].Value),

                TenVatTu =
                    row.DataGridView.Columns.Contains("TenVatTu")
                        ? row.Cells["TenVatTu"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                SoLuongMua = soLuongMua,
                DonGia = donGia,

                MucDichMua =
                    row.DataGridView.Columns.Contains("MucDichMua")
                        ? row.Cells["MucDichMua"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                NgayGiao =
                    row.DataGridView.Columns.Contains("NgayGiao")
                        ? row.Cells["NgayGiao"]
                            .Value?.ToString()?.Trim() ?? ""
                        : "",

                GhiChu =
                    row.DataGridView.Columns.Contains("GhiChu")
                        ? row.Cells["GhiChu"]
                            .Value?.ToString()?.Trim() ?? ""
                        : ""
            };
        }

        private static List<BaoCao_Model.CanEdit>
            GetCanEditItems(
                DataGridView dgr,
                string checkColumnName)
        {
            var items =
                new List<BaoCao_Model.CanEdit>();

            if (dgr == null ||
                !dgr.Columns.Contains(checkColumnName) ||
                !dgr.Columns.Contains("lsxn_id"))
            {
                return items;
            }

            foreach (DataGridViewRow row in dgr.Rows)
            {
                if (row.IsNewRow)
                    continue;

                object idValue =
                    row.Cells["lsxn_id"].Value;

                if (idValue == null ||
                    idValue == DBNull.Value)
                {
                    continue;
                }

                bool isChecked =
                    row.Cells[checkColumnName].Value != null &&
                    Convert.ToBoolean(
                        row.Cells[checkColumnName].Value);

                items.Add(
                    new BaoCao_Model.CanEdit
                    {
                        Id = Convert.ToInt32(idValue),
                        Value = isChecked ? 0 : 1
                    });
            }

            return items;
        }

        private static DataTable CreateExportTable(
            DataTable source,
            int kieu)
        {
            if (source == null)
                return null;

            DataTable result = source.Copy();

            string[] internalColumns =
            {
                "ThongTinDatHang_ID",
                "DanhSachKho_ID"
            };

            foreach (string columnName in internalColumns)
            {
                if (result.Columns.Contains(columnName))
                {
                    result.Columns.Remove(columnName);
                }
            }

            // kieu = 3 là Xuất Hàng.
            // Chỉ đổi trên bản sao dùng để xuất Excel.
            if (kieu == 3 &&
                result.Columns.Contains("SoLuong"))
            {
                Type quantityType =
                    result.Columns["SoLuong"].DataType;

                foreach (DataRow row in result.Rows)
                {
                    if (row["SoLuong"] == null ||
                        row["SoLuong"] == DBNull.Value)
                    {
                        continue;
                    }

                    row["SoLuong"] =
                        GetAbsoluteQuantity(
                            row["SoLuong"],
                            quantityType);
                }
            }

            return result;
        }

        private static object GetAbsoluteQuantity(
            object value,
            Type targetType)
        {
            if (targetType == typeof(decimal))
                return Math.Abs(Convert.ToDecimal(value));

            if (targetType == typeof(double))
                return Math.Abs(Convert.ToDouble(value));

            if (targetType == typeof(float))
                return Math.Abs(Convert.ToSingle(value));

            if (targetType == typeof(long))
                return Math.Abs(Convert.ToInt64(value));

            if (targetType == typeof(int))
                return Math.Abs(Convert.ToInt32(value));

            if (targetType == typeof(short))
                return Math.Abs(Convert.ToInt16(value));

            return Math.Abs(
                    Convert.ToDecimal(value))
                .ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SỰ KIỆN COMBOBOX / CHECKBOX
        // ═══════════════════════════════════════════════════════════════════════

        private void cbxThoiGian_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            bool custom =
                cbxThoiGian.SelectedIndex == 1;

            dtBatDau.Enabled = custom;
            dtKetThuc.Enabled = custom;
        }

        private void cbxLoaiYC_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            bool daNhap =
                cbxLoaiYC.SelectedIndex == 1;

            if (daNhap)
            {
                cbxdsKho.SelectedIndex = 0;
                cbxdsKho.Enabled = false;
            }
            else
            {
                cbxdsKho.Enabled = true;
            }
        }

        private void cbxAll_CheckedChanged(
            object sender,
            EventArgs e)
        {
            foreach (DataGridViewRow row in grvBaoCao.Rows)
            {
                row.Cells[COL_CHECK].Value =
                    cbxAll.Checked;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CELL FORMATTING
        // ═══════════════════════════════════════════════════════════════════════

        private void DgrBaoCao_CellFormatting(
            object sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (!(sender is DataGridView dgr))
                return;

            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0)
            {
                return;
            }

            // kieu = 3 là Xuất Hàng
            if (!(dgr.Tag is int kieu) ||
                kieu != 3)
            {
                return;
            }

            if (!string.Equals(
                    dgr.Columns[e.ColumnIndex].Name,
                    "SoLuong",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (e.Value == null ||
                e.Value == DBNull.Value)
            {
                return;
            }

            try
            {
                decimal soLuong =
                    Convert.ToDecimal(e.Value);

                if (soLuong >= 0)
                    return;

                // DataGridViewTextBoxCell yêu cầu formatted value
                // là chuỗi khi FormattingApplied = true.
                e.Value =
                    Math.Abs(soLuong).ToString();

                e.FormattingApplied = true;
            }
            catch (FormatException)
            {
                // Giữ nguyên nếu không phải số
            }
            catch (InvalidCastException)
            {
                // Giữ nguyên nếu không chuyển được sang số
            }
            catch (OverflowException)
            {
                // Giữ nguyên nếu vượt phạm vi decimal
            }
        }

        private void DgrXuatNhapTon_CellFormatting(
            object sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (!(sender is DataGridView dgr))
                return;

            if (e.ColumnIndex < 0)
                return;

            if (dgr.Columns[e.ColumnIndex].Name != "Edit")
                return;

            if (e.Value == null)
                return;

            if (int.TryParse(
                    e.Value.ToString(),
                    out int val))
            {
                e.Value =
                    val == 0
                        ? "Có thể"
                        : "Không thể";

                e.FormattingApplied = true;
            }
        }

        private void cbxExportExcel_CheckedChanged(
            object sender,
            EventArgs e)
        {
            cbxXuatTCVN.Enabled =
                cbxExportExcel.Checked;
        }
    }
}