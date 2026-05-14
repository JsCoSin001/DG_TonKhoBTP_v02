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

        // ── Constructor ──────────────────────────────────────────────────────────
        public UC_XuatKho()
        {
            InitializeComponent();
            InitLotSearch();
            InitGridFont();
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

            // Cột hiển thị trong dropdown → MaBin của TTThanhPham
            _lotSearchHelper.DisplayColumn = "MaBin";

            _lotSearchHelper.ItemSelected += OnLotSelected;
            _lotSearchHelper.Cleared += OnLotCleared;
        }

        // ── Khi chọn một LOT từ dropdown ─────────────────────────────────────────
        private void OnLotSelected(DataRowView row)
        {
            // Điền các TextBox thông tin cơ bản
            tbLot.Text = row["MaBin"]?.ToString() ?? string.Empty;
            tbTenSP.Text = row["Ten"]?.ToString() ?? string.Empty;

            _selectedTTThanhPhamID = long.TryParse(
                row["TTThanhPham_ID"]?.ToString(), out long id) ? id : (long?)null;

            // Nạp dữ liệu vào dgvLayDL
            if (_selectedTTThanhPhamID.HasValue)
                LoadCuonDayGrid(_selectedTTThanhPhamID.Value);
        }

        // ── Khi xoá / reset combobox ─────────────────────────────────────────────
        private void OnLotCleared()
        {
            tbLot.Text = string.Empty;
            tbTenSP.Text = string.Empty;
            _selectedTTThanhPhamID = null;
            dgvLayDL.Rows.Clear();
        }

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

                    r.Cells["ttNhapKho_ID"].Value = dr["TTNhapKho_ID"];
                    r.Cells["tongCD"].Value = dr["TongChieuDai"];
                    r.Cells["soCuon"].Value = dr["SoCuon"];
                    r.Cells["soDau"].Value = dr["SoDau"];
                    r.Cells["soCuoi"].Value = dr["SoCuoi"];
                    r.Cells["ghiChu"].Value = dr["GhiChu"];

                    // Các cột _user: để trống, user tự điền
                    r.Cells["soCuon_user"].Value = string.Empty;
                    r.Cells["soDau_user"].Value = string.Empty;
                    r.Cells["socuoi_user"].Value = string.Empty;
                    r.Cells["ghiChu_user"].Value = string.Empty;
                    r.Cells["getAll"].Value = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu cuộn/dây:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // RESET FORM
        // ════════════════════════════════════════════════════════════════════════

        private void ResetForm()
        {
            _lotSearchHelper.Reset();   // xoá combobox → trigger OnLotCleared
            dgvLayDL.Rows.Clear();
            dtNgayXuatKho.Value = DateTime.Today;
        }

        // ════════════════════════════════════════════════════════════════════════
        // FONT GRID
        // ════════════════════════════════════════════════════════════════════════

        private void InitGridFont()
        {
            Font gridFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            Font headerFont = new Font("Tahoma", 11.25F, FontStyle.Regular, GraphicsUnit.Point);

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