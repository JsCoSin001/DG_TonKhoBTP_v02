using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;          // WaitingHelper (có sẵn)
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    public partial class UC_BaoCao : UserControl
    {
        // ─── Fields ────────────────────────────────────────────────────────────
        private readonly DataGridView grvBaoCao = new DataGridView();

        private const string COL_CHECK = "colChon";
        private const string COL_UPDATE = "colUpdate";
        private const string COL_DELETE = "colDelete";

        // ─── Constructor ───────────────────────────────────────────────────────
        public UC_BaoCao(List<string> khoList)
        {
            InitializeComponent();
            InitComboBoxes(khoList);

            // Đăng ký sự kiện một lần duy nhất tại đây
            grvBaoCao.CellContentClick += GrvBaoCao_CellContentClick;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════════════

        private void InitComboBoxes(List<string> khoList)
        {
            cbxThoiGian.SelectedIndex = 0;
            cbxLoaiYC.SelectedIndex = 0;

            var newList = new List<string>(khoList);
            newList.Insert(0, "Không cần");
            cbxdsKho.DataSource = newList;
            cbxdsKho.SelectedIndex = 0;
            cbxKieu.SelectedIndex = 0;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SỰ KIỆN BUTTON TOOLBAR
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Báo cáo Xuất - Nhập - Tồn (LoaiDon = 1)
        /// 
        /// Luồng:
        ///   UI thread  → hiện FrmWaiting
        ///   BG thread  → gọi DB (LoadLichSuXuatNhap_LoaiDon1)
        ///   UI thread  → tắt waiting, bind DataTable lên lưới
        /// </summary>
        private async void btnIn_Out_Click(object sender, EventArgs e)
        {
            cbxAll.Enabled = false;
            lblTieuDe.Text = "BÁO CÁO XUẤT - NHẬP - TỒN";
            bool layDong1 = cbxLoaiYC.SelectedIndex == 3;

            SetToolbarEnabled(false);
            DataTable dt = null;

            // ── WaitingHelper.RunWithWaiting dùng Task.Run nội bộ ──
            // Hàm lambda truyền vào chạy trên background thread.
            // Mọi thao tác UI (bind lưới) đặt SAU await.
            await WaitingHelper.RunWithWaiting(async () =>
            {
                // BG thread: chỉ truy vấn DB, KHÔNG động vào UI
                dt = await Task.Run(() => DatabaseHelper.LoadLichSuXuatNhap_LoaiDon1(layDong1));
            }, "ĐANG TẢI LỊCH SỬ XUẤT NHẬP...");

            // UI thread: bind kết quả
            HienThiLenLuoi(dt, applyFormatTimDL: false);
            SetToolbarEnabled(true);
        }

        /// <summary>
        /// Tìm kiếm theo bộ lọc
        /// 
        /// Luồng:
        ///   UI thread  → đọc giá trị filter từ controls, hiện FrmWaiting
        ///   BG thread  → gọi DB tương ứng theo kieu
        ///   UI thread  → tắt waiting, bind / export Excel
        /// </summary>
        private async void btnTimDL_Click(object sender, EventArgs e)
        {
            cbxAll.Enabled = true;
            cbxAll.Checked = false;
            lblTieuDe.Text = "BÁO CÁO";

            // ── Đọc giá trị filter TRÊN UI THREAD trước khi vào Task.Run ──
            string ngayBatDau = cbxThoiGian.SelectedIndex == 0
                                    ? "" : dtBatDau.Value.ToString("yyyy-MM-dd");
            string ngayKetThuc = cbxThoiGian.SelectedIndex == 0
                                    ? "" : dtKetThuc.Value.ToString("yyyy-MM-dd");
            string kho = cbxdsKho.SelectedIndex == 0
                                    ? "" : cbxdsKho.SelectedItem.ToString();
            int tinhTrang = cbxLoaiYC.SelectedIndex;
            int kieu = cbxKieu.SelectedIndex;
            bool exportExcel = cbxExportExcel.Checked;

            SetToolbarEnabled(false);
            DataTable dt = null;

            try
            {
                await WaitingHelper.RunWithWaiting(async () =>
                {
                    // BG thread: chỉ truy vấn DB
                    dt = await Task.Run(() =>
                    {
                        switch (kieu)
                        {
                            case 1:
                                return DatabaseHelper.GetBaoCaoDatHang(ngayBatDau, ngayKetThuc);

                            case 2:
                                return DatabaseHelper.GetBaoCaoLichSuXuatNhap(
                                    ngayBatDau, ngayKetThuc, kho, tinhTrang,
                                    soLuongDuong: true);

                            case 3:
                                return DatabaseHelper.GetBaoCaoLichSuXuatNhap(
                                    ngayBatDau, ngayKetThuc, kho, tinhTrang,
                                    soLuongDuong: false);

                            case 0:
                            default:
                                return DatabaseHelper.GetBaoCao(ngayBatDau, ngayKetThuc, kho, tinhTrang);
                        }
                    });
                }, "ĐANG TẢI DỮ LIỆU BÁO CÁO...");

                // UI thread: xử lý kết quả
                if (exportExcel)
                {
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("Không có dữ liệu để xuất.", "Export", EnumStore.Icon.Warning);
                        return;
                    }

                    // Bước 1: SaveFileDialog PHẢI chạy trên UI thread → lấy path trước
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

                    // Bước 2: Ghi file trên background thread → UI thread rảnh → GIF chạy được
                    await WaitingHelper.RunWithWaiting(
                        () => ExcelExporter.ExportToPath(dt, filePath),
                        "ĐANG XUẤT FILE EXCEL...");

                    FrmWaiting.ShowGifAlert("Đã xuất Excel thành công!", "Export", EnumStore.Icon.Success);
                    return;
                }

                HienThiLenLuoi(dt, applyFormatTimDL: true);
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
        private void HienThiLenLuoi(DataTable dt, bool applyFormatTimDL)
        {
            pnBaoCao.Controls.Clear();
            grvBaoCao.Dock = DockStyle.Fill;
            pnBaoCao.Controls.Add(grvBaoCao);

            grvBaoCao.DataSource = null;
            grvBaoCao.Columns.Clear();

            if (dt == null || dt.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu");
                return;
            }

            grvBaoCao.AutoGenerateColumns = true;
            grvBaoCao.DataSource = dt;

            if (applyFormatTimDL)
                ApplyFormatTimDL(grvBaoCao);
            else
                ApplyFormatInOut(grvBaoCao);
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
                    case "DanhSachDatHang_ID":
                    case "ThongTinDatHang_ID":
                    case "LoaiDon":
                    case "CanEdit":
                        col.Visible = false; break;

                    case "MaDon": col.Width = 170; break;
                    case "YeuCau": col.Width = 170; break;
                    case "MaHang": col.Width = 150; break;
                    case "DonVi": col.Width = 170; break;

                    case "TenVatTu":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; break;

                    case "Nhap":
                    case "Xuat":
                    case "Ton":
                        col.Width = 110;
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

        private void ApplyFormatTimDL(DataGridView dgr)
        {
            dgr.AllowUserToAddRows = false;
            dgr.RowHeadersVisible = false;
            dgr.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgr.ColumnHeadersHeight = 30;
            dgr.RowTemplate.Height = 30;

            if (!dgr.Columns.Contains(COL_CHECK))
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
                    IndeterminateValue = false
                });
            }

            foreach (DataGridViewColumn col in dgr.Columns)
            {
                if (col.Name == COL_CHECK) { col.ReadOnly = false; continue; }
                if (col.Name == COL_UPDATE || col.Name == COL_DELETE) continue;

                if (col.Name.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) ||
                    col.Name == "LoaiDon" ||
                    col.Name == "Edit")
                {
                    col.Visible = false;
                }
                else
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }

            foreach (DataGridViewRow row in dgr.Rows)
            {
                row.Height = 30;
                if (!dgr.Columns.Contains("Edit")) continue;

                var editVal = row.Cells["Edit"].Value;
                row.Cells[COL_CHECK].Value =
                    editVal != null &&
                    editVal != DBNull.Value &&
                    Convert.ToInt32(editVal) == 0;
            }

            AddActionColumns(dgr);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  XỬ LÝ CLICK NÚT SỬA / XÓA TRÊN LƯỚI
        // ═══════════════════════════════════════════════════════════════════════

        private void GrvBaoCao_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var dgr = sender as DataGridView;
            if (dgr == null) return;

            string colName = dgr.Columns[e.ColumnIndex].Name;

            bool isLocked = dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value != null &&
                            Convert.ToBoolean(dgr.Rows[e.RowIndex].Cells[COL_CHECK].Value);

            if (isLocked && (colName == COL_UPDATE || colName == COL_DELETE))
            {
                FrmWaiting.ShowGifAlert(
                    "Dòng này đang bị khóa, không thể sửa/xóa!",
                    "THÔNG BÁO", EnumStore.Icon.Warning);
                return;
            }

            if (colName == COL_UPDATE)
                _ = UpdateByCurrentKieuAsync(dgr, e.RowIndex);
            else if (colName == COL_DELETE)
                _ = DeleteByCurrentKieuAsync(dgr, e.RowIndex);
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
                dgr.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = COL_UPDATE,
                    HeaderText = "",
                    Text = "Sửa",
                    UseColumnTextForButtonValue = true,
                    Width = 75
                });
            }

            if (!dgr.Columns.Contains(COL_DELETE))
            {
                dgr.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = COL_DELETE,
                    HeaderText = "",
                    Text = "Xóa",
                    UseColumnTextForButtonValue = true,
                    Width = 75
                });
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