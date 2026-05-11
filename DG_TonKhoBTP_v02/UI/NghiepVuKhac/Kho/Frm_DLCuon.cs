using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    public partial class Frm_DLCuon : Form
    {
        // ── Chế độ: true = nhiều cuộn, false = 1 dây (lô) ──────────────────
        private readonly bool _isCuon;

        // ── Dữ liệu hiện tại được truyền từ UC_NhapKho sang khi sửa ─────────
        private readonly List<ThongTinCuonDay> _thongTinCuonHienTai;

        // ── Tránh event tự tính chạy trong lúc đang load dữ liệu cũ ─────────
        private bool _loadingGrid = false;

        // ── Tên cột xoá ─────────────────────────────────────────────────────
        private const string COL_XOA = "colXoa";

        // ── Kết quả trả về sau khi lưu thành công ───────────────────────────
        public List<ThongTinCuonDay> KetQua { get; private set; }

        public List<ThongTinCuonDay> ThongTinCuon { get; private set; }

        public Frm_DLCuon(bool isCuon, List<ThongTinCuonDay> thongTinCuonHienTai = null)
        {
            InitializeComponent();

            _isCuon = isCuon;
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
            AddDeleteButtonColumn();
            ConfigureGridByMode();
            LoadThongTinCuonHienTaiVaoGrid();

            label1.Text = _isCuon ? "NHẬP THÔNG TIN CUỘN" : "NHẬP THÔNG TIN DÂY";

            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Height = dataGridView1.RowTemplate.Height;

            FocusFirstInputCell();
        }

        // ════════════════════════════════════════════════════════════════════
        // LOAD DỮ LIỆU CŨ LÊN GRID
        // ════════════════════════════════════════════════════════════════════

        private void LoadThongTinCuonHienTaiVaoGrid()
        {
            _loadingGrid = true;

            try
            {
                dataGridView1.Rows.Clear();

                if (_thongTinCuonHienTai.Count > 0)
                {
                    foreach (ThongTinCuonDay item in _thongTinCuonHienTai)
                    {
                        int rowIndex = dataGridView1.Rows.Add();
                        DataGridViewRow row = dataGridView1.Rows[rowIndex];

                        row.Cells["slCuon"].Value = item.SoCuon;
                        row.Cells["tongChieuDai"].Value = item.TongChieuDai;
                        row.Cells["soDau"].Value = item.SoDau;
                        row.Cells["soCuoi"].Value = item.SoCuoi;
                        row.Cells["ghichu"].Value = item.Ghichu;

                        if (!_isCuon)
                            KhoaCotSoCuonChoDongLo(row);
                    }

                    return;
                }

                // Trường hợp nhập mới ở chế độ lô/dây:
                // tạo sẵn 1 dòng, số cuộn = 1 và khoá ô này.
                if (!_isCuon)
                {
                    int rowIndex = dataGridView1.Rows.Add();
                    DataGridViewRow row = dataGridView1.Rows[rowIndex];

                    row.Cells["slCuon"].Value = 1;
                    KhoaCotSoCuonChoDongLo(row);
                }
            }
            finally
            {
                _loadingGrid = false;
            }
        }

        private void KhoaCotSoCuonChoDongLo(DataGridViewRow row)
        {
            if (!dataGridView1.Columns.Contains("slCuon")) return;

            DataGridViewCell cell = row.Cells["slCuon"];

            if (cell.Value == null || string.IsNullOrWhiteSpace(cell.Value.ToString()))
                cell.Value = 1;

            cell.ReadOnly = true;
            cell.Style.BackColor = SystemColors.Control;
            cell.Style.ForeColor = SystemColors.GrayText;
        }

        private void FocusFirstInputCell()
        {
            if (dataGridView1.Rows.Count == 0) return;

            int rowIndex = 0;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (!dataGridView1.Rows[i].IsNewRow)
                {
                    rowIndex = i;
                    break;
                }
            }

            string colName = _isCuon ? "slCuon" : "tongChieuDai";

            if (!dataGridView1.Columns.Contains(colName)) return;

            dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[colName];
            dataGridView1.BeginEdit(true);
        }

        // ════════════════════════════════════════════════════════════════════
        // CẤU HÌNH GRID THEO CHẾ ĐỘ
        // ════════════════════════════════════════════════════════════════════

        private void ConfigureGridByMode()
        {
            if (_isCuon)
            {
                // Nhiều dòng; slCuon + tongChieuDai nhập được; soDau/soCuoi chỉ đọc
                dataGridView1.AllowUserToAddRows = true;
                ApplyReadOnlyStyle("soDau");
                ApplyReadOnlyStyle("soCuoi");

                dataGridView1.CellValueChanged -= DataGridView1_CellValueChanged_Cuon;
                dataGridView1.CellValueChanged += DataGridView1_CellValueChanged_Cuon;
            }
            else
            {
                // 1 dòng cố định; tongChieuDai + soDau + soCuoi nhập được
                dataGridView1.AllowUserToAddRows = false;
            }

            dataGridView1.CellEndEdit -= DataGridView1_CellEndEdit;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
        }

        private void ApplyReadOnlyStyle(string colName)
        {
            if (!dataGridView1.Columns.Contains(colName)) return;

            dataGridView1.Columns[colName].ReadOnly = true;
            dataGridView1.Columns[colName].DefaultCellStyle.BackColor = SystemColors.Control;
            dataGridView1.Columns[colName].DefaultCellStyle.ForeColor = SystemColors.GrayText;
        }

        // ════════════════════════════════════════════════════════════════════
        // CỘT XOÁ
        // ════════════════════════════════════════════════════════════════════

        private void AddDeleteButtonColumn()
        {
            if (dataGridView1.Columns.Contains(COL_XOA)) return;

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
            btnCol.DefaultCellStyle.Font =
                new Font("Tahoma", 9.75F, FontStyle.Bold);

            dataGridView1.Columns.Add(btnCol);

            dataGridView1.CellClick -= DataGridView1_CellClick_Xoa;
            dataGridView1.CellClick += DataGridView1_CellClick_Xoa;
        }

        private void DataGridView1_CellClick_Xoa(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dataGridView1.Columns.Contains(COL_XOA)) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != COL_XOA) return;
            if (dataGridView1.Rows[e.RowIndex].IsNewRow) return;

            if (!_isCuon)
            {
                MessageBox.Show("Chế độ này chỉ có 1 dòng dữ liệu, không thể xoá.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xoá dòng {e.RowIndex + 1} không?",
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
                dataGridView1.Rows.RemoveAt(e.RowIndex);
        }

        // ════════════════════════════════════════════════════════════════════
        // VALIDATE INT
        // ════════════════════════════════════════════════════════════════════

        public static bool TryReadDgvCellInt(
            DataGridView dgv,
            int rowIndex,
            string columnName,
            out int parsedValue)
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

            var cell = dgv.Rows[rowIndex].Cells[columnName];
            string raw = cell.Value?.ToString()?.Trim() ?? string.Empty;

            if (dgv.Columns[columnName].ReadOnly)
            {
                int.TryParse(raw, out parsedValue);
                return true;
            }

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

        private static void MarkCellError(DataGridViewCell cell)
            => cell.Style.BackColor = Color.MistyRose;

        private static void ResetCellColor(DataGridViewCell cell)
            => cell.Style.BackColor = Color.Empty;

        // ════════════════════════════════════════════════════════════════════
        // SỰ KIỆN VALIDATE SAU KHI RỜI Ô
        // ════════════════════════════════════════════════════════════════════

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Rows[e.RowIndex].IsNewRow) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            var intCols = new Dictionary<string, string>
            {
                { "slCuon",       "Số lượng cuộn" },
                { "tongChieuDai", "Tổng chiều dài" },
                { "soDau",        "Số đầu" },
                { "soCuoi",       "Số cuối" }
            };

            if (intCols.TryGetValue(colName, out string tenCot))
                ValidateDgvCellIsNonNegativeInt(dataGridView1, e.RowIndex, colName, tenCot, out _);
        }

        // ════════════════════════════════════════════════════════════════════
        // TỰ TÍNH soDau / soCuoi KHI isCuon = true
        // ════════════════════════════════════════════════════════════════════

        private void DataGridView1_CellValueChanged_Cuon(object sender, DataGridViewCellEventArgs e)
        {
            if (_loadingGrid) return;
            if (e.RowIndex < 0) return;
            if (dataGridView1.Rows[e.RowIndex].IsNewRow) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "tongChieuDai") return;

            var row = dataGridView1.Rows[e.RowIndex];
            string raw = row.Cells["tongChieuDai"].Value?.ToString()?.Trim() ?? string.Empty;

            if (int.TryParse(raw, out int cd) && cd >= 0)
            {
                row.Cells["soDau"].Value = 0;
                row.Cells["soCuoi"].Value = cd;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // NÚT LƯU
        // ════════════════════════════════════════════════════════════════════

        private void btnLuuTTCuonDay_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();

            int rowCount = dataGridView1.Rows.Count;

            if (dataGridView1.AllowUserToAddRows && rowCount > 0)
                rowCount--;

            if (rowCount == 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có dữ liệu để lưu.");
                return;
            }

            var result = new List<ThongTinCuonDay>();

            for (int i = 0; i < rowCount; i++)
            {
                if (dataGridView1.Rows[i].IsNewRow) continue;

                if (!ValidateDgvCellIsNonNegativeInt(dataGridView1, i, "slCuon", "Số lượng cuộn", out int slCuon)) return;
                if (!ValidateDgvCellIsNonNegativeInt(dataGridView1, i, "tongChieuDai", "Tổng chiều dài", out int tongCD)) return;
                if (!ValidateDgvCellIsNonNegativeInt(dataGridView1, i, "soDau", "Số đầu", out int soDau)) return;
                if (!ValidateDgvCellIsNonNegativeInt(dataGridView1, i, "soCuoi", "Số cuối", out int soCuoi)) return;

                int diff = soCuoi - soDau;

                if (tongCD != diff)
                {
                    MarkCellError(dataGridView1.Rows[i].Cells["tongChieuDai"]);
                    MarkCellError(dataGridView1.Rows[i].Cells["soDau"]);
                    MarkCellError(dataGridView1.Rows[i].Cells["soCuoi"]);

                    MessageBox.Show(
                        $"Dòng {i + 1}: Tổng chiều dài ({tongCD}) phải bằng Số cuối − Số đầu\n" +
                        $"({soCuoi} − {soDau} = {diff}).",
                        "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                result.Add(new ThongTinCuonDay
                {
                    SoCuon = slCuon,
                    TongChieuDai = tongCD,
                    SoDau = soDau,
                    SoCuoi = soCuoi,
                    Ghichu = dataGridView1.Rows[i].Cells["ghichu"].Value?.ToString()?.Trim() ?? string.Empty
                });
            }

            KetQua = result;
            ThongTinCuon = KetQua;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}