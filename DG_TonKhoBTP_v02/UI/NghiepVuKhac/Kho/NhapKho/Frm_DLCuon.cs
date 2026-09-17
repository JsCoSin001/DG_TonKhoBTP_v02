using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public enum FrmDLCuonMode
    {
        DongGoiNguon = 0,
        NhapKho = 1
    }

    public partial class Frm_DLCuon : Form
    {
        // ── Dữ liệu hiện tại được truyền vào khi mở lại form / khi edit ───────
        private readonly List<ThongTinCuonDay> _thongTinCuonHienTai;

        // Frm_DLCuon được dùng chung:
        // - DongGoiNguon: giữ nguyên nghiệp vụ khai báo/chỉnh quy cách đóng gói.
        // - NhapKho: chỉ cho phép chỉnh số lượng nhập hoặc bỏ dòng khỏi lần nhập hiện tại.
        private readonly FrmDLCuonMode _mode;
        private readonly Dictionary<long, int> _soCuonToiDaNhapKho;

        // Chỉ btnDongGoi truyền true.
        // Khi true, dòng Cuộn MỚI (chưa có TTCuonDay_CD_ID) sẽ để Số đầu/Số cuối = NULL.
        // Dòng đã có trong DB luôn giữ nguyên Số đầu/Số cuối được truyền vào.
        private readonly bool _nullSoDauSoCuoiChoCuonMoi;

        // ── Tránh event tự tính chạy trong lúc đang load dữ liệu cũ ──────────
        private bool _loadingGrid = false;

        // ── Tên các cột thao tác ───────────────────────────────────────────────
        private const string COL_XOA = "colXoa";
        private const string COL_DAO_CHIEU = "colDaoChieu";

        // ── Giá trị nội bộ dùng trong combobox cột loai ───────────────────────
        // Lưu ý:
        // - Trong grid: -1 = -- Chọn loại --, 0 = Cuộn, > 0 = TTLo.id
        // - Khi trả về model/snapshot: 0 sẽ được convert thành TTLo_ID = null
        // - -1 chỉ là trạng thái chưa chọn, không được phép lưu
        private const int LOAI_CHUA_CHON_VALUE = -1;
        private const int LOAI_CUON_VALUE = 0;

        // Nguồn mặc định của combobox chỉ chứa Cuộn + TTLo đang Active.
        // Các TTLo inactive/không hợp lệ chỉ được bổ sung cho đúng cell lịch sử cần hiển thị.
        private DataTable _loaiDongGoiSource;
        private readonly Dictionary<int, string> _ttLoKichThuocById = new Dictionary<int, string>();
        private readonly HashSet<int> _ttLoActiveIds = new HashSet<int>();

        // ── Kết quả trả về sau khi lưu thành công ─────────────────────────────
        public List<ThongTinCuonDay> KetQua { get; private set; }
        public List<ThongTinCuonDay> ThongTinCuon { get; private set; }

        public Frm_DLCuon(
            List<ThongTinCuonDay> thongTinCuonHienTai = null,
            bool nullSoDauSoCuoiChoCuonMoi = false)
            : this(
                thongTinCuonHienTai,
                FrmDLCuonMode.DongGoiNguon,
                null,
                nullSoDauSoCuoiChoCuonMoi)
        {
        }

        public Frm_DLCuon(
            List<ThongTinCuonDay> thongTinCuonHienTai,
            FrmDLCuonMode mode,
            IDictionary<long, int> soCuonToiDaNhapKho)
            : this(thongTinCuonHienTai, mode, soCuonToiDaNhapKho, false)
        {
        }

        public Frm_DLCuon(
            List<ThongTinCuonDay> thongTinCuonHienTai,
            FrmDLCuonMode mode,
            IDictionary<long, int> soCuonToiDaNhapKho,
            bool nullSoDauSoCuoiChoCuonMoi)
        {
            InitializeComponent();

            _mode = mode;
            _nullSoDauSoCuoiChoCuonMoi = nullSoDauSoCuoiChoCuonMoi;
            _thongTinCuonHienTai = thongTinCuonHienTai == null
                ? new List<ThongTinCuonDay>()
                : new List<ThongTinCuonDay>(thongTinCuonHienTai);

            _soCuonToiDaNhapKho = soCuonToiDaNhapKho == null
                ? new Dictionary<long, int>()
                : new Dictionary<long, int>(soCuonToiDaNhapKho);

            this.Load += Frm_DLCuon_Load;
        }

        private static ThongTinCuonDay CloneThongTinCuonDay(ThongTinCuonDay item)
        {
            if (item == null) return null;

            return new ThongTinCuonDay
            {
                TTCuonDay_CD_ID = item.TTCuonDay_CD_ID,
                TTLo_ID = item.TTLo_ID,
                KichThuocLo = item.KichThuocLo ?? string.Empty,
                TTLoHopLe = item.TTLoHopLe,
                SoCuon = item.SoCuon,
                TongChieuDai = item.TongChieuDai,
                SoDau = item.SoDau,
                soCuoi = item.soCuoi,
                Ghichu = item.Ghichu ?? string.Empty
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // LOAD FORM
        // ════════════════════════════════════════════════════════════════════

        private void Frm_DLCuon_Load(object sender, EventArgs e)
        {
            if (_mode == FrmDLCuonMode.NhapKho)
            {
                label1.Text = "THÔNG TIN CUỘN/LÔ NHẬP KHO";
                this.Text = "Thông tin cuộn/lô nhập kho";
                btnLuuTTCuonDay.Text = "OK";
            }
            else
            {
                label1.Text = "THÔNG TIN ĐÓNG GÓI";
                this.Text = "Thông tin đóng gói";
            }

            LoadLoaiDongGoiVaoComboColumn();
            AddReverseButtonColumn();
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
            _ttLoKichThuocById.Clear();
            _ttLoActiveIds.Clear();

            DataTable dtSource = TaoNguonLoaiDongGoiCoBan();

            DataTable dtTTLoActive = DatabaseHelper.LayDanhSachTTLoActive();
            foreach (DataRow row in dtTTLoActive.Rows)
            {
                if (row["id"] == DBNull.Value) continue;

                int id = Convert.ToInt32(row["id"]);
                string kichThuoc = row["KichThuoc"] == DBNull.Value
                    ? string.Empty
                    : (Convert.ToString(row["KichThuoc"]) ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(kichThuoc)) continue;

                _ttLoActiveIds.Add(id);
                _ttLoKichThuocById[id] = kichThuoc;
                dtSource.Rows.Add(id, "Lô " + kichThuoc);
            }

            List<int> referencedIds = _thongTinCuonHienTai
                .Where(x => x.TTLo_ID.HasValue && x.TTLo_ID.Value > 0)
                .Select(x => x.TTLo_ID.Value)
                .Distinct()
                .ToList();

            if (referencedIds.Count > 0)
            {
                DataTable dtReferenced = DatabaseHelper.LayDanhSachTTLoTheoIds(referencedIds);
                HashSet<int> foundIds = new HashSet<int>();

                foreach (DataRow row in dtReferenced.Rows)
                {
                    if (row["id"] == DBNull.Value) continue;

                    int id = Convert.ToInt32(row["id"]);
                    string kichThuoc = row["KichThuoc"] == DBNull.Value
                        ? string.Empty
                        : (Convert.ToString(row["KichThuoc"]) ?? string.Empty).Trim();

                    foundIds.Add(id);
                    if (!string.IsNullOrWhiteSpace(kichThuoc))
                        _ttLoKichThuocById[id] = kichThuoc;
                }

            }

            _loaiDongGoiSource = dtSource;

            if (grvThongTinCuonDay.Columns["loai"] is DataGridViewComboBoxColumn colLoai)
            {
                colLoai.DataSource = null;
                colLoai.Items.Clear();

                colLoai.DataSource = _loaiDongGoiSource;
                colLoai.DisplayMember = "TenHienThi";
                colLoai.ValueMember = "LoaiValue";
                colLoai.ValueType = typeof(int);
                colLoai.DefaultCellStyle.NullValue = "-- Chọn loại --";

                colLoai.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                colLoai.FlatStyle = FlatStyle.Flat;
            }
        }

        private static DataTable TaoNguonLoaiDongGoiCoBan()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("LoaiValue", typeof(int));
            dt.Columns.Add("TenHienThi", typeof(string));
            dt.Rows.Add(LOAI_CHUA_CHON_VALUE, "-- Chọn loại --");
            dt.Rows.Add(LOAI_CUON_VALUE, "Cuộn");
            return dt;
        }

        private void GanNguonLoaiChoDongLichSu(DataGridViewRow row, ThongTinCuonDay item)
        {
            if (row == null || !item.TTLo_ID.HasValue) return;

            int ttLoId = item.TTLo_ID.Value;
            if (_ttLoActiveIds.Contains(ttLoId)) return;

            if (!(row.Cells["loai"] is DataGridViewComboBoxCell cell)) return;

            DataTable cellSource = _loaiDongGoiSource?.Copy() ?? TaoNguonLoaiDongGoiCoBan();

            if (_ttLoKichThuocById.TryGetValue(ttLoId, out string kichThuoc)
                && !string.IsNullOrWhiteSpace(kichThuoc))
            {
                cellSource.Rows.Add(ttLoId, "Lô " + kichThuoc + " (ngừng sử dụng)");
            }
            else
            {
                cellSource.Rows.Add(ttLoId, $"Lô [ID {ttLoId}] - không hợp lệ");
            }

            cell.DataSource = cellSource;
            cell.DisplayMember = "TenHienThi";
            cell.ValueMember = "LoaiValue";
            cell.ValueType = typeof(int);
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
                    // Giữ khóa nguồn để khi chỉnh sửa có thể UPDATE đúng TTCuonDay_CD,
                    // không nhận dạng gián tiếp bằng số cuộn/chiều dài.
                    row.Tag = item.TTCuonDay_CD_ID;

                    // Model/snapshot: null = Cuộn.
                    // Grid: 0 = Cuộn.
                    // TTLo inactive/không tồn tại chỉ được bổ sung vào đúng cell lịch sử này.
                    GanNguonLoaiChoDongLichSu(row, item);
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

            string columnName = _mode == FrmDLCuonMode.NhapKho ? "slCuon" : "loai";
            if (!grvThongTinCuonDay.Columns.Contains(columnName)) return;

            DataGridViewCell cell = grvThongTinCuonDay.Rows[0].Cells[columnName];
            if (_mode != FrmDLCuonMode.NhapKho && (cell.Value == null || cell.Value == DBNull.Value))
                cell.Value = LOAI_CHUA_CHON_VALUE;

            grvThongTinCuonDay.CurrentCell = cell;
            if (!cell.ReadOnly)
                grvThongTinCuonDay.BeginEdit(true);
        }

        // ════════════════════════════════════════════════════════════════════
        // CẤU HÌNH GRID THEO TƯ DUY MỚI: XỬ LÝ THEO TỪNG DÒNG
        // ════════════════════════════════════════════════════════════════════

        private void ConfigureGrid()
        {
            bool isNhapKho = _mode == FrmDLCuonMode.NhapKho;

            grvThongTinCuonDay.AllowUserToAddRows = !isNhapKho;
            // Ở chế độ Nhập kho chỉ xoá qua nút Xoá để luôn kiểm soát việc không xoá dòng cuối cùng.
            grvThongTinCuonDay.AllowUserToDeleteRows = !isNhapKho;

            if (grvThongTinCuonDay.Columns.Contains("loai"))
                grvThongTinCuonDay.Columns["loai"].ReadOnly = isNhapKho;
            if (grvThongTinCuonDay.Columns.Contains("slCuon"))
                grvThongTinCuonDay.Columns["slCuon"].ReadOnly = false;
            if (grvThongTinCuonDay.Columns.Contains("tongChieuDai"))
                grvThongTinCuonDay.Columns["tongChieuDai"].ReadOnly = isNhapKho;
            if (grvThongTinCuonDay.Columns.Contains("soDau"))
                grvThongTinCuonDay.Columns["soDau"].ReadOnly = isNhapKho;
            if (grvThongTinCuonDay.Columns.Contains("soCuoi"))
                grvThongTinCuonDay.Columns["soCuoi"].ReadOnly = isNhapKho;
            if (grvThongTinCuonDay.Columns.Contains("ghiChu"))
                grvThongTinCuonDay.Columns["ghiChu"].ReadOnly = isNhapKho;

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

            if (_mode == FrmDLCuonMode.NhapKho)
            {
                SetCellReadonly(row, "loai", true);
                SetCellReadonly(row, "slCuon", false);
                SetCellReadonly(row, "tongChieuDai", true);
                SetCellReadonly(row, "soDau", true);
                SetCellReadonly(row, "soCuoi", true);
                SetCellReadonly(row, "ghiChu", true);
                return;
            }

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
                // - Dòng đã có TTCuonDay_CD_ID: giữ nguyên Số đầu/Số cuối lịch sử.
                // - Dòng mới từ btnDongGoi: Số đầu/Số cuối = NULL.
                // - Dòng mới từ ngữ cảnh khác: giữ nghiệp vụ cũ (0 -> Tổng CD).
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
                // - Tổng CD tự tính = |Số cuối - Số đầu| và bị khóa.
                SetCellReadonly(row, "tongChieuDai", true);
                SetCellReadonly(row, "soDau", false);
                SetCellReadonly(row, "soCuoi", false);

                if (autoCalculate)
                    AutoSetTongChieuDaiForLo(row);
            }
        }


        private static bool TryGetSourceId(DataGridViewRow row, out long sourceId)
        {
            sourceId = 0;
            return row != null
                && row.Tag != null
                && long.TryParse(row.Tag.ToString(), out sourceId)
                && sourceId > 0;
        }

        private void AutoSetSoDauSoCuoiForCuon(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;
            if (!IsCuonRow(row)) return;

            // Dữ liệu đã có trong DB phải được bảo toàn tuyệt đối.
            if (TryGetSourceId(row, out _))
                return;

            if (_nullSoDauSoCuoiChoCuonMoi)
            {
                row.Cells["soDau"].Value = null;
                row.Cells["soCuoi"].Value = null;
                return;
            }

            // Các ngữ cảnh khác giữ nguyên nghiệp vụ cũ cho dòng mới.
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

            // Chiều dài là khoảng cách giữa hai đầu, không phụ thuộc hướng cuốn.
            int tongCD = Math.Abs(soCuoi - soDau);
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
            if (_mode == FrmDLCuonMode.NhapKho) return;
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
        // CỘT ĐẢO CHIỀU
        // ════════════════════════════════════════════════════════════════════

        private void AddReverseButtonColumn()
        {
            if (grvThongTinCuonDay.Columns.Contains(COL_DAO_CHIEU)) return;

            var btnCol = new DataGridViewButtonColumn
            {
                Name = COL_DAO_CHIEU,
                HeaderText = "",
                Text = "Đảo chiều",
                UseColumnTextForButtonValue = true,
                Width = 95,
                FlatStyle = FlatStyle.Flat,
                Visible = _mode != FrmDLCuonMode.NhapKho
            };

            btnCol.DefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);
            btnCol.DefaultCellStyle.ForeColor = Color.DarkBlue;
            btnCol.DefaultCellStyle.Font = new Font("Tahoma", 9.75F, FontStyle.Bold);

            grvThongTinCuonDay.Columns.Add(btnCol);

            grvThongTinCuonDay.CellClick -= DataGridView1_CellClick_DaoChieu;
            grvThongTinCuonDay.CellClick += DataGridView1_CellClick_DaoChieu;
        }

        private void DataGridView1_CellClick_DaoChieu(object sender, DataGridViewCellEventArgs e)
        {
            if (_mode == FrmDLCuonMode.NhapKho) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!grvThongTinCuonDay.Columns.Contains(COL_DAO_CHIEU)) return;
            if (grvThongTinCuonDay.Columns[e.ColumnIndex].Name != COL_DAO_CHIEU) return;
            if (grvThongTinCuonDay.Rows[e.RowIndex].IsNewRow) return;

            grvThongTinCuonDay.EndEdit();

            DataGridViewRow row = grvThongTinCuonDay.Rows[e.RowIndex];

            // Cuộn không có Số đầu/Số cuối nên không có khái niệm đảo chiều.
            if (!IsLoRow(row)) return;
            if (!TryReadDgvCellInt(grvThongTinCuonDay, e.RowIndex, "soDau", out _)
                || !TryReadDgvCellInt(grvThongTinCuonDay, e.RowIndex, "soCuoi", out _))
                return;

            object soDauCu = row.Cells["soDau"].Value;
            object soCuoiCu = row.Cells["soCuoi"].Value;

            // Chặn các event tự tính trong lúc đổi 2 ô để tránh trạng thái trung gian
            // làm thay đổi Tổng chiều dài.
            bool loadingCu = _loadingGrid;
            _loadingGrid = true;
            try
            {
                row.Cells["soDau"].Value = soCuoiCu;
                row.Cells["soCuoi"].Value = soDauCu;
            }
            finally
            {
                _loadingGrid = loadingCu;
            }

            // Với Lô, luôn đồng bộ Tổng CD theo khoảng cách tuyệt đối giữa hai đầu.
            if (IsLoRow(row))
                AutoSetTongChieuDaiForLo(row);

            ResetCellColor(row.Cells["soDau"]);
            ResetCellColor(row.Cells["soCuoi"]);
            ResetCellColor(row.Cells["tongChieuDai"]);
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
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!grvThongTinCuonDay.Columns.Contains(COL_XOA)) return;
            if (grvThongTinCuonDay.Columns[e.ColumnIndex].Name != COL_XOA) return;
            if (grvThongTinCuonDay.Rows[e.RowIndex].IsNewRow) return;

            if (_mode == FrmDLCuonMode.NhapKho && DemSoDongDuLieu() <= 1)
            {
                FrmWaiting.ShowGifAlert("Phải giữ lại ít nhất 1 dòng cuộn/lô để nhập kho.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xoá dòng {e.RowIndex + 1} không?",
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm == DialogResult.Yes)
                grvThongTinCuonDay.Rows.RemoveAt(e.RowIndex);
        }

        private int DemSoDongDuLieu()
        {
            int count = 0;
            foreach (DataGridViewRow row in grvThongTinCuonDay.Rows)
            {
                if (!row.IsNewRow) count++;
            }
            return count;
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

            if (loaiValue > LOAI_CUON_VALUE && !_ttLoKichThuocById.ContainsKey(loaiValue))
            {
                MarkCellError(cell);
                MessageBox.Show(
                    $"Dòng {rowIndex + 1} – TTLo_ID = {loaiValue} không còn tồn tại trong bảng TTLo.\n" +
                    "Vui lòng chọn lại một loại lô hợp lệ.",
                    "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            if (_mode == FrmDLCuonMode.NhapKho)
            {
                if (colName == "slCuon")
                    ValidateSoCuonNhapKho(e.RowIndex, showMessage: true, out _);
                return;
            }

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
                AutoSetSoDauSoCuoiForCuon(row);
        }


        private bool ValidateSoCuonNhapKho(int rowIndex, bool showMessage, out int soCuon)
        {
            soCuon = 0;

            if (rowIndex < 0 || rowIndex >= grvThongTinCuonDay.Rows.Count)
                return false;

            DataGridViewRow row = grvThongTinCuonDay.Rows[rowIndex];
            if (row.IsNewRow) return true;

            DataGridViewCell cell = row.Cells["slCuon"];
            string raw = cell.Value?.ToString()?.Trim() ?? string.Empty;

            if (!int.TryParse(raw, out soCuon) || soCuon <= 0)
            {
                MarkCellError(cell);
                if (showMessage)
                {
                    FrmWaiting.ShowGifAlert($"Dòng {rowIndex + 1}: số lượng nhập phải là số nguyên lớn hơn 0.");
                    grvThongTinCuonDay.CurrentCell = cell;
                }
                return false;
            }

            if (row.Tag == null || !long.TryParse(row.Tag.ToString(), out long sourceId) || sourceId <= 0)
            {
                MarkCellError(cell);
                if (showMessage)
                    FrmWaiting.ShowGifAlert($"Dòng {rowIndex + 1}: không xác định được TTCuonDay_CD_ID nguồn.");
                return false;
            }

            if (!_soCuonToiDaNhapKho.TryGetValue(sourceId, out int soCuonToiDa) || soCuonToiDa <= 0)
            {
                MarkCellError(cell);
                if (showMessage)
                {
                    FrmWaiting.ShowGifAlert(
                        $"Dòng {rowIndex + 1}: dữ liệu nguồn đã thay đổi hoặc không còn số lượng để nhập. " +
                        "Vui lòng chọn lại MaBin để tải dữ liệu mới.");
                    grvThongTinCuonDay.CurrentCell = cell;
                }
                return false;
            }

            if (soCuon > soCuonToiDa)
            {
                MarkCellError(cell);
                if (showMessage)
                {
                    FrmWaiting.ShowGifAlert(
                        $"Dòng {rowIndex + 1}: số lượng nhập ({soCuon}) không được vượt quá số lượng còn lại ({soCuonToiDa}).");
                    grvThongTinCuonDay.CurrentCell = cell;
                }
                return false;
            }

            ResetCellColor(cell);
            return true;
        }

        private void LuuCheDoNhapKho()
        {
            if (DemSoDongDuLieu() == 0)
            {
                FrmWaiting.ShowGifAlert("Phải có ít nhất 1 dòng cuộn/lô để nhập kho.");
                return;
            }

            var originalsById = _thongTinCuonHienTai
                .Where(x => x != null && x.TTCuonDay_CD_ID.HasValue && x.TTCuonDay_CD_ID.Value > 0)
                .GroupBy(x => x.TTCuonDay_CD_ID.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var result = new List<ThongTinCuonDay>();

            for (int i = 0; i < grvThongTinCuonDay.Rows.Count; i++)
            {
                DataGridViewRow row = grvThongTinCuonDay.Rows[i];
                if (row.IsNewRow) continue;

                if (!ValidateSoCuonNhapKho(i, showMessage: true, out int soCuon))
                    return;

                if (row.Tag == null || !long.TryParse(row.Tag.ToString(), out long sourceId) || sourceId <= 0
                    || !originalsById.TryGetValue(sourceId, out ThongTinCuonDay original))
                {
                    FrmWaiting.ShowGifAlert(
                        $"Dòng {i + 1}: không tìm thấy dữ liệu nguồn tương ứng. Vui lòng chọn lại MaBin.");
                    return;
                }

                ThongTinCuonDay item = CloneThongTinCuonDay(original);
                item.SoCuon = soCuon;
                result.Add(item);
            }

            KetQua = result;
            ThongTinCuon = result;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ════════════════════════════════════════════════════════════════════
        // NÚT LƯU: CHỈ TRẢ DỮ LIỆU VỀ UC, CHƯA LƯU DB
        // ════════════════════════════════════════════════════════════════════

        private void btnLuuTTCuonDay_Click(object sender, EventArgs e)
        {
            grvThongTinCuonDay.EndEdit();

            if (_mode == FrmDLCuonMode.NhapKho)
            {
                LuuCheDoNhapKho();
                return;
            }

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

                long? sourceId = TryGetSourceId(row, out long parsedSourceId)
                    ? (long?)parsedSourceId
                    : null;

                int tongCD;
                int? soDau = null;
                int? soCuoi = null;

                if (!ttLoId.HasValue)
                {
                    // Cuộn:
                    // - Dòng đã có trong DB: giữ nguyên Số đầu/Số cuối được truyền vào.
                    // - Dòng mới từ btnDongGoi: NULL/NULL.
                    // - Dòng mới từ ngữ cảnh khác: giữ nghiệp vụ cũ 0 -> Tổng CD.
                    if (!ValidateDgvCellIsNonNegativeInt(
                        grvThongTinCuonDay, i, "tongChieuDai", "Tổng chiều dài", out tongCD))
                        return;

                    AutoSetSoDauSoCuoiForCuon(row);

                    if (sourceId.HasValue)
                    {
                        ThongTinCuonDay original = _thongTinCuonHienTai.FirstOrDefault(x =>
                            x != null
                            && x.TTCuonDay_CD_ID.HasValue
                            && x.TTCuonDay_CD_ID.Value == sourceId.Value);

                        if (original == null)
                        {
                            FrmWaiting.ShowGifAlert(
                                $"Dòng {i + 1}: không tìm thấy dữ liệu nguồn TTCuonDay_CD_ID={sourceId.Value}.");
                            return;
                        }

                        soDau = original.SoDau;
                        soCuoi = original.soCuoi;
                    }
                    else if (_nullSoDauSoCuoiChoCuonMoi)
                    {
                        soDau = null;
                        soCuoi = null;
                    }
                    else
                    {
                        if (!ValidateDgvCellIsNonNegativeInt(
                            grvThongTinCuonDay, i, "soDau", "Số đầu", out int parsedSoDau))
                            return;
                        if (!ValidateDgvCellIsNonNegativeInt(
                            grvThongTinCuonDay, i, "soCuoi", "Số cuối", out int parsedSoCuoi))
                            return;

                        soDau = parsedSoDau;
                        soCuoi = parsedSoCuoi;
                    }
                }
                else
                {
                    // Lô: hai đầu là bắt buộc, nhưng KHÔNG yêu cầu Số cuối > Số đầu.
                    if (!ValidateDgvCellIsNonNegativeInt(
                        grvThongTinCuonDay, i, "soDau", "Số đầu", out int parsedSoDau))
                        return;
                    if (!ValidateDgvCellIsNonNegativeInt(
                        grvThongTinCuonDay, i, "soCuoi", "Số cuối", out int parsedSoCuoi))
                        return;

                    soDau = parsedSoDau;
                    soCuoi = parsedSoCuoi;
                    tongCD = Math.Abs(parsedSoCuoi - parsedSoDau);
                    row.Cells["tongChieuDai"].Value = tongCD;
                }

                string kichThuocLo = string.Empty;
                bool ttLoHopLe = true;
                if (ttLoId.HasValue)
                {
                    ttLoHopLe = _ttLoKichThuocById.TryGetValue(ttLoId.Value, out kichThuocLo);
                    kichThuocLo = kichThuocLo?.Trim() ?? string.Empty;
                }

                result.Add(new ThongTinCuonDay
                {
                    TTCuonDay_CD_ID = sourceId,
                    TTLo_ID = ttLoId,
                    KichThuocLo = kichThuocLo,
                    TTLoHopLe = ttLoHopLe,
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
