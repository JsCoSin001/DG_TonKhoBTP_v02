using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class Frm_BoSungThemDuLieu : Form
    {
        // ── State ────────────────────────────────────────────────────────────────
        private ComboBoxSearchHelper _tenSPSearchHelper;
        private ComboBoxSearchHelper _tenThanhPhamSearchHelper;
        private ComboBoxSearchHelper _tenNLSearchHelper;

        private readonly Dictionary<string, int> _congDoanDisplayToId =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Id bản ghi TTBoSung đang được xem/sửa. 0 = chế độ thêm mới.</summary>
        private long _currentBoSungId = 0;

        /// <summary>Id của DanhSachMaSP đã chọn qua cbxTimTen.</summary>
        private long _selectedMaSP_ID = 0;

        /// <summary>Id BOMStructure đang được sửa. 0 = chế độ thêm mới BOM.</summary>
        private long _currentBomId = 0;

        /// <summary>Active cũ của BOM đang sửa, dùng để hỏi xác nhận khi kích hoạt lại.</summary>
        private int _currentBomOldActive = 0;

        /// <summary>Dòng grid tương ứng BOM đang sửa.</summary>
        private DataGridViewRow _currentBomGridRow = null;

        /// <summary>DataTable đang bind vào grv để insert/update/delete trực tiếp không reload.</summary>
        private DataTable _bomTable;

        // ════════════════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ════════════════════════════════════════════════════════════════════════

        public Frm_BoSungThemDuLieu()
        {
            InitializeComponent();
            InitTimTenSearch();
            InitBomSearch();

            btnLuuBom.Click += BtnLuuBom_Click;
            grv.CellContentClick += Grv_CellContentClick;
            grv.CellDoubleClick += Grv_CellDoubleClick;
            grv.DataBindingComplete += Grv_DataBindingComplete;

            this.Load += Frm_Load;
        }

        private void Frm_Load(object sender, EventArgs e)
        {
            SetEditMode(false);
            InitBomControls();
            cbxTimTen.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        // COMBOBOX TÌM KIẾM TÊN SP (cbxTimTen)
        // ════════════════════════════════════════════════════════════════════════

        private void InitTimTenSearch()
        {
            _tenSPSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimTen,
                queryFunc: BoSungThongTinSP_DB.TimKiemTenSPAsync
            );
            _tenSPSearchHelper.DisplayColumn = "Ten";
            _tenSPSearchHelper.ItemSelected += OnTenSPSelected;
            _tenSPSearchHelper.Cleared += OnTenSPCleared;
        }

        /// <summary>Người dùng chọn một sản phẩm từ dropdown cbxTimTen.</summary>
        private void OnTenSPSelected(DataRowView row)
        {
            // Lấy dữ liệu từ row vào biến local trước khi Reset() làm mất row
            long spId = long.TryParse(row["id"]?.ToString(), out long parsedId) ? parsedId : 0;
            string ma = row["Ma"]?.ToString() ?? string.Empty;
            string ten = row["Ten"]?.ToString() ?? string.Empty;

            // Lưu state
            _selectedMaSP_ID = spId;
            _currentBoSungId = 0;

            // Điền tbID, tbMa, tbTen TRƯỚC
            tbID.Text = spId > 0 ? spId.ToString() : string.Empty;
            tbMa.Text = ma;
            tbTen.Text = ten;

            // Xoá cbxTimTen qua BeginInvoke để không xung đột với event đang chạy.
            // Reset() sẽ gọi Cleared → OnTenSPCleared, nên cần tạm tắt rồi bật lại.
            BeginInvoke(new Action(() =>
            {
                _tenSPSearchHelper.Cleared -= OnTenSPCleared;
                _tenSPSearchHelper.Reset();
                _tenSPSearchHelper.Cleared += OnTenSPCleared;
            }));

            ClearDataControls();
            SetEditMode(false);

            // Bôi đen toàn bộ nội dung tbTenChiTiet và focus
            tbTenChiTiet.SelectAll();
            tbTenChiTiet.Focus();
        }

        private void OnTenSPCleared()
        {
            _selectedMaSP_ID = 0;
            tbID.Text = string.Empty;
            tbMa.Text = string.Empty;
            tbTen.Text = string.Empty;

            ClearDataControls();
            _currentBoSungId = 0;
            SetEditMode(false);
        }


        // ════════════════════════════════════════════════════════════════════════
        // BOM STRUCTURE — INIT / SEARCH
        // ════════════════════════════════════════════════════════════════════════

        private void InitBomSearch()
        {
            _tenThanhPhamSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTen_ThanhPham,
                queryFunc: BOMStructure_DB.TimKiemThanhPhamAsync
            );
            _tenThanhPhamSearchHelper.DisplayColumn = "Ten";
            _tenThanhPhamSearchHelper.ItemSelected += OnBomThanhPhamSelected;
            _tenThanhPhamSearchHelper.Cleared += OnBomThanhPhamCleared;

            _tenNLSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTen_NL,
                queryFunc: BOMStructure_DB.TimKiemNguyenLieuAsync
            );
            _tenNLSearchHelper.DisplayColumn = "Ten";
            _tenNLSearchHelper.ItemSelected += OnBomNguyenLieuSelected;
            _tenNLSearchHelper.Cleared += OnBomNguyenLieuCleared;
        }

        private void InitBomControls()
        {
            grv.AutoGenerateColumns = false;
            grv.RowTemplate.Height = 35;
            foreach (DataGridViewRow row in grv.Rows)
                row.Height = 35;

            if (grv.Columns["colDelete"] is DataGridViewButtonColumn deleteColumn)
            {
                deleteColumn.Text = "Xóa";
                deleteColumn.UseColumnTextForButtonValue = true;
            }

            cbxCongDoan.Items.Clear();
            foreach (var congDoan in ThongTinChungCongDoan.TatCaCongDoan.Where(x => x.Id != 2))
                cbxCongDoan.Items.Add(congDoan.TenCongDoan);

            cbxActive.SelectedIndex = 0;

            _bomTable = BOMStructure_DB.GetByParentProduct(-1);
            grv.DataSource = _bomTable;
        }

        private void OnBomThanhPhamSelected(DataRowView row)
        {
            long spId = long.TryParse(row["id"]?.ToString(), out long parsedId) ? parsedId : 0;
            string ma = row["Ma"]?.ToString() ?? string.Empty;
            string ten = row["Ten"]?.ToString() ?? string.Empty;

            tbID_TP.Text = spId > 0 ? spId.ToString() : string.Empty;
            tbMa_ThanhPham.Text = ma;
            tbTen_ThanhPham.Text = ten;

            BeginInvoke(new Action(() =>
            {
                _tenThanhPhamSearchHelper.Cleared -= OnBomThanhPhamCleared;
                _tenThanhPhamSearchHelper.Reset();
                _tenThanhPhamSearchHelper.Cleared += OnBomThanhPhamCleared;
            }));

            ResetBomEditState();
            ClearBomNguyenLieuControls();
            LoadBomForThanhPham(spId);
            cbxTen_NL.Focus();
        }

        private void OnBomThanhPhamCleared()
        {
            tbID_TP.Text = string.Empty;
            tbMa_ThanhPham.Text = string.Empty;
            tbTen_ThanhPham.Text = string.Empty;

            ResetBomEditState();
            ClearBomNguyenLieuControls();
            _bomTable = BOMStructure_DB.GetByParentProduct(-1);
            grv.DataSource = _bomTable;
        }

        private void OnBomNguyenLieuSelected(DataRowView row)
        {
            long spId = long.TryParse(row["id"]?.ToString(), out long parsedId) ? parsedId : 0;
            string ma = row["Ma"]?.ToString() ?? string.Empty;
            string ten = row["Ten"]?.ToString() ?? string.Empty;

            tbID_NL.Text = spId > 0 ? spId.ToString() : string.Empty;
            tbMa_NL.Text = ma;
            tbTen_NL.Text = ten;

            BeginInvoke(new Action(() =>
            {
                _tenNLSearchHelper.Cleared -= OnBomNguyenLieuCleared;
                _tenNLSearchHelper.Reset();
                _tenNLSearchHelper.Cleared += OnBomNguyenLieuCleared;
            }));

            nbrTyLe.Focus();
        }

        private void OnBomNguyenLieuCleared()
        {
            ClearBomNguyenLieuControls();
        }

        private void LoadBomForThanhPham(long parentProductId)
        {
            if (parentProductId <= 0)
            {
                _bomTable = BOMStructure_DB.GetByParentProduct(-1);
                grv.DataSource = _bomTable;
                return;
            }

            _bomTable = BOMStructure_DB.GetByParentProduct(parentProductId);
            grv.DataSource = _bomTable;
        }

        // ════════════════════════════════════════════════════════════════════════
        // BOM STRUCTURE — LƯU INSERT / UPDATE
        // ════════════════════════════════════════════════════════════════════════

        private void BtnLuuBom_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            if (!ValidateBomForSave()) return;

            try
            {
                BOMStructure_Model model = BuildBomModelFromControls();

                if (BOMStructure_DB.ExistsDuplicate(
                    model.ParentProduct,
                    model.Component,
                    model.CongDoan,
                    excludeId: _currentBomId))
                {
                    FrmWaiting.ShowGifAlert("BOM đã tồn tại với cùng Thành phẩm, Nguyên liệu và Công đoạn.");
                    return;
                }

                if (_currentBomId > 0)
                {
                    if (_currentBomOldActive == 0 && model.Active == 1)
                    {
                        var confirm = MessageBox.Show(
                            "BOM này đang ở trạng thái Không hoạt động.\nBạn có muốn kích hoạt lại BOM này không?",
                            "Xác nhận kích hoạt lại BOM",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirm != DialogResult.Yes) return;
                    }

                    model.Id = _currentBomId;
                    BOMStructure_DB.Update(model);
                    UpdateCurrentBomGridRow(model);

                    FrmWaiting.ShowGifAlert("Cập nhật BOM thành công!", myIcon: EnumStore.Icon.Success);
                }
                else
                {
                    long newId = BOMStructure_DB.Insert(model);
                    model.Id = newId;
                    AddBomRowToGrid(model);

                    FrmWaiting.ShowGifAlert("Lưu BOM thành công!", myIcon: EnumStore.Icon.Success);
                }

                ResetBomEditState();
                ClearBomNguyenLieuControls();
                nbrTyLe.Value = 1;
                nbrTLeHoanDoi.Value = 1;
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi lưu BOM:\n{ex.Message}");
            }
        }

        private BOMStructure_Model BuildBomModelFromControls()
        {
            long parentProduct = long.TryParse(tbID_TP.Text, out long parsedParent) ? parsedParent : 0;
            long component = long.TryParse(tbID_NL.Text, out long parsedComponent) ? parsedComponent : 0;
            int congDoanId = GetSelectedCongDoanId();
            int active = cbxActive.SelectedIndex;

            return new BOMStructure_Model
            {
                ParentProduct = parentProduct,
                Component = component,
                TyLe = nbrTyLe.Value,
                TyLeHoanDoi = nbrTLeHoanDoi.Value,
                CongDoan = congDoanId,
                Active = active,
                Ma = tbMa_NL.Text.Trim(),
                Ten = tbTen_NL.Text.Trim(),
                TenCongDoan = cbxCongDoan.Text,
                ActiveText = active == 1 ? "Có" : "Không"
            };
        }

        private bool ValidateBomForSave()
        {
            if (!long.TryParse(tbID_TP.Text, out long parentProductId) || parentProductId <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn Thành phẩm.");
                cbxTen_ThanhPham.Focus();
                return false;
            }

            if (!long.TryParse(tbID_NL.Text, out long componentId) || componentId <= 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn Nguyên liệu.");
                cbxTen_NL.Focus();
                return false;
            }

            if (cbxCongDoan.SelectedIndex < 0 || GetSelectedCongDoanId() < 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn Công đoạn.");
                cbxCongDoan.Focus();
                return false;
            }

            if (cbxActive.SelectedIndex < 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn trạng thái Kích hoạt.");
                cbxActive.Focus();
                return false;
            }

            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // BOM STRUCTURE — GRID DOUBLE CLICK / DELETE
        // ════════════════════════════════════════════════════════════════════════

        private void Grv_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in grv.Rows)
                row.Height = 35;
        }

        private void Grv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grv.Rows[e.RowIndex].DataBoundItem is DataRowView view)
                FillBomControlsFromGridRow(view.Row, grv.Rows[e.RowIndex]);
        }

        private void Grv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grv.Columns[e.ColumnIndex].Name != "colDelete") return;
            if (!(grv.Rows[e.RowIndex].DataBoundItem is DataRowView view)) return;

            long bomId = GetLong(view.Row, "id");
            if (bomId <= 0) return;

            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn chuyển BOM này sang trạng thái Không hoạt động?",
                "Xác nhận xoá BOM",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                BOMStructure_DB.SoftDelete(bomId);

                view.Row["activeValue"] = 0;
                view.Row["active"] = "Không";

                if (_currentBomId == bomId)
                {
                    ResetBomEditState();
                    ClearBomNguyenLieuControls();
                    nbrTyLe.Value = 1;
                    nbrTLeHoanDoi.Value = 1;
                }

                FrmWaiting.ShowGifAlert("Đã chuyển BOM sang trạng thái Không hoạt động!", myIcon: EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi xoá BOM:\n{ex.Message}");
            }
        }

        private void FillBomControlsFromGridRow(DataRow row, DataGridViewRow gridRow)
        {
            if (row == null) return;

            _currentBomId = GetLong(row, "id");
            _currentBomOldActive = GetInt(row, "activeValue");
            _currentBomGridRow = gridRow;

            tbID_NL.Text = GetLong(row, "Component").ToString();
            tbMa_NL.Text = GetString(row, "ma");
            tbTen_NL.Text = GetString(row, "ten");

            SetNumericValue(nbrTyLe, GetDecimal(row, "tyLe"));
            SetNumericValue(nbrTLeHoanDoi, GetDecimal(row, "tyLeHoanDoi"));

            string tenCongDoan = GetString(row, "congDoan");
            cbxCongDoan.SelectedItem = tenCongDoan;

            int activeValue = GetInt(row, "activeValue");
            cbxActive.SelectedIndex = activeValue == 1 ? 1 : 0;
        }

        private void AddBomRowToGrid(BOMStructure_Model model)
        {
            if (_bomTable == null)
            {
                _bomTable = BOMStructure_DB.GetByParentProduct(-1);
                grv.DataSource = _bomTable;
            }

            DataRow row = BOMStructure_DB.CreateGridRow(_bomTable, model);
            _bomTable.Rows.Add(row);
        }

        private void UpdateCurrentBomGridRow(BOMStructure_Model model)
        {
            if (_currentBomGridRow?.DataBoundItem is DataRowView view)
            {
                BOMStructure_DB.FillGridRow(view.Row, model);
                return;
            }

            if (_bomTable == null) return;

            foreach (DataRow row in _bomTable.Rows)
            {
                if (GetLong(row, "id") == model.Id)
                {
                    BOMStructure_DB.FillGridRow(row, model);
                    return;
                }
            }
        }

        private void ClearBomNguyenLieuControls()
        {
            tbID_NL.Text = string.Empty;
            tbMa_NL.Text = string.Empty;
            tbTen_NL.Text = string.Empty;
        }

        private void ResetBomEditState()
        {
            _currentBomId = 0;
            _currentBomOldActive = 0;
            _currentBomGridRow = null;
        }

        private int GetSelectedCongDoanId()
        {
            string tenCongDoan = cbxCongDoan.Text;
            var congDoan = ThongTinChungCongDoan.TatCaCongDoan
                .FirstOrDefault(x => x.Id != 2 && x.TenCongDoan == tenCongDoan);

            return congDoan?.Id ?? -1;
        }

        private static string GetString(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName)) return string.Empty;
            return row[columnName] == DBNull.Value ? string.Empty : row[columnName]?.ToString() ?? string.Empty;
        }

        private static long GetLong(DataRow row, string columnName)
        {
            string value = GetString(row, columnName);
            return long.TryParse(value, out long result) ? result : 0;
        }

        private static int GetInt(DataRow row, string columnName)
        {
            string value = GetString(row, columnName);
            return int.TryParse(value, out int result) ? result : 0;
        }

        private static decimal GetDecimal(DataRow row, string columnName)
        {
            string value = GetString(row, columnName);
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }

        private static void SetNumericValue(NumericUpDown control, decimal value)
        {
            if (value < control.Minimum) value = control.Minimum;
            if (value > control.Maximum) value = control.Maximum;
            control.Value = value;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM TTBOSUNG THEO TÊN SP (tbTimKiemTenSP + Enter)
        // ════════════════════════════════════════════════════════════════════════

        private void tbTimKiemTenSP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            string keyword = tbTimKiemTenSP.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keyword)) return;

            try
            {
                BoSungThongTinSP_Model model = BoSungThongTinSP_DB.TimKiemBoSungTheoTenSP(keyword);

                if (model == null)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liêu");
                    return;
                }

                _selectedMaSP_ID = model.DanhSachMaSP_ID;
                tbID.Text = model.DanhSachMaSP_ID > 0 ? model.DanhSachMaSP_ID.ToString() : string.Empty;
                tbMa.Text = model.Ma ?? string.Empty;
                tbTen.Text = model.Ten ?? string.Empty;

                FillDataControls(model);
                _currentBoSungId = model.Id;
                SetEditMode(true);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi tìm kiếm");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // LƯU (INSERT) — btnLuu
        // ════════════════════════════════════════════════════════════════════════

        private void BtnLuu_Click(object sender, EventArgs e)
        {

            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            if (!ValidateForSave()) return;

            try
            {
                BoSungThongTinSP_Model model = BuildModelFromControls();
                BoSungThongTinSP_DB.InsertBoSung(model);
                                
                FrmWaiting.ShowGifAlert("Lưu dữ liệu thành công!",myIcon: EnumStore.Icon.Success);

                ResetForm();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi lưu dữ liệu:\n{ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SỬA (UPDATE) — tbSua
        // ════════════════════════════════════════════════════════════════════════

        private void TbSua_Click(object sender, EventArgs e)
        {
            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }


            if (_currentBoSungId <= 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có bản ghi nào được chọn để sửa.");
                return;
            }

            try
            {
                BoSungThongTinSP_Model model = BuildModelFromControls();
                model.Id = _currentBoSungId;
                BoSungThongTinSP_DB.UpdateBoSung(model);

                FrmWaiting.ShowGifAlert("Cập nhật dữ liệu thành công!", myIcon: EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi cập nhật dữ liệu");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // XOÁ (DELETE) — btnXoa
        // ════════════════════════════════════════════════════════════════════════

        private void BtnXoa_Click(object sender, EventArgs e)
        {

            if (!UserContext.IsAuthenticated
                || (!UserContext.HasRole(RoleNames.Wh) && !UserContext.HasRole(RoleNames.Acc) && !UserContext.HasRole(RoleNames.Admin)))
            {
                FrmWaiting.ShowGifAlert($"Bạn cần cấp quyền để thực hiện yêu cầu này.");
                return;
            }

            if (_currentBoSungId <= 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có bản ghi nào được chọn để xoá.");
                return;
            }

            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xoá bản ghi này?",
                "Xác nhận xoá", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                BoSungThongTinSP_DB.DeleteBoSung(_currentBoSungId);

                FrmWaiting.ShowGifAlert("Xoá bản ghi thành công!", myIcon: EnumStore.Icon.Success);

                ResetForm();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi xoá dữ liệu");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS — BUILD / FILL / CLEAR / VALIDATE
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Tạo BoSungThongTinSP_Model từ các TextBox trên form.</summary>
        private BoSungThongTinSP_Model BuildModelFromControls()
        {
            return new BoSungThongTinSP_Model
            {
                DanhSachMaSP_ID = _selectedMaSP_ID,
                TenChiTiet = tbTenChiTiet.Text.Trim(),
                TieuChuan = tbTieuChuan.Text.Trim(),
                Rph = tbRph.Text.Trim(),
                Rt = tbRt.Text.Trim(),
                Rtd = tbRtd.Text.Trim(),
                Rcd = tbRcd.Text.Trim(),
                Ca = tbCa.Text.Trim(),
                Is = tbIs.Text.Trim(),
                Istt = tbIstt.Text.Trim(),
                Sh = tbSh.Text.Trim(),
                No = tbNo.Text.Trim(),
                D = tbD.Text.Trim(),
                TNC = tbTNC.Text.Trim(),
                OD = tbOD.Text.Trim(),
                T = tbT.Text.Trim(),
                M = tbMau.Text.Trim(),
                KC = tbKetCau.Text.Trim(),
                LTDN = tbLtdn.Text.Trim(),
                GhiChu = tbGhiChu.Text.Trim(),
                LH = tbLH.Text.Trim(),
            };
        }

        /// <summary>Điền dữ liệu từ model vào các control trong tableLayoutPanel1 và tableLayoutPanel3.
        /// Không điền vào cbxTimTen, tbID, tbMa, tbTen.</summary>
        private void FillDataControls(BoSungThongTinSP_Model m)
        {
            // tableLayoutPanel1
            tbTenChiTiet.Text = m.TenChiTiet ?? string.Empty;
            tbTieuChuan.Text = m.TieuChuan ?? string.Empty;

            // tableLayoutPanel3
            tbRph.Text = m.Rph ?? string.Empty;
            tbRt.Text = m.Rt ?? string.Empty;
            tbRtd.Text = m.Rtd ?? string.Empty;
            tbRcd.Text = m.Rcd ?? string.Empty;
            tbCa.Text = m.Ca ?? string.Empty;
            tbIs.Text = m.Is ?? string.Empty;
            tbIstt.Text = m.Istt ?? string.Empty;
            tbSh.Text = m.Sh ?? string.Empty;
            tbNo.Text = m.No ?? string.Empty;
            tbD.Text = m.D ?? string.Empty;
            tbTNC.Text = m.TNC ?? string.Empty;
            tbOD.Text = m.OD ?? string.Empty;
            tbT.Text = m.T ?? string.Empty;
            tbMau.Text = m.M ?? string.Empty;
            tbKetCau.Text = m.KC ?? string.Empty;
            tbLtdn.Text = m.LTDN ?? string.Empty;
            tbGhiChu.Text = m.GhiChu ?? string.Empty;
            tbLH.Text = m.LH ?? string.Empty;
        }

        /// <summary>Xoá trắng toàn bộ các TextBox nhập liệu (tableLayoutPanel1 + tableLayoutPanel3).
        /// Không chạm cbxTimTen, tbID, tbMa, tbTen.</summary>
        private void ClearDataControls()
        {
            tbTenChiTiet.Text = string.Empty;
            tbTieuChuan.Text = string.Empty;

            tbRph.Text = string.Empty;
            tbRt.Text = string.Empty;
            tbRtd.Text = string.Empty;
            tbRcd.Text = string.Empty;
            tbCa.Text = string.Empty;
            tbIs.Text = string.Empty;
            tbIstt.Text = string.Empty;
            tbSh.Text = string.Empty;
            tbNo.Text = string.Empty;
            tbD.Text = string.Empty;
            tbTNC.Text = string.Empty;
            tbOD.Text = string.Empty;
            tbT.Text = string.Empty;
            tbMau.Text = string.Empty;
            tbKetCau.Text = string.Empty;
            tbLtdn.Text = string.Empty;
            tbGhiChu.Text = string.Empty;
            tbLH.Text = string.Empty;
        }

        /// <summary>Reset toàn bộ form về trạng thái ban đầu (sau lưu / sau xoá).</summary>
        private void ResetForm()
        {
            _currentBoSungId = 0;
            _selectedMaSP_ID = 0;

            _tenSPSearchHelper.Reset();   // xoá cbxTimTen
            tbID.Text = string.Empty;
            tbMa.Text = string.Empty;
            tbTen.Text = string.Empty;

            tbTimKiemTenSP.Text = string.Empty;

            ClearDataControls();
            SetEditMode(false);

            cbxTimTen.Focus();
        }

        /// <summary>
        /// false = chế độ thêm mới: btnLuu enabled, tbSua/btnXoa disabled.
        /// true  = chế độ xem/sửa:  tbSua/btnXoa enabled, btnLuu disabled.
        /// </summary>
        private void SetEditMode(bool isEditMode)
        {
            btnLuu.Enabled = !isEditMode;
            tbSua.Enabled = isEditMode;
            btnXoa.Enabled = isEditMode;
        }

        /// <summary>Kiểm tra điều kiện tối thiểu trước khi Insert.</summary>
        private bool ValidateForSave()
        {
            if (_selectedMaSP_ID <= 0)
            {

                FrmWaiting.ShowGifAlert("Vui lòng nhập Thông tin chi tiết");
                cbxTimTen.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbTenChiTiet.Text))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Thông tin chi tiết");
                tbTenChiTiet.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbTieuChuan.Text))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Tiêu chuẩn");

                tbTieuChuan.Focus();
                return false;
            }

            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ════════════════════════════════════════════════════════════════════════

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _tenSPSearchHelper?.Dispose();
            _tenThanhPhamSearchHelper?.Dispose();
            _tenNLSearchHelper?.Dispose();
            base.OnHandleDestroyed(e);
        }

    }
}