using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
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

            if (cbxdsKho.SelectedValue == null || cbxdsKho.SelectedValue == DBNull.Value)
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
            string sql = "SELECT username FROM users";

            DataTable dt = DatabaseHelper.GetData(sql);

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

                DateTime? ngayBatDau = cbxThoiGian.SelectedIndex == 0 ? null : (DateTime?)dtBatDau.Value;
                DateTime? ngayKetThuc = cbxThoiGian.SelectedIndex == 0 ? null : (DateTime?)dtKetThuc.Value;

                await WaitingHelper.RunWithWaiting(async () =>
                {

                    dt = await Task.Run(() => DatabaseHelper.TinhTonKho(ngayBatDau, ngayKetThuc, khoId));
                }, "ĐANG TẢI LỊCH SỬ XUẤT NHẬP...");

                if (cbxExportExcel.Checked)
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không có dữ liệu để xuất.", "Export", EnumStore.Icon.Warning);
                        return;
                    }
                    string filePath = null;
                    using (var sfd = new SaveFileDialog
                    {
                        Title = "Xuất báo cáo Excel",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName = $"BaoCao_XuatNhapTon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                    })
                    {
                        if (sfd.ShowDialog() != DialogResult.OK)
                        {
                            FrmWaiting.ShowGifAlert("Huỷ quá trình xuất Excel", "Export", EnumStore.Icon.Warning);
                            return;
                        }
                        filePath = sfd.FileName;
                    }
                    await WaitingHelper.RunWithWaiting(
                        () => ExcelExporter.ExportToPath(dt, filePath),
                        "ĐANG XUẤT FILE EXCEL...");
                    FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
                    return;
                }

                // ── Bind + format (có waiting) ──
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    await Task.Delay(30); // nhường frame cho waiting render
                    if (cbxMoCuaSo.Checked)
                        MoCuaSoMoi(dt, applyFormatTimDL: false, tieuDe: "BÁO CÁO XUẤT - NHẬP - TỒN");
                    else
                        HienThiLenLuoi(dt, applyFormatTimDL: false);
                }, "ĐANG HIỂN THỊ DỮ LIỆU...");
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "BÁO CÁO XUẤT - NHẬP - TỒN"),
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
            string nguoiThucHien = cbxNguoiThucHien.SelectedIndex == 0 ? "" : cbxNguoiThucHien.SelectedItem.ToString();
            int tinhTrang = cbxLoaiYC.SelectedIndex;
            int kieu = cbxKieu.SelectedIndex;
            bool exportExcel = cbxExportExcel.Checked;

            SetToolbarEnabled(false);
            DataTable dt = null;

            DateTime? ngayBatDau = cbxThoiGian.SelectedIndex == 0 ? null : (DateTime?)dtBatDau.Value;
            DateTime? ngayKetThuc = cbxThoiGian.SelectedIndex == 0 ? null : (DateTime?)dtKetThuc.Value;

            try
            {
                // ── Bước 1: Query DB + chuẩn bị dữ liệu (có waiting) ──
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    dt = await Task.Run(() =>
                    {
                        switch (kieu)
                        {
                            case 1: return DatabaseHelper.GetBaoCaoDatHang_v2(nguoiThucHien, ngayBatDau, ngayKetThuc);
                            case 2: return DatabaseHelper.GetBaoCaoLichSuXuatNhap_v2(kho, nguoiThucHien, true, ngayBatDau, ngayKetThuc);
                            case 3: return DatabaseHelper.GetBaoCaoLichSuXuatNhap_v2(kho, nguoiThucHien, false, ngayBatDau, ngayKetThuc);
                            default: return DatabaseHelper.GetBaoCaoDatHang_v2(nguoiThucHien, ngayBatDau, ngayKetThuc);
                        }
                    });
                }, "ĐANG TẢI DỮ LIỆU BÁO CÁO...");

                // ── Export Excel ──
                if (exportExcel)
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không có dữ liệu để xuất.", "Export", EnumStore.Icon.Warning);
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
                            FrmWaiting.ShowGifAlert("Huỷ quá trình xuất Excel", "Export", EnumStore.Icon.Warning);
                            return;
                        }
                        filePath = sfd.FileName;
                    }
                    await WaitingHelper.RunWithWaiting(
                        () => ExcelExporter.ExportToPath(dt, filePath),
                        "ĐANG XUẤT FILE EXCEL...");
                    FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
                    return;
                }

                // ── Bước 2: Bind DataSource lên grid (có waiting, vẫn UI thread) ──
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    // Yield 1 frame để WaitingHelper render xong trước khi UI thread bận
                    await Task.Delay(30);

                    if (cbxMoCuaSo.Checked)
                        MoCuaSoMoi(dt, applyFormatTimDL: true, tieuDe: "BÁO CÁO", kieu: kieu);
                    else
                        HienThiLenLuoi(dt, applyFormatTimDL: true, kieu);

                }, "ĐANG HIỂN THỊ DỮ LIỆU...");
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
        /// Thao tác DB nhẹ → vẫn bọc waiting cho nhất quán UX.
        /// </summary>
        private async void btnChinhTrangThai_Click(object sender, EventArgs e)
        {
            SetToolbarEnabled(false);

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    // UpdateCanEdit đọc DataGridView (UI object) → PHẢI chạy trên UI thread.
                    // Dùng Invoke để đảm bảo khi WaitingHelper wrap trong Task.Run.
                    await Task.Run(() => { });   // yield để waiting form có thể render

                    // Gọi thẳng trên UI thread sau khi yield
                    DatabaseHelper.UpdateCanEdit(grvBaoCao, COL_CHECK);
                }, "ĐANG CẬP NHẬT TRẠNG THÁI...");

                FrmWaiting.ShowGifAlert("Cập nhật thành công!", "THÔNG BÁO", EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "CẬP NHẬT TRẠNG THÁI"),
                    "LỖI");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LOAD & HIỂN THỊ DỮ LIỆU
        //  ⚠ Hàm này chỉ được gọi từ UI thread
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gán DataTable vào lưới rồi định dạng cột.
        /// applyFormatTimDL = false  →  ApplyFormatInOut  (dùng cho btnIn_Out)
        /// applyFormatTimDL = true   →  ApplyFormatTimDL  (dùng cho btnTimDL)
        /// ⚠ Phải gọi trên UI thread.
        /// </summary>
        private void HienThiLenLuoi(DataTable dt, bool applyFormatTimDL, int kieu = 0)
        {
            pnBaoCao.Controls.Clear();

            // ── Tạo filterPanel (giống UC_MonthyReport) ──
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.WhiteSmoke
            };

            grvBaoCao.Dock = DockStyle.Fill;

            // Quan trọng: add Fill trước, Top sau để winform xếp đúng thứ tự
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
            grvBaoCao.ColumnWidthChanged -= GrvBaoCao_ColumnWidthChanged;
            grvBaoCao.ColumnWidthChanged += GrvBaoCao_ColumnWidthChanged;

            if (applyFormatTimDL)
                ApplyFormatTimDL(grvBaoCao, kieu);
            else
                ApplyFormatInOut(grvBaoCao);

            BuildFilterBoxes(dt);
        }

        private void GrvBaoCao_Scroll(object sender, ScrollEventArgs e) => AlignFilterBoxes(grvBaoCao, _filterPanel, _filterBoxes);
        private void GrvBaoCao_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e) => AlignFilterBoxes(grvBaoCao, _filterPanel, _filterBoxes);

        private void BuildFilterBoxes(DataTable table)
        {
            if (_filterPanel == null) return;
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
            if (this.ParentForm != null)
            {
                this.ParentForm.Shown -= ParentForm_Shown;
                this.ParentForm.Shown += ParentForm_Shown;
            }

            // Căn ngay lập tức nếu đã visible
            if (this.IsHandleCreated)
                this.BeginInvoke(new Action(() => AlignFilterBoxes(grvBaoCao, _filterPanel, _filterBoxes)));
        }

        private void GrvBaoCao_ColumnAdded(object sender, DataGridViewColumnEventArgs e) => AlignFilterBoxes(grvBaoCao, _filterPanel, _filterBoxes);
        private void ParentForm_Shown(object sender, EventArgs e) => AlignFilterBoxes(grvBaoCao, _filterPanel, _filterBoxes);

        private static void AlignFilterBoxes(DataGridView dgr, Panel filterPanel, TextBox[] filterBoxes)
        {
            if (filterBoxes == null || filterPanel == null) return;

            // Lấy danh sách CÁC CỘT DỮ LIỆU thực sự (bỏ qua checkbox/action thêm bằng code)
            // Các cột này có Tag là tên cột DataTable, được lưu trong filterBoxes[i].Tag
            // Duyệt theo filterBoxes (DataTable columns), tìm cột tương ứng trên grid theo Name
            for (int i = 0; i < filterBoxes.Length; i++)
            {
                if (filterBoxes[i] == null) continue;

                string colName = filterBoxes[i].Tag?.ToString();
                if (colName == null || !dgr.Columns.Contains(colName))
                {
                    filterBoxes[i].Visible = false;
                    continue;
                }

                var col = dgr.Columns[colName];
                var rect = dgr.GetColumnDisplayRectangle(col.Index, true);

                filterBoxes[i].SetBounds(
                    dgr.Left + rect.Left,
                    2,
                    rect.Width,
                    filterPanel.Height - 4
                );
                filterBoxes[i].Visible = rect.Width > 0;
            }
        }

        private void ApplyFilter()
        {
            if (_dataView == null || _filterBoxes == null) return;

            var conditions = new System.Collections.Generic.List<string>();

            foreach (var tb in _filterBoxes)
            {
                if (string.IsNullOrWhiteSpace(tb.Text)) continue;

                string colName = tb.Tag.ToString();
                string value = tb.Text.Replace("'", "''");
                conditions.Add($"CONVERT([{colName}], System.String) LIKE '%{value}%'");
            }

            _dataView.RowFilter = string.Join(" AND ", conditions);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MỞ CỬA SỔ MỚI (khi cbxMoCuaSo.Checked = true)
        //  ⚠ Phải gọi trên UI thread
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo Form mới chứa DataGridView, hiển thị dữ liệu trong cửa sổ riêng biệt.
        /// pnBaoCao không bị thay đổi.
        /// </summary>
        private void MoCuaSoMoi(DataTable dt, bool applyFormatTimDL, string tieuDe, int kieu = 0)
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
                Font = this.Font
            };

            var pnTitle = new Panel { Dock = DockStyle.Top, Height = 38 };
            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = tieuDe,
                Font = new Font("Tahoma", 12.75f, FontStyle.Bold),
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

            var dgr = new DataGridView { Dock = DockStyle.Fill };
            dgr.CellContentClick += GrvBaoCao_CellContentClick;

            var pnGrid = new Panel { Dock = DockStyle.Fill };
            pnGrid.Controls.Add(dgr);           // Fill trước
            pnGrid.Controls.Add(localFilterPanel); // Top sau

            frm.Controls.Add(pnGrid);
            frm.Controls.Add(pnTitle);

            var localDataView = dt.DefaultView.Table.Copy().DefaultView;
            dgr.AutoGenerateColumns = true;
            dgr.DataSource = localDataView;

            if (applyFormatTimDL)
                ApplyFormatTimDL(dgr, kieu);
            else
                ApplyFormatInOut(dgr);

            // Xây dựng filter boxes cho cửa sổ mới
            TextBox[] localFilterBoxes = new TextBox[dt.Columns.Count];
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var tb = new TextBox
                {
                    Tag = dt.Columns[i].ColumnName,
                    Font = new Font("Segoe UI", 9F),
                    BorderStyle = BorderStyle.FixedSingle
                };
                // Capture cho closure
                var capturedDv = localDataView;
                var capturedBoxes = localFilterBoxes;
                tb.TextChanged += (s, e) =>
                {
                    var conditions = new System.Collections.Generic.List<string>();
                    foreach (var box in capturedBoxes)
                    {
                        if (box == null || string.IsNullOrWhiteSpace(box.Text)) continue;
                        string col = box.Tag.ToString();
                        string val = box.Text.Replace("'", "''");
                        conditions.Add($"CONVERT([{col}], System.String) LIKE '%{val}%'");
                    }
                    capturedDv.RowFilter = string.Join(" AND ", conditions);
                };
                localFilterPanel.Controls.Add(tb);
                localFilterBoxes[i] = tb;
            }

            dgr.Scroll += (s, e) => AlignFilterBoxes(dgr, localFilterPanel, localFilterBoxes);
            dgr.ColumnWidthChanged += (s, e) => AlignFilterBoxes(dgr, localFilterPanel, localFilterBoxes);
            frm.Shown += (s, e) => AlignFilterBoxes(dgr, localFilterPanel, localFilterBoxes);

            frm.Show();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ĐỊNH DẠNG LƯỚI — btnIn_Out
        // ═══════════════════════════════════════════════════════════════════════

        private void ApplyFormatInOut(DataGridView dgr)
        {
            dgr.CellFormatting -= DgrXuatNhapTon_CellFormatting;
            dgr.CellFormatting += DgrXuatNhapTon_CellFormatting;

            dgr.AllowUserToAddRows = false;
            dgr.RowHeadersVisible = false;
            dgr.AutoGenerateColumns = true;
            dgr.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgr.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgr.ColumnHeadersHeight = 30;
            dgr.RowTemplate.Height = 30;

            foreach (DataGridViewColumn col in dgr.Columns)
            {
                if (col.Name == COL_CHECK) { col.ReadOnly = false; continue; }
                if (col.Name == COL_UPDATE || col.Name == COL_DELETE) continue;

                col.ReadOnly = true;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

                switch (col.Name)
                {
                    case "Tên Kho":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                        break;

                    case "Tên Vật Tư":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
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
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        break;

                    default: col.Width = 90; break;
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
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);
        }

        private void ApplyFormatTimDL(DataGridView dgr, int kieu = 0)
        {
            const string COL_CAN_EDIT = "canEdit";

            NativeMethods.SendMessage(dgr.Handle, NativeMethods.WM_SETREDRAW, false, 0);
            dgr.SuspendLayout();

            try
            {
                dgr.AllowUserToAddRows = false;
                dgr.RowHeadersVisible = false;
                dgr.AutoGenerateColumns = true;

                dgr.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgr.ColumnHeadersHeight = 30;
                dgr.RowTemplate.Height = 30;

                // Cho phép người dùng resize cột
                dgr.AllowUserToResizeColumns = true;

                // Thêm cột checkbox nếu chưa có
                if (!dgr.Columns.Contains(COL_CHECK) && kieu > 1)
                {
                    dgr.Columns.Insert(0, new DataGridViewCheckBoxColumn
                    {
                        Name = COL_CHECK,
                        HeaderText = "Khóa",
                        Width = 50,
                        Frozen = true,
                        ReadOnly = false,
                        FalseValue = false,
                        TrueValue = true,
                        IndeterminateValue = false,
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                        Resizable = DataGridViewTriState.True
                    });
                }

                // Format cột
                foreach (DataGridViewColumn col in dgr.Columns)
                {
                    if (col.Name == COL_CHECK)
                    {
                        col.ReadOnly = false;
                        col.Visible = true;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Resizable = DataGridViewTriState.True;
                        continue;
                    }

                    if (col.Name == COL_UPDATE || col.Name == COL_DELETE)
                        continue;

                    if (col.Name.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) ||
                        col.Name == "LoaiDon" ||
                        col.Name.Equals(COL_CAN_EDIT, StringComparison.OrdinalIgnoreCase))
                    {
                        col.Visible = false;
                    }
                    else
                    {
                        col.Visible = true;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Resizable = DataGridViewTriState.True;

                        // Chỉ set width mặc định nếu width đang quá nhỏ
                        if (col.Width < 80)
                            col.Width = 120;
                    }
                }

                bool hasCanEdit = dgr.Columns.Contains(COL_CAN_EDIT);
                bool hasCheck = dgr.Columns.Contains(COL_CHECK);

                // Set row height + giá trị checkbox
                foreach (DataGridViewRow row in dgr.Rows)
                {
                    if (row.IsNewRow) continue;

                    row.Height = 30;

                    if (!hasCanEdit || !hasCheck) continue;

                    int canEdit = 0;
                    var val = row.Cells[COL_CAN_EDIT].Value;

                    if (val != null && val != DBNull.Value)
                        int.TryParse(val.ToString(), out canEdit);

                    // canEdit = 0 => checked
                    // canEdit = 1 => unchecked
                    row.Cells[COL_CHECK].Value = (canEdit == 0);
                }

                AddActionColumns(dgr);

                // Đảm bảo các cột action không bị autosize ngoài ý muốn
                if (dgr.Columns.Contains(COL_UPDATE))
                {
                    dgr.Columns[COL_UPDATE].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    dgr.Columns[COL_UPDATE].Resizable = DataGridViewTriState.True;
                }

                if (dgr.Columns.Contains(COL_DELETE))
                {
                    dgr.Columns[COL_DELETE].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    dgr.Columns[COL_DELETE].Resizable = DataGridViewTriState.True;
                }
            }
            finally
            {
                dgr.ResumeLayout();
                NativeMethods.SendMessage(dgr.Handle, NativeMethods.WM_SETREDRAW, true, 0);
                dgr.Refresh();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  XỬ LÝ CLICK NÚT SỬA / XÓA TRÊN LƯỚI
        // ═══════════════════════════════════════════════════════════════════════

        private void GrvBaoCao_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //if (e.RowIndex < 0) return;

            //var dgr = sender as DataGridView;
            //if (dgr == null) return;

            //string colName = dgr.Columns[e.ColumnIndex].Name;

            //bool isLocked = dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value != null &&
            //                Convert.ToBoolean(dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value);

            //if (isLocked && (colName == COL_UPDATE || colName == COL_DELETE))
            //{
            //    FrmWaiting.ShowGifAlert(
            //        "Dòng này đang bị khóa, không thể sửa/xóa!",
            //        "THÔNG BÁO", EnumStore.Icon.Warning);
            //    return;
            //}

            //if (colName == COL_UPDATE)
            //    _ = UpdateByCurrentKieuAsync(dgr, e.RowIndex);
            //else if (colName == COL_DELETE)
            //    _ = DeleteByCurrentKieuAsync(dgr, e.RowIndex);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  XÓA (async) — bọc waiting cho thao tác DB
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Luồng:
        ///   UI thread  → đọc row/kieu, hỏi confirm
        ///   BG thread  → gọi DB Delete
        ///   UI thread  → xóa dòng khỏi lưới, thông báo
        /// </summary>
        private async Task DeleteByCurrentKieuAsync(DataGridView dgr, int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dgr.Rows.Count) return;

                var row = dgr.Rows[rowIndex];
                int kieu = cbxKieu.SelectedIndex;

                string ten = dgr.Columns.Contains("TenVatTu")
                    ? row.Cells["TenVatTu"].Value?.ToString() ?? ""
                    : "";

                // ── Xác nhận TRÊN UI THREAD (trước await) ──
                if (kieu == 1)
                {
                    if (!dgr.Columns.Contains("DanhSachDatHang_ID"))
                        throw new Exception("Không tìm thấy cột DanhSachDatHang_ID.");

                    int id = Convert.ToInt32(row.Cells["DanhSachDatHang_ID"].Value);
                    string tenHien = string.IsNullOrWhiteSpace(ten) ? $"ID {id}" : ten;

                    var confirm = MessageBox.Show(
                        $"Thao tác này sẽ xóa toàn bộ lịch sử xuất/nhập của đơn {tenHien}",
                        "Xác nhận xóa",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirm != DialogResult.Yes) return;

                    SetToolbarEnabled(false);
                    await WaitingHelper.RunWithWaiting(
                        () => DatabaseHelper.DeleteDanhSachDatHang(id),
                        "ĐANG XÓA...");
                }
                else if (kieu == 2 || kieu == 3)
                {
                    if (!dgr.Columns.Contains("LichSu_ID"))
                        throw new Exception("Không tìm thấy cột LichSu_ID.");

                    int id = Convert.ToInt32(row.Cells["LichSu_ID"].Value);
                    string tenHien = string.IsNullOrWhiteSpace(ten) ? $"ID {id}" : ten;

                    var confirm = MessageBox.Show(
                        $"Đơn {tenHien} sẽ bị xóa lịch sử xuất/nhập?",
                        "Xác nhận xóa",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirm != DialogResult.Yes) return;

                    SetToolbarEnabled(false);
                    await WaitingHelper.RunWithWaiting(
                        () => DatabaseHelper.DeleteLichSuXuatNhap(id),
                        "ĐANG XÓA...");
                }
                else
                {
                    FrmWaiting.ShowGifAlert(
                        "Kiểu báo cáo hiện tại không hỗ trợ xóa.",
                        "THÔNG BÁO", EnumStore.Icon.Warning);
                    return;
                }

                // UI thread: xóa dòng khỏi lưới
                dgr.Rows.RemoveAt(rowIndex);
                FrmWaiting.ShowGifAlert("Đã xóa thành công!", "THÀNH CÔNG", EnumStore.Icon.Success);
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
        //  SỬA (async) — bọc waiting cho thao tác DB
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Luồng:
        ///   UI thread  → đọc model từ row
        ///   BG thread  → gọi DB Update
        ///   UI thread  → thông báo kết quả
        /// </summary>
        private async Task UpdateByCurrentKieuAsync(DataGridView dgr, int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dgr.Rows.Count) return;

                var row = dgr.Rows[rowIndex];
                int kieu = cbxKieu.SelectedIndex;

                // ── Đọc model TRÊN UI THREAD trước khi vào Task.Run ──
                if (kieu == 1)
                {
                    var model = GetThongTinDatHangUpdateModel(row);

                    SetToolbarEnabled(false);
                    await WaitingHelper.RunWithWaiting(
                        () => DatabaseHelper.UpdateThongTinDatHang(model),
                        "ĐANG CẬP NHẬT...");

                    FrmWaiting.ShowGifAlert(
                        "Đã cập nhật ThongTinDatHang thành công!",
                        "THÀNH CÔNG", EnumStore.Icon.Success);
                }
                else if (kieu == 2 || kieu == 3)
                {
                    var model = GetLichSuXuatNhapUpdateModel(row);

                    SetToolbarEnabled(false);
                    await WaitingHelper.RunWithWaiting(
                        () => DatabaseHelper.UpdateLichSuXuatNhap(model),
                        "ĐANG CẬP NHẬT...");

                    FrmWaiting.ShowGifAlert(
                        "Đã cập nhật LichSuXuatNhap thành công!",
                        "THÀNH CÔNG", EnumStore.Icon.Success);
                }
                else
                {
                    FrmWaiting.ShowGifAlert(
                        "Kiểu báo cáo hiện tại không hỗ trợ cập nhật.",
                        "THÔNG BÁO", EnumStore.Icon.Warning);
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, "CẬP NHẬT"),
                    "LỖI CẬP NHẬT");
            }
            finally
            {
                SetToolbarEnabled(true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  HELPER: enable/disable toolbar tránh double-click trong khi đang chờ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vô hiệu hóa / kích hoạt toàn bộ nút toolbar trong lúc đang xử lý.
        /// ⚠ Phải gọi trên UI thread.
        /// </summary>
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
                //dgr.Columns.Add(new DataGridViewButtonColumn
                //{
                //    Name = COL_UPDATE,
                //    HeaderText = "",
                //    Text = "Sửa",
                //    UseColumnTextForButtonValue = true,
                //    Width = 75
                //});
            }

            if (!dgr.Columns.Contains(COL_DELETE))
            {
                //dgr.Columns.Add(new DataGridViewButtonColumn
                //{
                //    Name = COL_DELETE,
                //    HeaderText = "",
                //    Text = "Xóa",
                //    UseColumnTextForButtonValue = true,
                //    Width = 75
                //});
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ĐỌC MODEL TỪ ROW  (không thay đổi logic)
        // ═══════════════════════════════════════════════════════════════════════

        private LichSuXuatNhapUpdateModel GetLichSuXuatNhapUpdateModel(DataGridViewRow row)
        {
            decimal soLuong = 0;
            if (row.DataGridView.Columns.Contains("SoLuong") &&
                row.Cells["SoLuong"].Value != DBNull.Value)
                soLuong = Convert.ToDecimal(row.Cells["SoLuong"].Value);

            return new LichSuXuatNhapUpdateModel
            {
                Id = Convert.ToInt32(row.Cells["LichSu_ID"].Value),
                SoLuong = soLuong,
                NguoiGiaoNhan = row.DataGridView.Columns.Contains("NguoiGiao_Nhan")
                                    ? row.Cells["NguoiGiao_Nhan"].Value?.ToString()?.Trim() ?? "" : "",
                Kho = row.DataGridView.Columns.Contains("Kho")
                                    ? row.Cells["Kho"].Value?.ToString()?.Trim() ?? "" : "",
                LyDo = row.DataGridView.Columns.Contains("LyDo")
                                    ? row.Cells["LyDo"].Value?.ToString()?.Trim() ?? "" : "",
                Ngay = row.DataGridView.Columns.Contains("NgayXuatNhap")
                                    ? row.Cells["NgayXuatNhap"].Value?.ToString()?.Trim() ?? "" : "",
                TenPhieu = row.DataGridView.Columns.Contains("TenPhieu")
                                    ? row.Cells["TenPhieu"].Value?.ToString()?.Trim() ?? "" : "",
                GhiChu = row.DataGridView.Columns.Contains("GhiChu")
                                    ? row.Cells["GhiChu"].Value?.ToString()?.Trim() ?? "" : ""
            };
        }

        private ThongTinDatHangUpdateModel GetThongTinDatHangUpdateModel(DataGridViewRow row)
        {
            decimal soLuongMua = 0;
            if (row.DataGridView.Columns.Contains("SoLuongMua") &&
                row.Cells["SoLuongMua"].Value != DBNull.Value)
                soLuongMua = Convert.ToDecimal(row.Cells["SoLuongMua"].Value);
            else if (row.DataGridView.Columns.Contains("SL_YeuCau") &&
                     row.Cells["SL_YeuCau"].Value != DBNull.Value)
                soLuongMua = Convert.ToDecimal(row.Cells["SL_YeuCau"].Value);

            decimal donGia = 0;
            if (row.DataGridView.Columns.Contains("DonGia") &&
                row.Cells["DonGia"].Value != DBNull.Value)
                donGia = Convert.ToDecimal(row.Cells["DonGia"].Value);

            return new ThongTinDatHangUpdateModel
            {
                Id = Convert.ToInt32(row.Cells["ThongTinDatHang_ID"].Value),
                TenVatTu = row.DataGridView.Columns.Contains("TenVatTu")
                                ? row.Cells["TenVatTu"].Value?.ToString()?.Trim() ?? "" : "",
                SoLuongMua = soLuongMua,
                DonGia = donGia,
                MucDichMua = row.DataGridView.Columns.Contains("MucDichMua")
                                ? row.Cells["MucDichMua"].Value?.ToString()?.Trim() ?? "" : "",
                NgayGiao = row.DataGridView.Columns.Contains("NgayGiao")
                                ? row.Cells["NgayGiao"].Value?.ToString()?.Trim() ?? "" : "",
                GhiChu = row.DataGridView.Columns.Contains("GhiChu")
                                ? row.Cells["GhiChu"].Value?.ToString()?.Trim() ?? "" : ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SỰ KIỆN COMBOBOX / CHECKBOX
        // ═══════════════════════════════════════════════════════════════════════

        private void cbxThoiGian_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool custom = cbxThoiGian.SelectedIndex == 1;
            dtBatDau.Enabled = custom;
            dtKetThuc.Enabled = custom;
        }

        private void cbxLoaiYC_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool daNhap = cbxLoaiYC.SelectedIndex == 1;
            if (daNhap) { cbxdsKho.SelectedIndex = 0; cbxdsKho.Enabled = false; }
            else cbxdsKho.Enabled = true;
        }

        private void cbxAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in grvBaoCao.Rows)
                row.Cells[COL_CHECK].Value = cbxAll.Checked;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CELL FORMATTING
        // ═══════════════════════════════════════════════════════════════════════

        private void DgrXuatNhapTon_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!(sender is DataGridView dgr)) return;
            if (dgr.Columns[e.ColumnIndex].Name != "Edit") return;
            if (e.Value == null) return;

            if (int.TryParse(e.Value.ToString(), out int val))
            {
                e.Value = val == 0 ? "Có thể" : "Không thể";
                e.FormattingApplied = true;
            }
        }
    }
}