using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    public partial class UC_XuatKho : UserControl
    {
        // ── State ────────────────────────────────────────────────────────────────
        private ComboBoxSearchHelper _lotSearchHelper;

        /// <summary>TTThanhPham.id của bin đang được chọn.</summary>
        private long? _selectedTTThanhPhamID = null;

        /// <summary>Loại nhập kho của bin đang chọn: "Lô" hoặc "Cuộn".</summary>
        private string _loaiNhapKho = string.Empty;

        /// <summary>
        /// id của bản ghi TTXuatKho đang được chỉnh sửa/xem.
        /// null = đang ở chế độ nhập mới.
        /// </summary>
        private long? _editingXuatKhoId = null;

        // ── Constructor ──────────────────────────────────────────────────────────
        public UC_XuatKho()
        {
            InitializeComponent();
            InitLotSearch();
            InitGridFont();
            InitButtonState();
            WireEvents();
        }

        // ════════════════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Khi khởi tạo: disable Sửa, Xoá; enable Lưu.</summary>
        private void InitButtonState()
        {
            btnSua.Enabled = false;
            btnXoa.Enabled = false;
            btnLuuXuatKho.Enabled = true;
        }

        private void WireEvents()
        {
            dgvLayDL.CellValueChanged += DgvLayDL_CellValueChanged;
            dgvLayDL.CurrentCellDirtyStateChanged += DgvLayDL_CurrentCellDirtyStateChanged;
            dgvLayDL.EditingControlShowing += DgvLayDL_EditingControlShowing;

            dgvLayDL_preview.CellDoubleClick += dgvLayDL_preview_CellDoubleClick;

            tbTimKiem.KeyDown += TbTimKiem_KeyDown;

            btnLuuXuatKho.Click += BtnLuuXuatKho_Click;
            btnSua.Click += BtnSua_Click;
            btnXoa.Click += BtnXoa_Click;
        }

        // ════════════════════════════════════════════════════════════════════════
        // COMBOBOX TÌM KIẾM LOT (MaBin)
        // ════════════════════════════════════════════════════════════════════════

        private void InitLotSearch()
        {
            _lotSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimLOT,
                queryFunc: XuatKho_DB.TimKiemLotAsync
            );

            _lotSearchHelper.DisplayColumn = "MaBin";
            _lotSearchHelper.ItemSelected += OnLotSelected;
            _lotSearchHelper.Cleared += OnLotCleared;
        }

        // ── Khi chọn một LOT từ dropdown ─────────────────────────────────────────
        private void OnLotSelected(DataRowView row)
        {
            tbLot.Text = row["MaBin"]?.ToString() ?? string.Empty;
            tbTenSP.Text = row["Ten"]?.ToString() ?? string.Empty;

            _loaiNhapKho = row["Loai"]?.ToString() ?? string.Empty;

            _selectedTTThanhPhamID = long.TryParse(
                row["TTThanhPham_ID"]?.ToString(), out long id) ? id : (long?)null;

            if (_selectedTTThanhPhamID.HasValue)
                LoadCuonDayGrid(_selectedTTThanhPhamID.Value);

            // Đặt lại về chế độ nhập mới
            SetNewMode();
        }

        // ── Khi xoá / reset combobox ─────────────────────────────────────────────
        private void OnLotCleared()
        {
            tbLot.Text = string.Empty;
            tbTenSP.Text = string.Empty;
            _selectedTTThanhPhamID = null;
            _loaiNhapKho = string.Empty;
            dgvLayDL.Rows.Clear();
            SetNewMode();
        }

        // ════════════════════════════════════════════════════════════════════════
        // NẠP LƯỚI dgvLayDL
        // ════════════════════════════════════════════════════════════════════════

        private void LoadCuonDayGrid(long ttThanhPhamId)
        {
            try
            {
                DataTable dt = XuatKho_DB.LayDuLieuCuonDay(ttThanhPhamId);

                dgvLayDL.Rows.Clear();

                foreach (DataRow dr in dt.Rows)
                {
                    int rowIdx = dgvLayDL.Rows.Add();
                    DataGridViewRow r = dgvLayDL.Rows[rowIdx];

                    // Dữ liệu gốc (readonly) — từ TTNhapKho + TTCuonDay
                    r.Cells["ttNhapKho_ID"].Value = dr["TTNhapKho_ID"];
                    r.Cells["tongCD"].Value = dr["TongChieuDai"];
                    r.Cells["soCuon"].Value = dr["SoCuon"];
                    r.Cells["soDau"].Value = dr["SoDau"];
                    r.Cells["soCuoi"].Value = dr["SoCuoi"];
                    r.Cells["ghiChu"].Value = dr["GhiChu"];

                    // Lưu TTCuonDay_ID vào tag của dòng để dùng khi lưu
                    r.Tag = dr["TTCuonDay_ID"];

                    // Cột user: để trống mặc định rồi áp giá trị theo Loai (FIX #1)
                    r.Cells["soCuon_user"].Value = string.Empty;
                    r.Cells["soDau_user"].Value = string.Empty;
                    r.Cells["socuoi_user"].Value = string.Empty;
                    r.Cells["ghiChu_user"].Value = string.Empty;
                    r.Cells["getAll"].Value = false;

                    // Áp dụng readonly + điền giá trị mặc định theo Loai
                    ApplyLoaiReadOnly(r, _loaiNhapKho);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu cuộn/dây:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Áp readonly theo Loai cho một dòng ──────────────────────────────────
        /// <summary>
        /// "Lô"   → soDau_user + socuoi_user có thể nhập; soCuon_user readonly và
        ///           tự điền = soCuon (giá trị gốc).
        /// "Cuộn" → soCuon_user có thể nhập; soDau_user + socuoi_user readonly và
        ///           tự điền = soDau / soCuoi (giá trị gốc).
        /// FIX #1: sau khi đặt readonly thì điền giá trị gốc vào ô bị khoá.
        /// </summary>
        private static void ApplyLoaiReadOnly(DataGridViewRow r, string loai)
        {
            bool isLo = loai == "Lô";
            bool isCuon = loai == "Cuộn";

            // ── readonly ──────────────────────────────────────────────────────────
            r.Cells["soCuon_user"].ReadOnly = isLo;
            r.Cells["soDau_user"].ReadOnly = isCuon;
            r.Cells["socuoi_user"].ReadOnly = isCuon;

            // ── Màu nền để phân biệt ──────────────────────────────────────────────
            Color readonlyColor = Color.FromArgb(230, 230, 230);
            Color normalColor = Color.White;

            r.Cells["soCuon_user"].Style.BackColor = isLo ? readonlyColor : normalColor;
            r.Cells["soDau_user"].Style.BackColor = isCuon ? readonlyColor : normalColor;
            r.Cells["socuoi_user"].Style.BackColor = isCuon ? readonlyColor : normalColor;

            // ── FIX #1: Điền giá trị gốc vào ô readonly ─────────────────────────
            if (isLo)
            {
                // Loại "Lô" → khóa soCuon_user, điền = soCuon gốc
                r.Cells["soCuon_user"].Value = r.Cells["soCuon"].Value;
            }
            else if (isCuon)
            {
                // Loại "Cuộn" → khóa soDau_user + socuoi_user, điền = soDau / soCuoi gốc
                r.Cells["soDau_user"].Value = r.Cells["soDau"].Value;
                r.Cells["socuoi_user"].Value = r.Cells["soCuoi"].Value;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SỰ KIỆN CHECKBOX "LẤY TẤT" trong dgvLayDL
        // ════════════════════════════════════════════════════════════════════════

        // Commit checkbox ngay khi click (không cần click ra ngoài)
        private void DgvLayDL_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvLayDL.IsCurrentCellDirty &&
                dgvLayDL.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvLayDL.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvLayDL_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvLayDL.Columns[e.ColumnIndex].Name != "getAll") return;

            DataGridViewRow r = dgvLayDL.Rows[e.RowIndex];
            bool isChecked = Convert.ToBoolean(r.Cells["getAll"].Value);

            if (isChecked)
            {
                // Copy dữ liệu gốc sang cột user
                r.Cells["soCuon_user"].Value = r.Cells["soCuon"].Value;
                r.Cells["soDau_user"].Value = r.Cells["soDau"].Value;
                r.Cells["socuoi_user"].Value = r.Cells["soCuoi"].Value;

                // Khoá tất cả ô user lại
                r.Cells["soCuon_user"].ReadOnly = true;
                r.Cells["soDau_user"].ReadOnly = true;
                r.Cells["socuoi_user"].ReadOnly = true;

                Color readonlyColor = Color.FromArgb(200, 230, 200); // xanh nhạt = "lấy hết"
                r.Cells["soCuon_user"].Style.BackColor = readonlyColor;
                r.Cells["soDau_user"].Style.BackColor = readonlyColor;
                r.Cells["socuoi_user"].Style.BackColor = readonlyColor;
            }
            else
            {
                // Xoá và mở khoá, rồi áp lại logic theo Loai (kể cả điền giá trị mặc định)
                r.Cells["soCuon_user"].Value = string.Empty;
                r.Cells["soDau_user"].Value = string.Empty;
                r.Cells["socuoi_user"].Value = string.Empty;

                ApplyLoaiReadOnly(r, _loaiNhapKho);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHỈ CHO PHÉP NHẬP SỐ vào soCuon_user, soDau_user, socuoi_user
        // ════════════════════════════════════════════════════════════════════════

        private static readonly string[] _numericUserCols =
            { "soCuon_user", "soDau_user", "socuoi_user" };

        private void DgvLayDL_EditingControlShowing(object sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            string colName = dgvLayDL.CurrentCell?.OwningColumn?.Name ?? string.Empty;

            if (Array.IndexOf(_numericUserCols, colName) >= 0)
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= NumericOnly_KeyPress;
                    tb.KeyPress += NumericOnly_KeyPress;
                }
            }
            else
            {
                if (e.Control is TextBox tb)
                    tb.KeyPress -= NumericOnly_KeyPress;
            }
        }

        private static void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                e.Handled = true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // VALIDATION CHUNG (FIX #2 + #4)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kiểm tra toàn bộ grid trước khi Lưu / Sửa.
        /// FIX #2: soCuon_user &lt;= soCuon; soDau_user &lt;= soDau; soCuoi_user &lt;= soCuoi.
        /// FIX #4: chỉ hiện đúng 1 thông báo lỗi đầu tiên gặp phải.
        /// </summary>
        /// <returns>true = hợp lệ, false = có lỗi (đã hiện MessageBox).</returns>
        private bool ValidateGrid()
        {
            // Kiểm tra tbNguoiLam trước
            if (string.IsNullOrWhiteSpace(tbNguoiLam.Text))
            {
                MessageBox.Show("Vui lòng nhập tên người làm.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbNguoiLam.Focus();
                return false;
            }

            foreach (DataGridViewRow r in dgvLayDL.Rows)
            {
                if (r.Tag == null || r.Tag is DBNull) continue;

                // Lấy giá trị gốc
                int soCuonGoc = ParseInt(r.Cells["soCuon"].Value);
                int soDauGoc = ParseInt(r.Cells["soDau"].Value);
                int soCuoiGoc = ParseInt(r.Cells["soCuoi"].Value);

                // Lấy giá trị người dùng
                string sCuon = r.Cells["soCuon_user"].Value?.ToString() ?? string.Empty;
                string sDau = r.Cells["soDau_user"].Value?.ToString() ?? string.Empty;
                string sCuoi = r.Cells["socuoi_user"].Value?.ToString() ?? string.Empty;

                // Chỉ validate những ô không trống
                if (!string.IsNullOrWhiteSpace(sCuon))
                {
                    if (!int.TryParse(sCuon, out int vCuon) || vCuon > soCuonGoc)
                    {
                        // FIX #4: hiện 1 lỗi rồi dừng
                        MessageBox.Show(
                            $"Số cuộn lấy ({vCuon}) không được lớn hơn số cuộn gốc ({soCuonGoc}).",
                            "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(sDau))
                {
                    if (!int.TryParse(sDau, out int vDau) || vDau > soDauGoc)
                    {
                        MessageBox.Show(
                            $"Số đầu lấy ({vDau}) không được lớn hơn số đầu gốc ({soDauGoc}).",
                            "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(sCuoi))
                {
                    if (!int.TryParse(sCuoi, out int vCuoi) || vCuoi > soCuoiGoc)
                    {
                        MessageBox.Show(
                            $"Số cuối lấy ({vCuoi}) không được lớn hơn số cuối gốc ({soCuoiGoc}).",
                            "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        private static int ParseInt(object val)
        {
            return int.TryParse(val?.ToString(), out int v) ? v : 0;
        }

        // ════════════════════════════════════════════════════════════════════════
        // NÚT LƯU — chèn TTXuatKho cho các dòng có dữ liệu user
        // ════════════════════════════════════════════════════════════════════════

        private void BtnLuuXuatKho_Click(object sender, EventArgs e)
        {
            // FIX #2 + #4: validate tập trung, chỉ 1 lỗi
            if (!ValidateGrid()) return;

            string ngayXuat = dtNgayXuatKho.Value.ToString("yyyy-MM-dd");
            string nguoiLam = tbNguoiLam.Text.Trim();

            int saved = 0;
            bool errorShown = false; // FIX #4: cờ kiểm soát chỉ hiện 1 lỗi

            foreach (DataGridViewRow r in dgvLayDL.Rows)
            {
                if (r.Tag == null || r.Tag is DBNull) continue;
                if (!long.TryParse(r.Tag.ToString(), out long ttCuonDayId)) continue;

                string sCuon = r.Cells["soCuon_user"].Value?.ToString() ?? string.Empty;
                string sDau = r.Cells["soDau_user"].Value?.ToString() ?? string.Empty;
                string sCuoi = r.Cells["socuoi_user"].Value?.ToString() ?? string.Empty;

                bool hasData = !string.IsNullOrWhiteSpace(sCuon)
                            || !string.IsNullOrWhiteSpace(sDau)
                            || !string.IsNullOrWhiteSpace(sCuoi);

                if (!hasData) continue;

                int? soCuon = int.TryParse(sCuon, out int v1) ? v1 : (int?)null;
                int? soDau = int.TryParse(sDau, out int v2) ? v2 : (int?)null;
                int? soCuoi = int.TryParse(sCuoi, out int v3) ? v3 : (int?)null;
                string ghiChu = r.Cells["ghiChu_user"].Value?.ToString() ?? string.Empty;

                try
                {
                    long newId = XuatKho_DB.ThemXuatKho(
                        ttCuonDayId, soCuon, soDau, soCuoi, ghiChu, ngayXuat, nguoiLam);

                    AddRowToPreview(
                        id: newId,
                        tongCD: (soCuon ?? 0) * ((soCuoi ?? 0) - (soDau ?? 0)),
                        ten: tbTenSP.Text,
                        lot: tbLot.Text,
                        soCuonGoc: r.Cells["soCuon"].Value,
                        soDauGoc: r.Cells["soDau"].Value,
                        soCuoiGoc: r.Cells["soCuoi"].Value,
                        ghiChuGoc: r.Cells["ghiChu"].Value,
                        soCuonUser: sCuon,
                        soDauUser: sDau,
                        soCuoiUser: sCuoi,
                        ghiChuUser: ghiChu
                    );

                    saved++;
                }
                catch (Exception ex)
                {
                    // FIX #4: chỉ hiện lỗi đầu tiên
                    if (!errorShown)
                    {
                        MessageBox.Show($"Lỗi khi lưu dòng:\n{ex.Message}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        errorShown = true;
                    }
                }
            }

            if (saved > 0)
            {
                MessageBox.Show($"Đã lưu {saved} dòng xuất kho thành công.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
            }
            else if (!errorShown)
            {
                MessageBox.Show(
                    "Không có dòng nào có dữ liệu để lưu.\n" +
                    "Vui lòng điền ít nhất một cột: Số cuộn lấy, Số đầu lấy hoặc Số cuối lấy.",
                    "Chú ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── Thêm dòng vào dgvLayDL_preview ──────────────────────────────────────
        private void AddRowToPreview(
            long id, int tongCD, string ten, string lot,
            object soCuonGoc, object soDauGoc, object soCuoiGoc, object ghiChuGoc,
            string soCuonUser, string soDauUser, string soCuoiUser, string ghiChuUser)
        {
            int rowIdx = dgvLayDL_preview.Rows.Add();
            DataGridViewRow r = dgvLayDL_preview.Rows[rowIdx];

            r.Cells["id_preview"].Value = id;
            r.Cells["tongCD_preview"].Value = tongCD;
            r.Cells["ten_preview"].Value = ten;
            r.Cells["lot_preview"].Value = lot;
            r.Cells["soCuon_preview"].Value = soCuonGoc;
            r.Cells["soDau_preview"].Value = soDauGoc;
            r.Cells["soCuoi_preview"].Value = soCuoiGoc;
            r.Cells["ghiChu_preview"].Value = ghiChuGoc;
            r.Cells["SoCuon_user_preview"].Value = soCuonUser;
            r.Cells["soDau_user_preview"].Value = soDauUser;
            r.Cells["soCuoi_user_preview"].Value = soCuoiUser;
            r.Cells["ghiChu_user_preview"].Value = ghiChuUser;
        }

        // ════════════════════════════════════════════════════════════════════════
        // DOUBLE CLICK vào dgvLayDL_preview → load ngược về dgvLayDL để sửa/xoá
        // FIX #3: lấy đúng nguồn dữ liệu cho từng nhóm cột
        // ════════════════════════════════════════════════════════════════════════

        private void dgvLayDL_preview_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow previewRow = dgvLayDL_preview.Rows[e.RowIndex];

            // Lấy id bản ghi TTXuatKho
            if (!long.TryParse(previewRow.Cells["id_preview"].Value?.ToString(), out long xuatKhoId))
                return;

            _editingXuatKhoId = xuatKhoId;

            // ── FIX #3: truy vấn dữ liệu gốc từ DB, không dùng lại giá trị trong preview ──
            DataRow nhapKhoRow = null;
            DataRow cuonDayRow = null;
            DataRow xuatKhoRow = null;

            try
            {
                // Lấy thông tin TTNhapKho + TTCuonDay theo xuatKhoId
                DataTable dtSource = XuatKho_DB.LayChiTietXuatKho(xuatKhoId);
                if (dtSource != null && dtSource.Rows.Count > 0)
                {
                    DataRow dr = dtSource.Rows[0];

                    // ── Thông tin chung ──────────────────────────────────────────
                    tbTenSP.Text = previewRow.Cells["ten_preview"].Value?.ToString() ?? string.Empty;
                    tbLot.Text = previewRow.Cells["lot_preview"].Value?.ToString() ?? string.Empty;

                    // ── Xoá grid và tạo 1 dòng sửa ─────────────────────────────
                    dgvLayDL.Rows.Clear();
                    int rowIdx = dgvLayDL.Rows.Add();
                    DataGridViewRow nr = dgvLayDL.Rows[rowIdx];

                    // Cột gốc: từ TTNhapKho / TTCuonDay
                    nr.Cells["tongCD"].Value = dr["TongChieuDai_NK"];  // từ TTNhapKho
                    nr.Cells["soCuon"].Value = dr["SoCuon_CD"];         // từ TTCuonDay
                    nr.Cells["soDau"].Value = dr["SoDau_CD"];          // từ TTCuonDay
                    nr.Cells["soCuoi"].Value = dr["SoCuoi_CD"];         // từ TTCuonDay
                    nr.Cells["ghiChu"].Value = dr["GhiChu_CD"];         // từ TTCuonDay

                    // Cột lấy: từ TTXuatKho — FIX #3: tất cả cột lấy là readonly
                    nr.Cells["soCuon_user"].Value = dr["SoCuon_XK"];    // từ TTXuatKho
                    nr.Cells["soDau_user"].Value = dr["SoDau_XK"];     // từ TTXuatKho
                    nr.Cells["socuoi_user"].Value = dr["SoCuoi_XK"];    // từ TTXuatKho
                    nr.Cells["ghiChu_user"].Value = dr["GhiChu_XK"];    // từ TTXuatKho
                    nr.Cells["getAll"].Value = false;

                    // FIX #3: đặt tất cả cột "_user" thành readonly khi ở chế độ xem từ preview
                    SetAllUserColumnsReadonly(nr);
                }
                else
                {
                    // Fallback: không có dữ liệu DB thì dùng giá trị từ preview row
                    FallbackLoadFromPreviewRow(previewRow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải chi tiết xuất kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Chuyển sang chế độ sửa/xoá
            SetEditMode();
        }

        /// <summary>
        /// FIX #3: Đặt tất cả cột "_user" thành readonly (màu xám nhạt) để hiển thị.
        /// </summary>
        private static void SetAllUserColumnsReadonly(DataGridViewRow r)
        {
            Color readonlyColor = Color.FromArgb(230, 230, 230);

            r.Cells["soCuon_user"].ReadOnly = true;
            r.Cells["soDau_user"].ReadOnly = true;
            r.Cells["socuoi_user"].ReadOnly = true;
            r.Cells["ghiChu_user"].ReadOnly = true;

            r.Cells["soCuon_user"].Style.BackColor = readonlyColor;
            r.Cells["soDau_user"].Style.BackColor = readonlyColor;
            r.Cells["socuoi_user"].Style.BackColor = readonlyColor;
            r.Cells["ghiChu_user"].Style.BackColor = readonlyColor;
        }

        /// <summary>
        /// Fallback khi không lấy được dữ liệu từ DB: dùng giá trị sẵn trong preview row.
        /// </summary>
        private void FallbackLoadFromPreviewRow(DataGridViewRow previewRow)
        {
            tbTenSP.Text = previewRow.Cells["ten_preview"].Value?.ToString() ?? string.Empty;
            tbLot.Text = previewRow.Cells["lot_preview"].Value?.ToString() ?? string.Empty;

            dgvLayDL.Rows.Clear();
            int rowIdx = dgvLayDL.Rows.Add();
            DataGridViewRow nr = dgvLayDL.Rows[rowIdx];

            nr.Cells["tongCD"].Value = previewRow.Cells["tongCD_preview"].Value;
            nr.Cells["soCuon"].Value = previewRow.Cells["soCuon_preview"].Value;
            nr.Cells["soDau"].Value = previewRow.Cells["soDau_preview"].Value;
            nr.Cells["soCuoi"].Value = previewRow.Cells["soCuoi_preview"].Value;
            nr.Cells["ghiChu"].Value = previewRow.Cells["ghiChu_preview"].Value;

            nr.Cells["soCuon_user"].Value = previewRow.Cells["SoCuon_user_preview"].Value;
            nr.Cells["soDau_user"].Value = previewRow.Cells["soDau_user_preview"].Value;
            nr.Cells["socuoi_user"].Value = previewRow.Cells["soCuoi_user_preview"].Value;
            nr.Cells["ghiChu_user"].Value = previewRow.Cells["ghiChu_user_preview"].Value;
            nr.Cells["getAll"].Value = false;

            SetAllUserColumnsReadonly(nr);
        }

        // ════════════════════════════════════════════════════════════════════════
        // NÚT SỬA
        // ════════════════════════════════════════════════════════════════════════

        private void BtnSua_Click(object sender, EventArgs e)
        {
            if (!_editingXuatKhoId.HasValue || dgvLayDL.Rows.Count == 0) return;

            // FIX #2 + #4: validate tập trung trước khi lưu
            if (!ValidateGrid()) return;

            DataGridViewRow r = dgvLayDL.Rows[0];

            string sCuon = r.Cells["soCuon_user"].Value?.ToString() ?? string.Empty;
            string sDau = r.Cells["soDau_user"].Value?.ToString() ?? string.Empty;
            string sCuoi = r.Cells["socuoi_user"].Value?.ToString() ?? string.Empty;
            string ghiChu = r.Cells["ghiChu_user"].Value?.ToString() ?? string.Empty;

            int? soCuon = int.TryParse(sCuon, out int v1) ? v1 : (int?)null;
            int? soDau = int.TryParse(sDau, out int v2) ? v2 : (int?)null;
            int? soCuoi = int.TryParse(sCuoi, out int v3) ? v3 : (int?)null;

            string ngayXuat = dtNgayXuatKho.Value.ToString("yyyy-MM-dd");
            string nguoiLam = tbNguoiLam.Text.Trim();

            try
            {
                XuatKho_DB.SuaXuatKho(
                    _editingXuatKhoId.Value,
                    soCuon, soDau, soCuoi, ghiChu, ngayXuat, nguoiLam);

                UpdatePreviewRow(_editingXuatKhoId.Value,
                    (soCuon ?? 0) * ((soCuoi ?? 0) - (soDau ?? 0)),
                    sCuon, sDau, sCuoi, ghiChu);

                MessageBox.Show("Đã cập nhật thành công.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetForm();
            }
            catch (Exception ex)
            {
                // FIX #4: chỉ 1 thông báo lỗi
                MessageBox.Show($"Lỗi khi cập nhật:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Cập nhật dòng trong dgvLayDL_preview sau khi sửa ────────────────────
        private void UpdatePreviewRow(long id, int tongCD,
            string soCuon, string soDau, string soCuoi, string ghiChu)
        {
            foreach (DataGridViewRow r in dgvLayDL_preview.Rows)
            {
                if (long.TryParse(r.Cells["id_preview"].Value?.ToString(), out long rowId)
                    && rowId == id)
                {
                    r.Cells["tongCD_preview"].Value = tongCD;
                    r.Cells["SoCuon_user_preview"].Value = soCuon;
                    r.Cells["soDau_user_preview"].Value = soDau;
                    r.Cells["soCuoi_user_preview"].Value = soCuoi;
                    r.Cells["ghiChu_user_preview"].Value = ghiChu;
                    break;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // NÚT XOÁ
        // ════════════════════════════════════════════════════════════════════════

        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (!_editingXuatKhoId.HasValue) return;

            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xoá bản ghi xuất kho này không?",
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                XuatKho_DB.XoaXuatKho(_editingXuatKhoId.Value);
                RemovePreviewRow(_editingXuatKhoId.Value);

                MessageBox.Show("Đã xoá thành công.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetForm();
            }
            catch (Exception ex)
            {
                // FIX #4: chỉ 1 thông báo lỗi
                MessageBox.Show($"Lỗi khi xoá:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemovePreviewRow(long id)
        {
            foreach (DataGridViewRow r in dgvLayDL_preview.Rows)
            {
                if (long.TryParse(r.Cells["id_preview"].Value?.ToString(), out long rowId)
                    && rowId == id)
                {
                    dgvLayDL_preview.Rows.Remove(r);
                    break;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM TRONG tbTimKiem (nhấn Enter)
        // ════════════════════════════════════════════════════════════════════════

        private void TbTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            string keyword = tbTimKiem.Text.Trim();
            if (string.IsNullOrEmpty(keyword)) return;

            try
            {
                DataTable dt = XuatKho_DB.TimKiemXuatKho(keyword);

                dgvLayDL_preview.Rows.Clear();

                foreach (DataRow dr in dt.Rows)
                {
                    int rowIdx = dgvLayDL_preview.Rows.Add();
                    DataGridViewRow r = dgvLayDL_preview.Rows[rowIdx];

                    r.Cells["id_preview"].Value = dr["id_preview"];
                    r.Cells["tongCD_preview"].Value = dr["tongCD_preview"];
                    r.Cells["ten_preview"].Value = dr["ten_preview"];
                    r.Cells["lot_preview"].Value = dr["lot_preview"];
                    r.Cells["soCuon_preview"].Value = dr["soCuon_preview"];
                    r.Cells["soDau_preview"].Value = dr["soDau_preview"];
                    r.Cells["soCuoi_preview"].Value = dr["soCuoi_preview"];
                    r.Cells["ghiChu_preview"].Value = dr["ghiChu_preview"];
                    r.Cells["SoCuon_user_preview"].Value = dr["SoCuon_user_preview"];
                    r.Cells["soDau_user_preview"].Value = dr["soDau_user_preview"];
                    r.Cells["soCuoi_user_preview"].Value = dr["soCuoi_user_preview"];
                    r.Cells["ghiChu_user_preview"].Value = dr["ghiChu_user_preview"];
                }

                SetSearchResultMode();
            }
            catch (Exception ex)
            {
                // FIX #4: chỉ 1 thông báo lỗi
                MessageBox.Show($"Lỗi khi tìm kiếm:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // QUẢN LÝ TRẠNG THÁI NÚT
        // ════════════════════════════════════════════════════════════════════════

        private void SetNewMode()
        {
            _editingXuatKhoId = null;
            btnLuuXuatKho.Enabled = true;
            btnSua.Enabled = false;
            btnXoa.Enabled = false;
        }

        private void SetEditMode()
        {
            btnLuuXuatKho.Enabled = false;
            btnSua.Enabled = true;
            btnXoa.Enabled = true;
        }

        private void SetSearchResultMode()
        {
            btnLuuXuatKho.Enabled = false;
            btnSua.Enabled = true;
            btnXoa.Enabled = true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // RESET FORM
        // ════════════════════════════════════════════════════════════════════════

        private void ResetForm()
        {
            _lotSearchHelper.Reset();
            dtNgayXuatKho.Value = DateTime.Today;
            SetNewMode();
        }

        // ════════════════════════════════════════════════════════════════════════
        // FONT GRID
        // ════════════════════════════════════════════════════════════════════════

        private void InitGridFont()
        {
            Font gridFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            Font headerFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);

            // dgvLayDL
            dgvLayDL.Font = gridFont;
            dgvLayDL.DefaultCellStyle.Font = gridFont;
            dgvLayDL.RowsDefaultCellStyle.Font = gridFont;
            dgvLayDL.AlternatingRowsDefaultCellStyle.Font = gridFont;
            dgvLayDL.EnableHeadersVisualStyles = false;
            dgvLayDL.ColumnHeadersDefaultCellStyle.Font = headerFont;
            dgvLayDL.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            dgvLayDL.RowTemplate.Height = 35;
            dgvLayDL.ColumnHeadersHeight = 38;
            dgvLayDL.Columns["ttNhapKho_ID"].Visible = false;

            // dgvLayDL_preview
            dgvLayDL_preview.Font = gridFont;
            dgvLayDL_preview.DefaultCellStyle.Font = gridFont;
            dgvLayDL_preview.EnableHeadersVisualStyles = false;
            dgvLayDL_preview.ColumnHeadersDefaultCellStyle.Font = headerFont;
            dgvLayDL_preview.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            dgvLayDL_preview.RowTemplate.Height = 35;
            dgvLayDL_preview.ColumnHeadersHeight = 38;
            dgvLayDL_preview.Columns["id_preview"].Visible = false;
        }

        // ════════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ════════════════════════════════════════════════════════════════════════

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _lotSearchHelper?.Dispose();
            base.OnHandleDestroyed(e);
        }
    }
}