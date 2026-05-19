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


        private bool _isLoadingPreviewToEdit = false;

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
                DataTable dt = XuatKho_DB.LayDuLieuTonKhoCuonDay(ttThanhPhamId);

                dgvLayDL.Rows.Clear();

                foreach (DataRow dr in dt.Rows)
                {
                    int rowIdx = dgvLayDL.Rows.Add();
                    DataGridViewRow r = dgvLayDL.Rows[rowIdx];

                    // Dữ liệu tồn thực tế (readonly) — đã trừ TTXuatKho
                    r.Cells["ttNhapKho_ID"].Value = dr["TTNhapKho_ID"];
                    r.Cells["tongCD"].Value = dr["TongChieuDai"];
                    r.Cells["soCuon"].Value = dr["SoCuon"];
                    r.Cells["soDau"].Value = dr["SoDau"];
                    r.Cells["soCuoi"].Value = dr["soCuoi"];
                    r.Cells["ghiChu"].Value = dr["GhiChu"];

                    // Lưu TTCuonDay_ID vào tag của dòng để dùng khi lưu
                    r.Tag = dr["TTCuonDay_ID"];

                    // Cột user: để trống mặc định rồi áp giá trị theo Loai (FIX #1)
                    r.Cells["soCuon_user"].Value = string.Empty;
                    r.Cells["soDau_user"].Value = string.Empty;
                    r.Cells["soCuoi_user"].Value = string.Empty;
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
        /// Áp dụng trạng thái nhập theo Loai dựa trên dữ liệu tồn thực tế:
        /// - "Lô"   → soCuon_user readonly = 1, soCuoi_user readonly = SoCuoi tồn,
        ///             người dùng chỉ nhập SoDau xuất.
        /// - "Cuộn" → người dùng nhập SoCuon xuất, SoDau/SoCuoi readonly theo thông tin cuộn tồn.
        /// </summary>
        private static void ApplyLoaiReadOnly(DataGridViewRow r, string loai)
        {
            bool isLo = loai == "Lô";
            bool isCuon = loai == "Cuộn";

            Color readonlyColor = Color.FromArgb(230, 230, 230);
            Color normalColor = Color.White;

            if (isLo)
            {
                r.Cells["soCuon_user"].ReadOnly = true;
                r.Cells["soDau_user"].ReadOnly = false;
                r.Cells["soCuoi_user"].ReadOnly = true;

                // Lô luôn xem là 1 đơn vị. Số cuối xuất bị khóa bằng số cuối tồn.
                r.Cells["soCuon_user"].Value = r.Cells["soCuon"].Value;
                r.Cells["soCuoi_user"].Value = r.Cells["soCuoi"].Value;
            }
            else if (isCuon)
            {
                r.Cells["soCuon_user"].ReadOnly = false;
                r.Cells["soDau_user"].ReadOnly = true;
                r.Cells["soCuoi_user"].ReadOnly = true;

                // Cuộn xuất nguyên cuộn nên SoDau/SoCuoi bị khóa theo thông tin cuộn tồn.
                r.Cells["soDau_user"].Value = r.Cells["soDau"].Value;
                r.Cells["soCuoi_user"].Value = r.Cells["soCuoi"].Value;
            }
            else
            {
                r.Cells["soCuon_user"].ReadOnly = false;
                r.Cells["soDau_user"].ReadOnly = false;
                r.Cells["soCuoi_user"].ReadOnly = false;
            }

            r.Cells["soCuon_user"].Style.BackColor =
                r.Cells["soCuon_user"].ReadOnly ? readonlyColor : normalColor;
            r.Cells["soDau_user"].Style.BackColor =
                r.Cells["soDau_user"].ReadOnly ? readonlyColor : normalColor;
            r.Cells["soCuoi_user"].Style.BackColor =
                r.Cells["soCuoi_user"].ReadOnly ? readonlyColor : normalColor;
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
            if (_isLoadingPreviewToEdit) return;

            if (e.RowIndex < 0) return;
            if (dgvLayDL.Columns[e.ColumnIndex].Name != "getAll") return;

            DataGridViewRow r = dgvLayDL.Rows[e.RowIndex];
            bool isChecked = Convert.ToBoolean(r.Cells["getAll"].Value);

            if (isChecked)
            {
                r.Cells["soCuon_user"].Value = r.Cells["soCuon"].Value;
                r.Cells["soDau_user"].Value = r.Cells["soDau"].Value;
                r.Cells["soCuoi_user"].Value = r.Cells["soCuoi"].Value;

                r.Cells["soCuon_user"].ReadOnly = true;
                r.Cells["soDau_user"].ReadOnly = true;
                r.Cells["soCuoi_user"].ReadOnly = true;

                Color readonlyColor = Color.FromArgb(200, 230, 200);
                r.Cells["soCuon_user"].Style.BackColor = readonlyColor;
                r.Cells["soDau_user"].Style.BackColor = readonlyColor;
                r.Cells["soCuoi_user"].Style.BackColor = readonlyColor;
            }
            else
            {
                r.Cells["soCuon_user"].Value = string.Empty;
                r.Cells["soDau_user"].Value = string.Empty;
                r.Cells["soCuoi_user"].Value = string.Empty;

                ApplyLoaiReadOnly(r, _loaiNhapKho);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // VALIDATION DỮ LIỆU USER
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Các cột số cho phép người dùng nhập tạm thời bất kỳ giá trị nào.
        /// Khi bấm Lưu/Sửa mới kiểm tra để tránh lưu dữ liệu không hợp lệ.
        /// </summary>
        private static readonly string[] _numericUserCols =
            { "soCuon_user", "soDau_user", "soCuoi_user" };

        private void DgvLayDL_EditingControlShowing(object sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            // Giữ lại method để không phá các wiring cũ nếu Designer có gắn event.
            // Theo yêu cầu mới: cho nhập tạm thời, validate khi bấm Lưu/Sửa.
        }

        private static void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Không dùng chặn phím nữa vì người dùng được phép nhập tạm thời.
            // Dữ liệu không phải số nguyên dương sẽ bị bỏ qua khi Lưu hoặc báo lỗi khi Sửa.
        }

        /// <summary>
        /// Kiểm tra thông tin chung bắt buộc trước khi Lưu/Sửa.
        /// </summary>
        private bool ValidateRequiredInfo()
        {
            if (string.IsNullOrWhiteSpace(tbNguoiLam.Text))
            {
                MessageBox.Show("Vui lòng nhập tên người làm.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbNguoiLam.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Dùng cho nút Sửa: nếu dòng đang sửa không hợp lệ thì báo lỗi và không cập nhật.
        /// </summary>
        private bool ValidateGrid()
        {
            if (!ValidateRequiredInfo()) return false;

            foreach (DataGridViewRow r in dgvLayDL.Rows)
            {
                if (r.IsNewRow) continue;

                if (!TryReadAndValidateRow(
                        r,
                        _loaiNhapKho,
                        showMessage: true,
                        out _, out _, out _))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryReadPositiveInt(DataGridViewRow r, string columnName, out int value)
        {
            value = 0;
            string text = r.Cells[columnName].Value?.ToString().Trim() ?? string.Empty;
            return int.TryParse(text, out value) && value > 0;
        }

        private static bool TryReadNonNegativeInt(DataGridViewRow r, string columnName, out int value)
        {
            value = 0;
            string text = r.Cells[columnName].Value?.ToString().Trim() ?? string.Empty;
            return int.TryParse(text, out value) && value >= 0;
        }

        /// <summary>
        /// Dùng cho nút Lưu: không báo lỗi từng dòng, chỉ trả về hợp lệ/không hợp lệ.
        /// Dữ liệu giới hạn theo tồn thực tế đang hiển thị trên grid.
        /// </summary>
        private bool TryGetValidSaveValues(
            DataGridViewRow r,
            out int soCuonUser,
            out int soDauUser,
            out int soCuoiUser)
        {
            return TryReadAndValidateRow(
                r,
                _loaiNhapKho,
                showMessage: false,
                out soCuonUser,
                out soDauUser,
                out soCuoiUser);
        }

        private static bool TryReadAndValidateRow(
            DataGridViewRow r,
            string loai,
            bool showMessage,
            out int soCuonUser,
            out int soDauUser,
            out int soCuoiUser)
        {
            soCuonUser = 0;
            soDauUser = 0;
            soCuoiUser = 0;

            int rowNumber = r.Index + 1;

            if (!TryReadPositiveInt(r, "soCuon_user", out soCuonUser))
            {
                ShowValidationMessage(showMessage,
                    $"Dòng {rowNumber}: Số cuộn lấy phải là số nguyên lớn hơn 0.");
                return false;
            }

            if (!TryReadNonNegativeInt(r, "soDau_user", out soDauUser))
            {
                ShowValidationMessage(showMessage,
                    $"Dòng {rowNumber}: Số đầu lấy phải là số nguyên lớn hơn hoặc bằng 0.");
                return false;
            }

            if (!TryReadPositiveInt(r, "soCuoi_user", out soCuoiUser))
            {
                ShowValidationMessage(showMessage,
                    $"Dòng {rowNumber}: Số cuối lấy phải là số nguyên lớn hơn 0.");
                return false;
            }

            int soCuonTon = ParseInt(r.Cells["soCuon"].Value);
            int soDauTon = ParseInt(r.Cells["soDau"].Value);
            int soCuoiTon = ParseInt(r.Cells["soCuoi"].Value);

            if (loai == "Lô")
            {
                if (soCuonUser != 1)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Loại Lô luôn phải có số cuộn lấy bằng 1.");
                    return false;
                }

                if (soCuoiUser != soCuoiTon)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Loại Lô phải xuất đến đúng số cuối tồn ({soCuoiTon}).");
                    return false;
                }

                if (soDauUser < soDauTon)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số đầu lấy ({soDauUser}) không được nhỏ hơn số đầu tồn ({soDauTon}).");
                    return false;
                }

                if (soDauUser >= soCuoiUser)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số đầu lấy ({soDauUser}) phải nhỏ hơn số cuối lấy ({soCuoiUser}).");
                    return false;
                }
            }
            else if (loai == "Cuộn")
            {
                if (soCuonUser > soCuonTon)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số cuộn lấy ({soCuonUser}) không được lớn hơn số cuộn tồn ({soCuonTon}).");
                    return false;
                }

                if (soDauUser != soDauTon || soCuoiUser != soCuoiTon)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Loại Cuộn phải xuất nguyên cuộn theo đúng đoạn tồn ({soDauTon}–{soCuoiTon}).");
                    return false;
                }

                if (soDauUser >= soCuoiUser)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số đầu lấy ({soDauUser}) phải nhỏ hơn số cuối lấy ({soCuoiUser}).");
                    return false;
                }
            }
            else
            {
                if (soCuonUser > soCuonTon)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số cuộn lấy ({soCuonUser}) không được lớn hơn số cuộn tồn ({soCuonTon}).");
                    return false;
                }

                if (soDauUser >= soCuoiUser)
                {
                    ShowValidationMessage(showMessage,
                        $"Dòng {rowNumber}: Số đầu lấy ({soDauUser}) phải nhỏ hơn số cuối lấy ({soCuoiUser}).");
                    return false;
                }
            }

            return true;
        }

        private static void ShowValidationMessage(bool showMessage, string message)
        {
            if (!showMessage) return;

            MessageBox.Show(message,
                "Giá trị không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            dgvLayDL.EndEdit();

            if (!ValidateRequiredInfo()) return;

            string ngayXuat = dtNgayXuatKho.Value.ToString("yyyy-MM-dd");
            string nguoiLam = tbNguoiLam.Text.Trim();

            int saved = 0;
            int skipped = 0;
            string firstDbError = null;

            foreach (DataGridViewRow r in dgvLayDL.Rows)
            {
                if (r.IsNewRow) continue;

                if (r.Tag == null || r.Tag is DBNull ||
                    !long.TryParse(r.Tag.ToString(), out long ttCuonDayId))
                {
                    skipped++;
                    continue;
                }

                if (!TryGetValidSaveValues(r,
                        out int soCuon,
                        out int soDau,
                        out int soCuoi))
                {
                    skipped++;
                    continue;
                }

                string ghiChu = r.Cells["ghiChu_user"].Value?.ToString() ?? string.Empty;

                try
                {
                    long newId = XuatKho_DB.ThemXuatKho(
                        ttCuonDayId, soCuon, soDau, soCuoi, ghiChu, ngayXuat, nguoiLam);

                    AddRowToPreview(
                        id: newId,
                        tongCD: soCuon * (soCuoi - soDau),
                        ten: tbTenSP.Text,
                        lot: tbLot.Text,
                        soCuonGoc: r.Cells["soCuon"].Value,
                        soDauGoc: r.Cells["soDau"].Value,
                        soCuoiGoc: r.Cells["soCuoi"].Value,
                        ghiChuGoc: r.Cells["ghiChu"].Value,
                        soCuonUser: soCuon.ToString(),
                        soDauUser: soDau.ToString(),
                        soCuoiUser: soCuoi.ToString(),
                        ghiChuUser: ghiChu
                    );

                    saved++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    if (firstDbError == null)
                        firstDbError = ex.Message;
                }
            }

            if (saved > 0)
            {
                string message = $"Đã lưu {saved} dòng xuất kho thành công.";

                if (skipped > 0)
                    message += $"\nCó {skipped} dòng không được thêm.";

                if (!string.IsNullOrWhiteSpace(firstDbError))
                    message += $"\nLỗi đầu tiên khi lưu: {firstDbError}";

                MessageBox.Show(message,
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetForm();
            }
            else
            {
                string message = skipped > 0
                    ? $"Không có dòng nào được lưu.\nCó {skipped} dòng không được thêm do không hợp lệ."
                    : "Không có dòng nào để lưu.";

                if (!string.IsNullOrWhiteSpace(firstDbError))
                    message += $"\nLỗi đầu tiên khi lưu: {firstDbError}";

                MessageBox.Show(message,
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

            if (!long.TryParse(previewRow.Cells["id_preview"].Value?.ToString(), out long xuatKhoId))
                return;

            _editingXuatKhoId = xuatKhoId;

            try
            {
                DataTable dtSource = XuatKho_DB.LayChiTietXuatKho(xuatKhoId);
                if (dtSource != null && dtSource.Rows.Count > 0)
                {
                    DataRow dr = dtSource.Rows[0];

                    _isLoadingPreviewToEdit = true;

                    try
                    {
                        _loaiNhapKho = dr["Loai"]?.ToString() ?? string.Empty;

                        bool isLoMoiNhat = !dtSource.Columns.Contains("IsLoMoiNhat")
                            || ParseInt(dr["IsLoMoiNhat"]) == 1;

                        if (_loaiNhapKho == "Lô" && !isLoMoiNhat)
                        {
                            MessageBox.Show(
                                "Phiếu xuất loại Lô này không phải lần xuất mới nhất của lô nên không được sửa trực tiếp.\n" +
                                "Vui lòng sửa/xoá các lần xuất sau trước, hoặc tạo nghiệp vụ điều chỉnh.",
                                "Không thể sửa", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            _editingXuatKhoId = null;
                            return;
                        }

                        tbTenSP.Text = previewRow.Cells["ten_preview"].Value?.ToString() ?? string.Empty;
                        tbLot.Text = previewRow.Cells["lot_preview"].Value?.ToString() ?? string.Empty;

                        tbNguoiLam.Text = dr["NguoiLam"]?.ToString() ?? string.Empty;

                        // Điền ngày xuất từ TTXuatKho
                        if (DateTime.TryParse(dr["NgayXuat"]?.ToString(), out DateTime ngayXuat))
                        {
                            dtNgayXuatKho.Value = ngayXuat;
                        }

                        // Lấy Loai trước khi xử lý grid để tránh dùng _loaiNhapKho cũ
                        _loaiNhapKho = dr["Loai"]?.ToString() ?? string.Empty;

                        dgvLayDL.Rows.Clear();

                        int rowIdx = dgvLayDL.Rows.Add();
                        DataGridViewRow nr = dgvLayDL.Rows[rowIdx];

                        nr.Cells["getAll"].Value = false;

                        if (dtSource.Columns.Contains("TTNhapKho_ID"))
                            nr.Cells["ttNhapKho_ID"].Value = dr["TTNhapKho_ID"];

                        if (dtSource.Columns.Contains("TTCuonDay_ID"))
                            nr.Tag = dr["TTCuonDay_ID"];

                        // Cột tồn khả dụng khi sửa = tồn hiện tại + lượng của phiếu đang sửa
                        nr.Cells["tongCD"].Value = dr["TongChieuDai_NK"];
                        nr.Cells["soCuon"].Value = dr["SoCuon_CD"];
                        nr.Cells["soDau"].Value = dr["SoDau_CD"];
                        nr.Cells["soCuoi"].Value = dr["soCuoi_CD"];
                        nr.Cells["ghiChu"].Value = dr["GhiChu_CD"];

                        // Cột lấy từ TTXuatKho
                        nr.Cells["soCuon_user"].Value = dr["SoCuon_XK"];
                        nr.Cells["soDau_user"].Value = dr["SoDau_XK"];
                        nr.Cells["soCuoi_user"].Value = dr["soCuoi_XK"];
                        nr.Cells["ghiChu_user"].Value = dr["GhiChu_XK"];

                        ApplyPreviewEditReadOnly(nr, _loaiNhapKho);
                    }
                    finally
                    {
                        _isLoadingPreviewToEdit = false;
                    }
                }
                else
                {
                    FallbackLoadFromPreviewRow(previewRow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải chi tiết xuất kho:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetEditMode();
        }

        /// <summary>
        /// Double click từ preview: khoá cột "Lấy tất", còn các cột lấy mở/khoá theo Loai.
        /// Không tự ghi đè giá trị user vì các giá trị này đang lấy từ TTXuatKho.
        /// </summary>
        private static void ApplyPreviewEditReadOnly(DataGridViewRow r, string loai)
        {
            bool isLo = loai == "Lô";
            bool isCuon = loai == "Cuộn";

            Color readonlyColor = Color.FromArgb(230, 230, 230);
            Color normalColor = Color.White;

            // Cột Lấy tất luôn bị khoá khi load dữ liệu từ preview lên để sửa
            if (r.DataGridView != null && r.DataGridView.Columns.Contains("getAll"))
                r.DataGridView.Columns["getAll"].ReadOnly = true;

            r.Cells["getAll"].ReadOnly = true;
            r.Cells["getAll"].Style.BackColor = readonlyColor;

            // Ghi chú xuất kho vẫn cho sửa
            r.Cells["ghiChu_user"].ReadOnly = false;
            r.Cells["ghiChu_user"].Style.BackColor = normalColor;

            if (isLo)
            {
                // TTNhapKho.Loai = "Lô": chỉ cho sửa số đầu; số cuối phải bằng số cuối tồn.
                r.Cells["soCuon_user"].ReadOnly = true;
                r.Cells["soDau_user"].ReadOnly = false;
                r.Cells["soCuoi_user"].ReadOnly = true;
            }
            else if (isCuon)
            {
                // TTNhapKho.Loai = "Cuộn"
                r.Cells["soCuon_user"].ReadOnly = false;
                r.Cells["soDau_user"].ReadOnly = true;
                r.Cells["soCuoi_user"].ReadOnly = true;
            }
            else
            {
                // Không xác định Loai thì mở các ô số để tránh khoá nhầm
                r.Cells["soCuon_user"].ReadOnly = false;
                r.Cells["soDau_user"].ReadOnly = false;
                r.Cells["soCuoi_user"].ReadOnly = false;
            }

            r.Cells["soCuon_user"].Style.BackColor =
                r.Cells["soCuon_user"].ReadOnly ? readonlyColor : normalColor;
            r.Cells["soDau_user"].Style.BackColor =
                r.Cells["soDau_user"].ReadOnly ? readonlyColor : normalColor;
            r.Cells["soCuoi_user"].Style.BackColor =
                r.Cells["soCuoi_user"].ReadOnly ? readonlyColor : normalColor;
        }

        /// <summary>
        /// Fallback khi không lấy được dữ liệu từ DB: dùng giá trị sẵn trong preview row.
        /// </summary>
        private void FallbackLoadFromPreviewRow(DataGridViewRow previewRow)
        {
            _isLoadingPreviewToEdit = true;

            try
            {
                tbTenSP.Text = previewRow.Cells["ten_preview"].Value?.ToString() ?? string.Empty;
                tbLot.Text = previewRow.Cells["lot_preview"].Value?.ToString() ?? string.Empty;

                dgvLayDL.Rows.Clear();

                int rowIdx = dgvLayDL.Rows.Add();
                DataGridViewRow nr = dgvLayDL.Rows[rowIdx];

                nr.Cells["getAll"].Value = false;

                nr.Cells["tongCD"].Value = previewRow.Cells["tongCD_preview"].Value;
                nr.Cells["soCuon"].Value = previewRow.Cells["soCuon_preview"].Value;
                nr.Cells["soDau"].Value = previewRow.Cells["soDau_preview"].Value;
                nr.Cells["soCuoi"].Value = previewRow.Cells["soCuoi_preview"].Value;
                nr.Cells["ghiChu"].Value = previewRow.Cells["ghiChu_preview"].Value;

                nr.Cells["soCuon_user"].Value = previewRow.Cells["SoCuon_user_preview"].Value;
                nr.Cells["soDau_user"].Value = previewRow.Cells["soDau_user_preview"].Value;
                nr.Cells["soCuoi_user"].Value = previewRow.Cells["soCuoi_user_preview"].Value;
                nr.Cells["ghiChu_user"].Value = previewRow.Cells["ghiChu_user_preview"].Value;

                ApplyPreviewEditReadOnly(nr, _loaiNhapKho);
            }
            finally
            {
                _isLoadingPreviewToEdit = false;
            }
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
            string sCuoi = r.Cells["soCuoi_user"].Value?.ToString() ?? string.Empty;
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

            // Khi quay về nhập mới, cột "Lấy tất" phải dùng được lại.
            if (dgvLayDL.Columns.Contains("getAll"))
                dgvLayDL.Columns["getAll"].ReadOnly = false;
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