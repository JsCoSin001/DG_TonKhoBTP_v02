using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Authentication;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_KiemKe : UserControl
    {
        private Dictionary<string, decimal> dsBin = new Dictionary<string, decimal>();
        private DataTable _dtKiemKe = new DataTable();
        private bool _isProgrammaticChange = false;

        // Session lưu dữ liệu bin hiện tại
        private PrinterModel _currentPrinterModel = null;

        private bool _isBusy = false;

        private AutoCompleteDropDown _dropDownSP;
        private CancellationTokenSource _ctsSP;

        public UC_KiemKe()
        {
            InitializeComponent();
            InitAutoCompleteSanPham();
            InitComboBox();
        }

        private void InitComboBox()
        {
            List<string> data1 = new List<string>();
            List<string> data2 = new List<string>();

            for (int i = 1; i <= 10; i++)
            {
                string text = "Người " + i;
                data1.Add(text);
                data2.Add(text);
            }

            cbxNguoiKK_CauHinh.DataSource = data1;
            cbxNguoiKK.DataSource = data2;

            cbxNguoiKK_CauHinh.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxNguoiKK.DropDownStyle = ComboBoxStyle.DropDownList;

            cbxNguoiKK_CauHinh.SelectedIndex = 0;
            cbxNguoiKK.SelectedIndex = 0;
        }

        private void InitAutoCompleteSanPham()
        {
            _dropDownSP = new AutoCompleteDropDown();

            // Dọn dẹp khi UserControl bị destroy
            this.HandleDestroyed += (s, e) =>
            {
                _ctsSP?.Cancel();
                _ctsSP?.Dispose();
                _dropDownSP?.Dispose();
            };

            // Khi dropdown ẩn đi → kiểm tra DialogResult OK = người dùng đã chọn item
            _dropDownSP.VisibleChanged += (s, e) =>
            {
                if (_dropDownSP.Visible) return;
                if (_dropDownSP.DialogResult != DialogResult.OK) return;

                DataRow row = _dropDownSP.GetSelected();
                if (row == null) return;

                _isProgrammaticChange = true;
                try
                {
                    tbTenSanPham_KK.Text = row["Ten"]?.ToString() ?? "";
                    tbMa_KK.Text = row["id"]?.ToString() ?? "";
                }
                finally
                {
                    _isProgrammaticChange = false;
                    _dropDownSP.DialogResult = DialogResult.None;
                }

                nbrKLDong_KK.Focus();
                nbrKLDong_KK.Select(0, nbrKLDong_KK.Text.Length);
            };

            // ComboBox dùng TextUpdate (không trigger khi set bằng code)
            tbTenSanPham_KK.TextUpdate += TbTenSanPham_KK_TextUpdate;

            // Phím mũi tên xuống → chuyển focus sang dropdown
            tbTenSanPham_KK.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && _dropDownSP.Visible)
                {
                    _dropDownSP.Focus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.Escape)
                    _dropDownSP.Hide();
            };
        }

        private async void TbTenSanPham_KK_TextUpdate(object sender, EventArgs e)
        {
            // Bỏ qua nếu đang set bằng code
            if (_isProgrammaticChange) return;

            string keyword = tbTenSanPham_KK.Text.Trim();

            // Huỷ tìm kiếm cũ nếu đang chạy
            _ctsSP?.Cancel();
            _ctsSP = new CancellationTokenSource();
            var token = _ctsSP.Token;

            // Debounce 300ms
            try { await Task.Delay(300, token); }
            catch (TaskCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                _dropDownSP.Hide();
                tbMa_KK.Text = "";
                return;
            }

            try
            {
                string sql = @"
                    SELECT id, Ten, Ma
                    FROM DanhSachMaSP
                    WHERE Ten LIKE @kw
                      AND (
                            Ma LIKE 'NVL.%' COLLATE NOCASE
                             OR Ma LIKE 'TP.%'  COLLATE NOCASE
                             OR Ma LIKE 'BTP.%'  COLLATE NOCASE
                      )
                    ORDER BY Ten
                    LIMIT 30";

                var dt = await Task.Run(() =>
                    DatabaseHelper.GetData(sql, $"%{keyword}%", "kw"));

                if (token.IsCancellationRequested) return;

                if (dt == null || dt.Rows.Count == 0)
                {
                    _dropDownSP.Hide();
                    return;
                }

                // Hiển thị dropdown ngay bên dưới ComboBox
                var screenPt = tbTenSanPham_KK.Parent.PointToScreen(
                    new Point(tbTenSanPham_KK.Left, tbTenSanPham_KK.Bottom));

                _dropDownSP.Location = screenPt;
                _dropDownSP.Width = tbTenSanPham_KK.Width;
                _dropDownSP.LoadData(dt, "Ten");

                if (!_dropDownSP.Visible)
                    _dropDownSP.Show(this.ParentForm ?? (IWin32Window)this);
            }
            catch (Exception ex) when (!(ex is TaskCanceledException))
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tìm kiếm SP: " + ex.Message);
            }
        }

        private void SetValueSafe(NumericUpDown ctrl, decimal value)
        {
            try
            {
                _isProgrammaticChange = true;

                // tránh lỗi vượt min/max
                if (value < ctrl.Minimum) value = ctrl.Minimum;
                if (value > ctrl.Maximum) value = ctrl.Maximum;

                ctrl.Value = value;
            }
            finally
            {
                _isProgrammaticChange = false;
            }
        }
        
        private async void UC_KiemKe_Load(object sender, EventArgs e)
        {
            // Dùng WaitingHelper.RunWithWaiting<T>:
            //   - Func<Task<T>> chạy phần DB bất đồng bộ trên thread pool
            //   - Sau khi await xong, kết quả trả về UI thread → gán control an toàn
            (Dictionary<string, decimal> bins, DataTable dt) result =
                await WaitingHelper.RunWithWaiting(
                    async () =>
                    {
                        // Chạy song song hai truy vấn độc lập để nhanh hơn
                        var t1 = DatabaseHelper.LayDanhSachBin_KhoiLuong();
                        var t2 = System.Threading.Tasks.Task.Run(
                                     () => DatabaseHelper.Load_TTKiemKeThang());

                        await System.Threading.Tasks.Task.WhenAll(t1, t2);
                        return (t1.Result, t2.Result);
                    },
                    "ĐANG TẢI DỮ LIỆU...");

            // ── Cập nhật UI – luôn ở UI thread vì await trả về đây ──
            dsBin = result.bins ?? new Dictionary<string, decimal>();

            InitDataGridView();

            _dtKiemKe = result.dt ?? new DataTable();
            dsKiemKe.DataSource = _dtKiemKe;
            dsKiemKe.ClearSelection();
        }
        
        private async void tbTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            // Guard: không cho chạy lại khi đang xử lý
            if (_isBusy) return;


            string keyword = tbTimKiem.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword)) return;

            _isBusy = true;
            try
            {
                // ── Phần DB: chạy trong WaitingHelper, không động đến control ──
                DataRow row = await WaitingHelper.RunWithWaiting(
                    () => DatabaseHelper.LayThongTinTheoMaBin(keyword),
                    "ĐANG TÌM KIẾM...");

                if (row == null)
                {
                    FrmWaiting.ShowGifAlert($"KHÔNG TÌM THẤY BIN: {keyword}");
                    ClearFormSauKhiLuu();
                    return;
                }

                // ── Phần UI: sau await, chắc chắn ở UI thread ──────────────────
                string tenBin = CoreHelper.CatMaBin(keyword).LastOrDefault() ?? keyword;
                decimal klBin = dsBin.TryGetValue(tenBin, out var kl) ? kl : 0;

                tbQr.Text = keyword;
                tbTimKiem.Text = "";
                tbTenBin.Text = tenBin;

                tbTTThanhPhamID.Text = row?["TTThanhPham_ID"]?.ToString() ?? "";

                tbTenSanPham_DB.Text = row?["Ten"]?.ToString() ?? "";
                tbTenSanPham_KK.Text = row?["Ten"]?.ToString() ?? "";
                tbMa_DB.Text = row?["Ma"]?.ToString() ?? "";
                tbMa_KK.Text = row?["DanhSachSP_ID"]?.ToString() ?? "";

                decimal klSau = CoreHelper.TryParseDecimal(row?["KhoiLuongSau"]) ?? 0;
                decimal cdSau = CoreHelper.TryParseDecimal(row?["ChieuDaiSau"]) ?? 0;

                nbrKLDong_DB.Value = klSau;
                nbrKLDong_KK.Value = klSau;
                nbrChieuDai_DB.Value = cdSau;
                nbrChieuDai_KK.Value = cdSau;

                nbrKLBi_DB.Value = klBin;
                nbrKLBi_KK.Value = klBin;

                decimal klTong = klBin + klSau;
                nbrKLTong_DB.Value = klTong;
                nbrKLTong_KK.Value = klTong;

                rtbGhiChu.Text = (row?["GhiChu"]?.ToString() ?? "") + "\nKK";

                _currentPrinterModel = new PrinterModel
                {
                    id = row?["DanhSachSP_ID"]?.ToString(),
                    TenSP = row?["Ten"]?.ToString() ?? "",
                    MaSP = row?["Ma"]?.ToString() ?? "",
                    MaBin = keyword,
                    NgaySX = row?["NgaySX"]?.ToString() ?? "",
                    CaSX = row?["CaSX"]?.ToString() ?? "",
                    TenCN = row?["TenCN"]?.ToString() ?? "",
                    Mau = row?["Mau"]?.ToString() ?? "",
                    QC = row?["QC"]?.ToString() ?? "",
                    GhiChu = (row?["GhiChu"]?.ToString() ?? "") + "\nKK",
                };
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "TÌM KIẾM BIN"));
            }
            finally
            {
                _isBusy = false;
            }
        }
               
        private void btnTaoQr_Click(object sender, EventArgs e)
        {
            ClearFormSauKhiLuu();
            string bin = tbTenBin.Text.Trim();
            tbQr.Text = CoreHelper.GenerateLOT(bin, dsBin);
            tbTenBin.Text = CoreHelper.CatMaBin(tbQr.Text).LastOrDefault();
        }
                
        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            string maBin = tbQr.Text.Trim();
            if (string.IsNullOrWhiteSpace(maBin))
            {
                FrmWaiting.ShowGifAlert("Vui lòng quét hoặc tạo mã bin trước khi lưu.");
                return;
            }

            string nguoiKK = cbxNguoiKK.SelectedItem?.ToString() ?? ""; 
            if (string.IsNullOrWhiteSpace(nguoiKK))
            {
                FrmWaiting.ShowGifAlert("Chọn người Kiểm kê.");
                return;
            }

            // ── Kiểm tra trùng MaBin trong grid ─────────────────────────────────
            bool isDuplicate = _dtKiemKe?.AsEnumerable()
                .Any(r => string.Equals(
                        r["MaBin"]?.ToString(),
                        maBin,
                        StringComparison.OrdinalIgnoreCase)) == true;

            if (isDuplicate)
            {
                FrmWaiting.ShowGifAlert(
                    $"{maBin} đã có trong danh sách.",
                    "TRÙNG DỮ LIỆU");
                return;
            }

            if (tbMa_KK.Text.Trim() == "" || (nbrKLDong_KK.Value == 0 && nbrChieuDai_KK.Value == 0))
            {
                FrmWaiting.ShowGifAlert("Kiểm tra lại dữ liệu");
                return;
            }

            // ── Thu thập dữ liệu trên UI thread TRƯỚC khi chạy nền ──────────────
            KiemKe kiemKeModel = BuildKiemKeModelFromForm();
            DanhSachBin binModel = new DanhSachBin
            {
                KhoiLuongBin = nbrKLBi_KK.Value,
                TenBin = tbTenBin.Text.Trim()
            };

            PrinterModel printerModel = BuildPrinterModelForPrint();
            bool shouldPrint = cbInTem.Checked;

            _isBusy = true;
            try
            {
                // ── Phần DB: chạy trên thread pool, KHÔNG đụng UI ────────────────
                long newId = await WaitingHelper.RunWithWaiting(
                    () => System.Threading.Tasks.Task.Run(() =>
                    {
                        if (QrIsKiemKe(tbQr.Text.Trim()))
                        {
                            long TTThanhPham_id = DatabaseHelper.InsertTTThanhPham_FromKiemKe(kiemKeModel);
                            kiemKeModel.TTThanhPham_ID = TTThanhPham_id;
                        }

                        long id = DatabaseHelper.Insert_TTKiemKeThang(kiemKeModel);

                        if (tbTenBin.Text.Trim() != "00") DatabaseHelper.UpsertDanhSachBin(binModel);
                        return id;
                    }),
                    "ĐANG LƯU...");

                // ── Phần UI: sau await, chắc chắn ở UI thread ────────────────────
                kiemKeModel.id = newId;
                InsertRowToGridTop(kiemKeModel);

                // PrintDocument dùng GDI+ → phải chạy trên UI thread
                if (printerModel != null && shouldPrint)
                {
                    try
                    {
                        PrintHelper.PrintLabel(printerModel);
                    }
                    catch (Exception printEx)
                    {
                        FrmWaiting.ShowGifAlert(
                            $"Lưu thành công nhưng in thất bại:\n{printEx.Message}",
                            "CẢNH BÁO IN");
                    }
                }

                FrmWaiting.ShowGifAlert("Lưu kiểm kê thành công.", "THÀNH CÔNG", EnumStore.Icon.Success);

                ClearFormSauKhiLuu();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "LƯU KIỂM KÊ THÁNG"));
            }
            finally
            {
                _isBusy = false;
            }
        }
                
        private void InitDataGridView()
        {
            dsKiemKe.Columns.Clear();
            dsKiemKe.AutoGenerateColumns = false;
            dsKiemKe.AllowUserToAddRows = false;
            dsKiemKe.RowHeadersVisible = false;
            dsKiemKe.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dsKiemKe.EditMode = DataGridViewEditMode.EditOnEnter;

            dsKiemKe.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.SteelBlue;
            dsKiemKe.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;

            // ✅ Cột checkbox đầu tiên
            dsKiemKe.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colCheck",
                HeaderText = "",
                Width = 40
            });

            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DanhSachSP_ID",
                HeaderText = "DanhSachSP_ID",
                DataPropertyName = "DanhSachSP_ID",
                Visible = false // QUAN TRỌNG: ẩn đi
            });

            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "ID", DataPropertyName = "id", Width = 60, ReadOnly = true });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Tên SP", DataPropertyName = "Ten", Width = 220, ReadOnly = true });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaBin", HeaderText = "LOT", DataPropertyName = "MaBin", Width = 150, ReadOnly = true });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChieuDai", HeaderText = "Chiều dài", DataPropertyName = "ChieuDai", Width = 100 });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "KhoiLuong", HeaderText = "Khối lượng", DataPropertyName = "KhoiLuong", Width = 100 });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", DataPropertyName = "GhiChu", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dsKiemKe.Columns.Add(new DataGridViewTextBoxColumn { Name = "NguoiKK", HeaderText = "Người KK", DataPropertyName = "NguoiKK",  Width = 100,  ReadOnly = true });
            dsKiemKe.Columns.Add(new DataGridViewButtonColumn { Name = "colEdit", HeaderText = "", Text = "Cập nhật", UseColumnTextForButtonValue = true, Width = 70 });
            dsKiemKe.Columns.Add(new DataGridViewButtonColumn { Name = "colDelete", HeaderText = "", Text = "Xóa", UseColumnTextForButtonValue = true, Width = 70 });

            dsKiemKe.CellContentClick -= dsKiemKe_CellContentClick;
            dsKiemKe.CellContentClick += dsKiemKe_CellContentClick;
        }

        private async void dsKiemKe_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _isBusy) return;

            dsKiemKe.EndEdit();

            string colName = dsKiemKe.Columns[e.ColumnIndex].Name;
            DataGridViewRow gridRow = dsKiemKe.Rows[e.RowIndex];

            if (colName == "colEdit")
            {
                // ── Thu thập dữ liệu từ grid trên UI thread ──────────────────────
                int id;
                if (!int.TryParse(gridRow.Cells["id"].Value?.ToString(), out id)) return;

                decimal? chieuDai = TryGetDecimalFromCell(gridRow.Cells["ChieuDai"]);
                decimal? khoiLuong = TryGetDecimalFromCell(gridRow.Cells["KhoiLuong"]);
                string ghiChu = gridRow.Cells["GhiChu"].Value?.ToString();

                _isBusy = true;
                try
                {
                    bool ok = await WaitingHelper.RunWithWaiting(
                        () => System.Threading.Tasks.Task.Run(
                            () => DatabaseHelper.Update_TTKiemKeThang(id, chieuDai, khoiLuong, ghiChu)),
                        "ĐANG CẬP NHẬT...");

                    FrmWaiting.ShowGifAlert(ok ? "Cập nhật thành công." : "Không có dòng nào được cập nhật.");
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "EDIT KIỂM KÊ THÁNG"));
                }
                finally
                {
                    _isBusy = false;
                }
            }
            else if (colName == "colDelete")
            {
                int id;
                if (!int.TryParse(gridRow.Cells["id"].Value?.ToString(), out id)) return;

                DialogResult rs = MessageBox.Show(
                    "Bạn có chắc muốn xóa dòng này không?",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (rs != DialogResult.Yes) return;

                _isBusy = true;
                try
                {
                    // Lưu rowIndex trước khi vào async (tránh row bị thay đổi)
                    int rowIndex = e.RowIndex;

                    bool ok = await WaitingHelper.RunWithWaiting(
                        () => System.Threading.Tasks.Task.Run(
                            () => DatabaseHelper.Delete_TTKiemKeThang(id)),
                        "ĐANG XÓA...");

                    // Cập nhật DataTable trên UI thread
                    if (ok && _dtKiemKe != null
                           && rowIndex >= 0
                           && rowIndex < _dtKiemKe.Rows.Count)
                    {
                        _dtKiemKe.Rows.RemoveAt(rowIndex);
                        FrmWaiting.ShowGifAlert("Xóa thành công.", "THÔNG BÁO", EnumStore.Icon.Success);
                    }
                    else if (!ok)
                    {
                        FrmWaiting.ShowGifAlert("Không tìm thấy dòng cần xóa.");
                    }
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "XÓA KIỂM KÊ THÁNG"));
                }
                finally
                {
                    _isBusy = false;
                }
            }
        }
                
        private static decimal? TryGetDecimalFromCell(DataGridViewCell cell)
        {
            if (cell?.Value == null || cell.Value == DBNull.Value) return null;
            string s = cell.Value.ToString();
            if (string.IsNullOrWhiteSpace(s)) return null;
            return decimal.TryParse(s, out decimal v) ? v : (decimal?)null;
        }

        private KiemKe BuildKiemKeModelFromForm()
        {
            long? parsedId = null;
            if (!string.IsNullOrWhiteSpace(tbMa_KK.Text) &&
                long.TryParse(tbMa_KK.Text.Trim(), out long ttId))
                parsedId = ttId;

            DateTime now = DateTime.Now;

            long ttThanhPhamId = 0;
            long.TryParse(tbTTThanhPhamID.Text, out ttThanhPhamId);

            return new KiemKe
            {
                TTThanhPham_ID = ttThanhPhamId,
                DanhSachSP_ID = parsedId,
                MaBin = tbQr.Text.Trim(),
                ChieuDai = nbrChieuDai_KK.Value,
                KhoiLuong = nbrKLDong_KK.Value,
                GhiChu = (string.IsNullOrWhiteSpace(rtbGhiChu.Text)
                                    ? null : rtbGhiChu.Text.Trim()) + "\nKK",
                ThoiGianKiemKe = now.ToString("yyyy-MM"),
                ApprovedDate = null,
                DateInsert = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Ten = tbTenSanPham_KK.Text.Trim(),
                NguoiKK = cbxNguoiKK.SelectedItem?.ToString() ?? ""
            };
        }

        private bool QrIsKiemKe(string qr)
        {
            string value = string.IsNullOrWhiteSpace(qr)
                    ? ""
                    : tbQr.Text.Trim()[0].ToString();

            return value == "X";
        }

        private PrinterModel BuildPrinterModelForPrint()
        {
            if (_currentPrinterModel == null)
            {
               string qr = tbQr.Text.Trim();
               
                if (QrIsKiemKe(qr)) {

                    return new PrinterModel()
                    {
                        NgaySX = DateTime.Now.ToString("dd-MM-yyyy"),
                        CaSX = "1",
                        KhoiLuong = nbrKLDong_KK.Value.ToString(),
                        ChieuDai = nbrChieuDai_KK.Value.ToString(),
                        TenSP = tbTenSanPham_KK.Text.Trim(),
                        QC = "0",
                        MaBin = qr,
                        MaSP = tbMa_KK.Text.Trim(),
                        TenCN = "KK",
                        GhiChu = (string.IsNullOrWhiteSpace(rtbGhiChu.Text)
                               ? "" : rtbGhiChu.Text.Trim()) + "\nKK",
                        DanhGia = ""
                    };
                }
                else
                {
                    return null;
                }

            }

            return new PrinterModel(_currentPrinterModel)
            {
                KhoiLuong = nbrKLDong_KK.Value.ToString(),
                ChieuDai = nbrChieuDai_KK.Value.ToString(),
                GhiChu = (string.IsNullOrWhiteSpace(rtbGhiChu.Text)
                               ? "" : rtbGhiChu.Text.Trim()) + "\nKK",
                DanhGia = ""
            };
        }

        private void InsertRowToGridTop(KiemKe model)
        {
            if (model == null) return;

            if (_dtKiemKe == null)
                _dtKiemKe = new DataTable();

            if (_dtKiemKe.Columns.Count == 0)
            {
                _dtKiemKe.Columns.Add("DanhSachSP_ID", typeof(long));
                _dtKiemKe.Columns.Add("id", typeof(long));
                _dtKiemKe.Columns.Add("Ten", typeof(string));
                _dtKiemKe.Columns.Add("MaBin", typeof(string));
                _dtKiemKe.Columns.Add("ChieuDai", typeof(decimal));
                _dtKiemKe.Columns.Add("KhoiLuong", typeof(decimal));
                _dtKiemKe.Columns.Add("GhiChu", typeof(string));
                _dtKiemKe.Columns.Add("NguoiKK", typeof(string));
            }

            DataRow newRow = _dtKiemKe.NewRow();
            newRow["DanhSachSP_ID"] = model.DanhSachSP_ID.HasValue ? (object)model.DanhSachSP_ID.Value : DBNull.Value;
            newRow["id"] = model.id.HasValue ? (object)model.id.Value : DBNull.Value;
            newRow["Ten"] = model.Ten ?? "";
            newRow["MaBin"] = model.MaBin ?? "";
            newRow["ChieuDai"] = model.ChieuDai.HasValue ? (object)model.ChieuDai.Value : DBNull.Value;
            newRow["KhoiLuong"] = model.KhoiLuong.HasValue ? (object)model.KhoiLuong.Value : DBNull.Value;
            newRow["GhiChu"] = string.IsNullOrWhiteSpace(model.GhiChu)
                                    ? (object)DBNull.Value : model.GhiChu;
            newRow["NguoiKK"] = model.NguoiKK;

            _dtKiemKe.Rows.InsertAt(newRow, 0);

            if (dsKiemKe.DataSource == null)
                dsKiemKe.DataSource = _dtKiemKe;
        }

        private void ClearFormSauKhiLuu()
        {
            _currentPrinterModel = null;

            tbTimKiem.Clear();
            tbQr.Clear();
            tbTenBin.Clear();

            tbTenSanPham_DB.Clear();
            tbTenSanPham_KK.SelectedIndex = -1;
            tbTenSanPham_KK.Text = "";

            tbMa_DB.Clear();
            tbMa_KK.Clear();

            nbrKLDong_DB.Value = 0;
            nbrKLDong_KK.Value = 0;
            nbrKLBi_DB.Value = 0;
            nbrKLBi_KK.Value = 0;
            nbrKLTong_DB.Value = 0;
            nbrKLTong_KK.Value = 0;
            nbrChieuDai_DB.Value = 0;
            nbrChieuDai_KK.Value = 0;

            cbInTem.Checked = true;

            tbTTThanhPhamID.Text = "";
            rtbGhiChu.Clear();
            tbTimKiem.Focus();
        }

        private void nbrKLBi_KK_ValueChanged(object sender, EventArgs e)
        {

            if (_isProgrammaticChange) return;

            decimal klTong = nbrKLDong_KK.Value;
            decimal klBi = nbrKLBi_KK.Value;

            decimal klDong = klTong - klBi;

            // Set giá trị tổng (KHÔNG trigger lại event)
            SetValueSafe(nbrKLDong_KK, klDong);
        }

        private void nbrKLTong_KK_ValueChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange) return;

            decimal klTong = nbrKLTong_KK.Value;
            decimal klBi = nbrKLBi_KK.Value;

            decimal klDong = klTong - klBi;

           

            // Set giá trị tổng (KHÔNG trigger lại event)
            SetValueSafe(nbrKLDong_KK, klDong);
        }

        private async void btnCapNhatDuLieu_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            dsKiemKe.EndEdit();

            var items = new List<KiemKe>();

            foreach (DataGridViewRow row in dsKiemKe.Rows)
            {
                if (row == null || row.IsNewRow) continue;

                bool isChecked = row.Cells["colCheck"]?.Value != null
                                 && row.Cells["colCheck"].Value != DBNull.Value
                                 && Convert.ToBoolean(row.Cells["colCheck"].Value);

                if (!isChecked) continue;

                if (!long.TryParse(row.Cells["DanhSachSP_ID"]?.Value?.ToString(), out long danhSachSpId))
                    continue;

                string maBin = row.Cells["MaBin"]?.Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(maBin))
                    continue;

                decimal? chieuDai = TryGetDecimalFromCell(row.Cells["ChieuDai"]);
                decimal? khoiLuong = TryGetDecimalFromCell(row.Cells["KhoiLuong"]);
                string ghiChu = row.Cells["GhiChu"]?.Value?.ToString();

                items.Add(new KiemKe
                {
                    id = row.Cells["id"]?.Value != null && row.Cells["id"].Value != DBNull.Value
                            ? (long?)Convert.ToInt64(row.Cells["id"].Value)
                            : null,
                    DanhSachSP_ID = danhSachSpId,
                    MaBin = maBin,
                    ChieuDai = chieuDai,
                    KhoiLuong = khoiLuong,
                    GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
                    DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            if (items.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn ít nhất 1 dòng hợp lệ.");
                return;
            }

            _isBusy = true;
            try
            {
                int affected = await WaitingHelper.RunWithWaiting(
                    () => System.Threading.Tasks.Task.Run(() =>
                        DatabaseHelper.UpsertTTThanhPhamByMaBin(items)),
                    "ĐANG CẬP NHẬT DỮ LIỆU...");

                FrmWaiting.ShowGifAlert(
                    affected > 0
                        ? $"Cập nhật thành công."
                        : "Không có dòng nào được cập nhật.",
                    affected > 0 ? "THÀNH CÔNG" : "THÔNG BÁO",
                    affected > 0 ? EnumStore.Icon.Success : EnumStore.Icon.Warning);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "UPSERT TTTHANHPHAM"));
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void cbCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            SetCheckAllRows(cbCheckAll.Checked);
        }
       
        private void SetCheckAllRows(bool isChecked)
        {
            if (dsKiemKe.Rows.Count == 0) return;

            dsKiemKe.EndEdit();

            foreach (DataGridViewRow row in dsKiemKe.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.Cells["colCheck"].Value = isChecked;
                }
            }

            dsKiemKe.RefreshEdit();
        }

        private async void dtThangKiemKe_ValueChanged(object sender, EventArgs e)
        {
            string nguoiKK = cbxNguoiKK.SelectedItem?.ToString();

            await LoadDataKiemKeAsync( nguoiKK);
        }

        private async Task LoadDataKiemKeAsync(string nguoiKK)
        {
            if (_isBusy) return;

            _isBusy = true;
            try
            {

                string namThang = dtThangKiemKe.Value.ToString("yyyy-MM");

                DataTable dt = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() =>
                        DatabaseHelper.Load_TTKiemKeThang(namThang, nguoiKK)),
                    "ĐANG TẢI DỮ LIỆU KIỂM KÊ...");

                _dtKiemKe = dt ?? new DataTable();
                dsKiemKe.DataSource = _dtKiemKe;
                dsKiemKe.ClearSelection();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "TẢI KIỂM KÊ THEO THÁNG"));
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void nbrKLDong_KK_ValueChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange) return;

            decimal klDong = nbrKLDong_KK.Value;
            decimal klBi = nbrKLBi_KK.Value;

            decimal klTong = klDong + klBi;

            // Set giá trị tổng (KHÔNG trigger lại event)
            SetValueSafe(nbrKLTong_KK, klTong);
        }

        private async void cbxNguoiKK_CauHinh_SelectedIndexChanged(object sender, EventArgs e)
        {

            string nguoiKK = cbxNguoiKK_CauHinh.SelectedItem?.ToString();

            await LoadDataKiemKeAsync(nguoiKK);
        }

        private void btnXuatExcel_Click(object sender, EventArgs e)
        {
            if (_dtKiemKe == null || _dtKiemKe.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không có dữ liệu để xuất.", "THÔNG BÁO");
                return;
            }

            // Tạo bản sao để loại bỏ cột không cần thiết trước khi xuất
            DataTable dtExport = _dtKiemKe.Copy();
            dtExport.Columns.Remove("DanhSachSP_ID"); // Ẩn cột nội bộ

            string thang = dtThangKiemKe.Value.ToString("yyyy-MM");
            ExcelExporter.Export(dtExport, $"KiemKe_{thang}");
        }
    }
}