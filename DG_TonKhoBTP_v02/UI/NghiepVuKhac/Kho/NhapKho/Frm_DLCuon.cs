using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class Frm_DLCuon : Form
    {
        // ── Dữ liệu hiện tại được truyền vào khi mở lại form / khi edit ───────
        private readonly List<ThongTinCuonDay> _thongTinCuonHienTai;

        // ── Tránh event tự tính chạy trong lúc đang load dữ liệu cũ ──────────
        private bool _loadingGrid = false;

        // ── Tên cột xoá ───────────────────────────────────────────────────────
        private const string COL_XOA = "colXoa";

        // ── Giá trị nội bộ dùng trong combobox cột loai ───────────────────────
        // Lưu ý:
        // - Trong grid: -1 = -- Chọn loại --, 0 = Cuộn, > 0 = TTLo.id
        // - Khi trả về model/snapshot: 0 sẽ được convert thành TTLo_ID = null
        // - -1 chỉ là trạng thái chưa chọn, không được phép lưu
        private const int LOAI_CHUA_CHON_VALUE = -1;
        private const int LOAI_CUON_VALUE = 0;

        // ── Kết quả trả về sau khi lưu thành công ─────────────────────────────
        public List<ThongTinCuonDay> KetQua { get; private set; }
        public List<ThongTinCuonDay> ThongTinCuon { get; private set; }

        public Frm_DLCuon(List<ThongTinCuonDay> thongTinCuonHienTai = null)
        {
            InitializeComponent();

            _thongTinCuonHienTai = thongTinCuonHienTai == null
                ? new List<ThongTinCuonDay>()
                : new List<ThongTinCuonDay>(thongTinCuonHienTai);

            this.Load += Frm_DLCuon_Load;
        }

        // ════════════════════════════════════════════════════════════════════
        // LOAD FORM
        // ════════════════════════════════════════════════════════════════════

        private void Frm_DLCuon_Load(object sender, EventArgs e)
        {
            label1.Text = "THÔNG TIN ĐÓNG GÓI";
            this.Text = "Thông tin đóng gói";

            LoadLoaiDongGoiVaoComboColumn();
            AddDeleteButtonColumn();
            ConfigureGrid();
            LoadThongTinCuonHienTaiVaoGrid();

            foreach (DataGridViewRow row in grvThongTinCuonDay.Rows)
                row.Height = grvThongTinCuonDay.RowTemplate.Height;

            FocusFirstInputCell();
        }

        // ════════════════════════════════════════════════════════════════════
        // LOAD DROPDOWN LOẠI ĐÓNG GÓI
        // ════════════════════════════════════════════════════════════════════

        private void LoadLoaiDongGoiVaoComboColumn()
        {
            // Dùng DataSource + ValueMember kiểu int để tránh lỗi combobox
            // tự nhảy về item đầu tiên khi rời ô.
            // Quy ước trong grid:
            // - -1 = -- Chọn loại --
            // - 0  = Cuộn
            // - >0 = TTLo.id
            // Quy ước trong model/snapshot:
            // - -1 không được lưu
            // - null = Cuộn
            // - >0   = TTLo.id
            DataTable dtSource = new DataTable();
            dtSource.Columns.Add("LoaiValue", typeof(int));
            dtSource.Columns.Add("TenHienThi", typeof(string));

            // Không mặc định là Cuộn. Dòng mới sẽ nhận -1 và bắt buộc người dùng chọn.
            dtSource.Rows.Add(LOAI_CHUA_CHON_VALUE, "-- Chọn loại --");
            dtSource.Rows.Add(LOAI_CUON_VALUE, "Cuộn");

            DataTable dtTTLo = DatabaseHelper.LayDanhSachTTLoActive();
            foreach (DataRow row in dtTTLo.Rows)
            {
                if (row["id"] == DBNull.Value) continue;

                int id = Convert.ToInt32(row["id"]);
                string kichThuoc = row["KichThuoc"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(row["KichThuoc"])?.Trim();

                if (string.IsNullOrWhiteSpace(kichThuoc)) continue;

                // Yêu cầu mới: hiển thị dạng "Lô 12", "Lô 14", ...
                dtSource.Rows.Add(id, "Lô " + kichThuoc);
            }

            if (grvThongTinCuonDay.Columns["loai"] is DataGridViewComboBoxColumn colLoai)
            {
                colLoai.DataSource = null;
                colLoai.Items.Clear();

                colLoai.DataSource = dtSource;
                colLoai.DisplayMember = "TenHienThi";
                colLoai.ValueMember = "LoaiValue";
                colLoai.ValueType = typeof(int);
                colLoai.DefaultCellStyle.NullValue = "-- Chọn loại --";

                colLoai.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                colLoai.FlatStyle = FlatStyle.Flat;
            }
        }

        private static bool TryGetLoaiValueFromCell(DataGridViewCell cell, out int loaiValue)
        {
            loaiValue = -1;

            if (cell == null || cell.Value == null || cell.Value == DBNull.Value)
                return false;

            if (cell.Value is int intValue)
            {
                loaiValue = intValue;
                return true;
            }

            return int.TryParse(Convert.ToString(cell.Value), out loaiValue);
        }

        private static int? ConvertLoaiValueToTTLoId(int loaiValue)
        {
            if (loaiValue == LOAI_CUON_VALUE) return null;
            if (loaiValue > LOAI_CUON_VALUE) return loaiValue;

            // -1 là trạng thái chưa chọn, ValidateLoaiDongGoi sẽ chặn trước khi lưu.
            return null;
        }

        private bool IsCuonRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return false;
            if (!TryGetLoaiValueFromCell(row.Cells["loai"], out int loaiValue)) return false;

            return loaiValue == LOAI_CUON_VALUE;
        }

        private bool IsLoRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return false;
            if (!TryGetLoaiValueFromCell(row.Cells["loai"], out int loaiValue)) return false;

            // Trong grid: > 0 = TTLo.id = Lô
            return loaiValue > LOAI_CUON_VALUE;
        }


        // ════════════════════════════════════════════════════════════════════
        // LOAD DỮ LIỆU CŨ LÊN GRID
        // ════════════════════════════════════════════════════════════════════

        private void LoadThongTinCuonHienTaiVaoGrid()
        {
            _loadingGrid = true;

            try
            {
                grvThongTinCuonDay.Rows.Clear();

                if (_thongTinCuonHienTai.Count == 0)
                    return;

                foreach (ThongTinCuonDay item in _thongTinCuonHienTai)
                {
                    int rowIndex = grvThongTinCuonDay.Rows.Add();
                    DataGridViewRow row = grvThongTinCuonDay.Rows[rowIndex];

                    // Model/snapshot: null = Cuộn.
                    // Grid: 0 = Cuộn.
                    row.Cells["loai"].Value = item.TTLo_ID.HasValue
                        ? item.TTLo_ID.Value
                        : LOAI_CUON_VALUE;

                    row.Cells["slCuon"].Value = item.SoCuon;
                    row.Cells["tongChieuDai"].Value = item.TongChieuDai;
                    row.Cells["soDau"].Value = item.SoDau;
                    row.Cells["soCuoi"].Value = item.soCuoi;
                    row.Cells["ghiChu"].Value = item.Ghichu;

                    ApplyRowMode(row, autoCalculate: false);
                }
            }
            finally
            {
                _loadingGrid = false;
            }
        }

        private void FocusFirstInputCell()
        {
            if (grvThongTinCuonDay.Rows.Count == 0) return;
            if (!grvThongTinCuonDay.Columns.Contains("loai")) return;

            DataGridViewCell cell = grvThongTinCuonDay.Rows[0].Cells["loai"];
            if (cell.Value == null || cell.Value == DBNull.Value)
                cell.Value = LOAI_CHUA_CHON_VALUE;

            grvThongTinCuonDay.CurrentCell = cell;
            grvThongTinCuonDay.BeginEdit(true);
        }

        // ════════════════════════════════════════════════════════════════════
        // CẤU HÌNH GRID THEO TƯ DUY MỚI: XỬ LÝ THEO TỪNG DÒNG
        // ════════════════════════════════════════════════════════════════════

        private void ConfigureGrid()
        {
            grvThongTinCuonDay.AllowUserToAddRows = true;
            grvThongTinCuonDay.AllowUserToDeleteRows = true;

            grvThongTinCuonDay.CellEndEdit -= DataGridView1_CellEndEdit;
            grvThongTinCuonDay.CellEndEdit += DataGridView1_CellEndEdit;

            grvThongTinCuonDay.CellValueChanged -= DataGridView1_CellValueChanged;
            grvThongTinCuonDay.CellValueChanged += DataGridView1_CellValueChanged;

            grvThongTinCuonDay.CurrentCellDirtyStateChanged -= DataGridView1_CurrentCellDirtyStateChanged;
            grvThongTinCuonDay.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;

            grvThongTinCuonDay.DataError -= DataGridView1_DataError;
            grvThongTinCuonDay.DataError += DataGridView1_DataError;

            grvThongTinCuonDay.DefaultValuesNeeded -= DataGridView1_DefaultValuesNeeded;
            grvThongTinCuonDay.DefaultValuesNeeded += DataGridView1_DefaultValuesNeeded;
        }

        private void ApplyRowMode(DataGridViewRow row, bool autoCalculate)
        {
            if (row == null || row.IsNewRow) return;

            // Nếu chưa chọn Loại đóng gói thì không xem là Cuộn/Lô.
            // Người dùng sẽ bị validate bắt buộc chọn khi bấm Lưu.
            if (!TryGetLoaiValueFromCell(row.Cells["loai"], out int loaiValue)
                || loaiValue == LOAI_CHUA_CHON_VALUE)
            {
                SetCellReadonly(row, "tongChieuDai", false);
                SetCellReadonly(row, "soDau", false);
                SetCellReadonly(row, "soCuoi", false);
                return;
            }

            bool isCuon = loaiValue == LOAI_CUON_VALUE;

            if (isCuon)
            {
                // Cuộn:
                // - Người dùng nhập Tổng CD.
                // - Số đầu / Số cuối tự tính và bị khóa.
                SetCellReadonly(row, "tongChieuDai", false);
                SetCellReadonly(row, "soDau", true);
                SetCellReadonly(row, "soCuoi", true);

                if (autoCalculate)
                    AutoSetSoDauSoCuoiForCuon(row);
            }
            else
            {
                // Lô:
                // - Người dùng nhập Số đầu / Số cuối.
                // - Tổng CD tự tính = Số cuối - Số đầu và bị khóa.
                SetCellReadonly(row, "tongChieuDai", true);
                SetCellReadonly(row, "soDau", false);
                SetCellReadonly(row, "soCuoi", false);

                if (autoCalculate)
                    AutoSetTongChieuDaiForLo(row);
            }
        }


        private void AutoSetSoDauSoCuoiForCuon(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;
            if (!IsCuonRow(row)) return;

            string raw = row.Cells["tongChieuDai"].Value?.ToString()?.Trim() ?? string.Empty;
            if (!int.TryParse(raw, out int tongCD) || tongCD < 0) return;

            row.Cells["soDau"].Value = 0;
            row.Cells["soCuoi"].Value = tongCD;
        }

        private void AutoSetTongChieuDaiForLo(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;
            if (!IsLoRow(row)) return;

            string rawSoDau = row.Cells["soDau"].Value?.ToString()?.Trim() ?? string.Empty;
            string rawSoCuoi = row.Cells["soCuoi"].Value?.ToString()?.Trim() ?? string.Empty;

            if (!int.TryParse(rawSoDau, out int soDau) || soDau < 0)
                return;

            if (!int.TryParse(rawSoCuoi, out int soCuoi) || soCuoi < 0)
                return;

            int tongCD = soCuoi - soDau;

            if (tongCD < 0)
                return;

            row.Cells["tongChieuDai"].Value = tongCD;
        }


        private void SetCellReadonly(DataGridViewRow row, string colName, bool readOnly)
        {
            if (!grvThongTinCuonDay.Columns.Contains(colName)) return;

            DataGridViewCell cell = row.Cells[colName];
            cell.ReadOnly = readOnly;

            if (readOnly)
            {
                cell.Style.BackColor = SystemColors.Control;
                cell.Style.ForeColor = SystemColors.GrayText;
            }
            else
            {
                cell.Style.BackColor = Color.Empty;
                cell.Style.ForeColor = Color.Empty;
            }
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grvThongTinCuonDay.IsCurrentCellDirty)
                grvThongTinCuonDay.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_loadingGrid) return;
            if (e.RowIndex < 0) return;
            if (grvThongTinCuonDay.Rows[e.RowIndex].IsNewRow) return;

            string colName = grvThongTinCuonDay.Columns[e.ColumnIndex].Name;
            DataGridViewRow row = grvThongTinCuonDay.Rows[e.RowIndex];

            if (colName == "loai")
            {
                ApplyRowMode(row, autoCalculate: true);
                return;
            }

            if (colName == "tongChieuDai")
            {
                // Cuộn: nhập Tổng CD => tự set Số đầu / Số cuối.
                if (IsCuonRow(row))
                    AutoSetSoDauSoCuoiForCuon(row);

                return;
            }

            if (colName == "soDau" || colName == "soCuoi")
            {
                // Lô: nhập Số đầu / Số cuối => tự tính Tổng CD.
                if (IsLoRow(row))
                    AutoSetTongChieuDaiForLo(row);

                return;
            }
        }


        private void DataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            // Dòng mới không được mặc định là Cuộn.
            // Gán -1 để hiển thị "-- Chọn loại --" và bắt buộc người dùng chọn khi lưu.
            if (e.Row != null && grvThongTinCuonDay.Columns.Contains("loai"))
                e.Row.Cells["loai"].Value = LOAI_CHUA_CHON_VALUE;
        }

        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Tránh văng lỗi DataGridViewComboBoxCell value is not valid.
            // Validate nghiệp vụ sẽ xử lý rõ khi người dùng bấm Lưu.
            e.ThrowException = false;
        }

        // ════════════════════════════════════════════════════════════════════
        // CỘT XOÁ
        // ════════════════════════════════════════════════════════════════════

        private void AddDeleteButtonColumn()
        {
            if (grvThongTinCuonDay.Columns.Contains(COL_XOA)) return;

            var btnCol = new DataGridViewButtonColumn
            {
                Name = COL_XOA,
                HeaderText = "",
                Text = "Xoá",
                UseColumnTextForButtonValue = true,
                Width = 65,
                FlatStyle = FlatStyle.Flat
            };

            btnCol.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
            btnCol.DefaultCellStyle.ForeColor = Color.DarkRed;
            btnCol.DefaultCellStyle.Font = new Font("Tahoma", 9.75F, FontStyle.Bold);

            grvThongTinCuonDay.Columns.Add(btnCol);

            grvThongTinCuonDay.CellClick -= DataGridView1_CellClick_Xoa;
            grvThongTinCuonDay.CellClick += DataGridView1_CellClick_Xoa;
        }

        private void DataGridView1_CellClick_Xoa(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!grvThongTinCuonDay.Columns.Contains(COL_XOA)) return;
            if (grvThongTinCuonDay.Columns[e.ColumnIndex].Name != COL_XOA) return;
            if (grvThongTinCuonDay.Rows[e.RowIndex].IsNewRow) return;

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xoá dòng {e.RowIndex + 1} không?",
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
                grvThongTinCuonDay.Rows.RemoveAt(e.RowIndex);
        }

        // ════════════════════════════════════════════════════════════════════
        // VALIDATE
        // ════════════════════════════════════════════════════════════════════

        public static bool TryReadDgvCellInt(DataGridView dgv, int rowIndex, string columnName, out int parsedValue)
        {
            parsedValue = 0;

            if (!dgv.Columns.Contains(columnName)) return false;

            string raw = dgv.Rows[rowIndex].Cells[columnName].Value?.ToString()?.Trim() ?? string.Empty;
            return int.TryParse(raw, out parsedValue) && parsedValue >= 0;
        }

        public static bool ValidateDgvCellIsNonNegativeInt(
            DataGridView dgv,
            int rowIndex,
            string columnName,
            string tenCot,
            out int parsedValue)
        {
            parsedValue = 0;

            if (!dgv.Columns.Contains(columnName)) return true;

            DataGridViewCell cell = dgv.Rows[rowIndex].Cells[columnName];
            string raw = cell.Value?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                MarkCellError(cell);
                MessageBox.Show($"Dòng {rowIndex + 1} – Cột \"{tenCot}\" không được để trống.",
                    "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgv.CurrentCell = cell;
                return false;
            }

            if (!int.TryParse(raw, out parsedValue) || parsedValue < 0)
            {
                MarkCellError(cell);
                MessageBox.Show($"Dòng {rowIndex + 1} – Cột \"{tenCot}\" phải là số nguyên ≥ 0.\nGiá trị nhập: \"{raw}\"",
                    "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgv.CurrentCell = cell;
                return false;
            }

            ResetCellColor(cell);
            return true;
        }

        private bool ValidateLoaiDongGoi(int rowIndex, out int? ttLoId)
        {
            ttLoId = null;

            if (!grvThongTinCuonDay.Columns.Contains("loai"))
                return true;

            DataGridViewCell cell = grvThongTinCuonDay.Rows[rowIndex].Cells["loai"];

            if (!TryGetLoaiValueFromCell(cell, out int loaiValue))
            {
                MarkCellError(cell);
                MessageBox.Show($"Dòng {rowIndex + 1} – Cột \"Loại đóng gói\" không được để trống.",
                    "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                grvThongTinCuonDay.CurrentCell = cell;
                return false;
            }

            if (loaiValue == LOAI_CHUA_CHON_VALUE)
            {
                MarkCellError(cell);
                MessageBox.Show($"Dòng {rowIndex + 1} – Vui lòng chọn \"Loại đóng gói\".",
                    "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                grvThongTinCuonDay.CurrentCell = cell;
                return false;
            }

            if (loaiValue < LOAI_CHUA_CHON_VALUE)
            {
                MarkCellError(cell);
                MessageBox.Show($"Dòng {rowIndex + 1} – Cột \"Loại đóng gói\" không hợp lệ.",
                    "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                grvThongTinCuonDay.CurrentCell = cell;
                return false;
            }

            ttLoId = ConvertLoaiValueToTTLoId(loaiValue);
            ResetCellColor(cell);
            return true;
        }

        private static void MarkCellError(DataGridViewCell cell)
        {
            if (cell != null) cell.Style.BackColor = Color.MistyRose;
        }

        private static void ResetCellColor(DataGridViewCell cell)
        {
            if (cell != null) cell.Style.BackColor = Color.Empty;
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grvThongTinCuonDay.Rows[e.RowIndex].IsNewRow) return;

            string colName = grvThongTinCuonDay.Columns[e.ColumnIndex].Name;
            DataGridViewRow row = grvThongTinCuonDay.Rows[e.RowIndex];

            var intCols = new Dictionary<string, string>
            {
                { "slCuon",       "Số lượng" },
                { "tongChieuDai", "Tổng chiều dài" },
                { "soDau",        "Số đầu" },
                { "soCuoi",       "Số cuối" }
            };

            if (intCols.TryGetValue(colName, out string tenCot))
            {
                // Không validate ngay ô readonly vì đó là ô hệ thống tự tính.
                // Validate đầy đủ sẽ chạy khi bấm nút Lưu.
                if (!row.Cells[colName].ReadOnly)
                    ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, e.RowIndex, colName, tenCot, out _);
            }

            if ((colName == "soDau" || colName == "soCuoi") && IsLoRow(row))
            {
                AutoSetTongChieuDaiForLo(row);
            }

            if (colName == "tongChieuDai" && IsCuonRow(row))
            {
                AutoSetSoDauSoCuoiForCuon(row);
            }
        }


        // ════════════════════════════════════════════════════════════════════
        // NÚT LƯU: CHỈ TRẢ DỮ LIỆU VỀ UC, CHƯA LƯU DB
        // ════════════════════════════════════════════════════════════════════

        private void btnLuuTTCuonDay_Click(object sender, EventArgs e)
        {
            grvThongTinCuonDay.EndEdit();

            int rowCount = grvThongTinCuonDay.Rows.Count;
            if (grvThongTinCuonDay.AllowUserToAddRows && rowCount > 0)
                rowCount--;

            if (rowCount == 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có dữ liệu để lưu.");
                return;
            }

            var result = new List<ThongTinCuonDay>();

            for (int i = 0; i < rowCount; i++)
            {
                if (grvThongTinCuonDay.Rows[i].IsNewRow) continue;

                DataGridViewRow row = grvThongTinCuonDay.Rows[i];

                if (!ValidateLoaiDongGoi(i, out int? ttLoId)) return;

                // Đảm bảo khóa/mở khóa và tự tính đúng theo loại trước khi validate.
                ApplyRowMode(row, autoCalculate: true);

                if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "slCuon", "Số lượng", out int slCuon)) return;

                if (!ttLoId.HasValue)
                {
                    // Cuộn:
                    // - Người dùng nhập Tổng CD.
                    // - Hệ thống tự tính Số đầu = 0, Số cuối = Tổng CD.
                    if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "tongChieuDai", "Tổng chiều dài", out _)) return;
                    AutoSetSoDauSoCuoiForCuon(row);
                }
                else
                {
                    // Lô:
                    // - Người dùng nhập Số đầu / Số cuối.
                    // - Hệ thống tự tính Tổng CD = Số cuối - Số đầu.
                    if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "soDau", "Số đầu", out _)) return;
                    if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "soCuoi", "Số cuối", out _)) return;
                    AutoSetTongChieuDaiForLo(row);
                }

                if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "tongChieuDai", "Tổng chiều dài", out int tongCD)) return;
                if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "soDau", "Số đầu", out int soDau)) return;
                if (!ValidateDgvCellIsNonNegativeInt(grvThongTinCuonDay, i, "soCuoi", "Số cuối", out int soCuoi)) return;

                int diff = soCuoi - soDau;
                if (tongCD != diff)
                {
                    MarkCellError(row.Cells["tongChieuDai"]);
                    MarkCellError(row.Cells["soDau"]);
                    MarkCellError(row.Cells["soCuoi"]);

                    MessageBox.Show(
                        $"Dòng {i + 1}: Tổng chiều dài ({tongCD}) phải bằng Số cuối − Số đầu\n" +
                        $"({soCuoi} − {soDau} = {diff}).",
                        "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                result.Add(new ThongTinCuonDay
                {
                    TTLo_ID = ttLoId,
                    SoCuon = slCuon,
                    TongChieuDai = tongCD,
                    SoDau = soDau,
                    soCuoi = soCuoi,
                    Ghichu = row.Cells["ghiChu"].Value?.ToString()?.Trim() ?? string.Empty
                });
            }

            KetQua = result;
            ThongTinCuon = KetQua;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
