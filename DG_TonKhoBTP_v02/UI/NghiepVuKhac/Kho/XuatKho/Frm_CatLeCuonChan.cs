using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    public partial class Frm_CatLeCuonChan : Form
    {
        private readonly string _tenSP;
        private readonly int _chieuDai1Cuon;
        private readonly int _tonThucTe;
        private readonly int _datTruoc;
        private readonly int _soCuonNguyen;
        private readonly int _soCuonDaLay;
        private readonly List<LapKeHoachCatDay_NhomCat> _duLieuGoc;
        private readonly List<LapKeHoachCatDay_NhomCat> _duLieuLamViec;
        private bool _dangNapGrid;
        private string _giaTriTruocSua = string.Empty;

        internal List<LapKeHoachCatDay_NhomCat> KetQua { get; private set; } = new List<LapKeHoachCatDay_NhomCat>();

        /// <summary>Constructor để WinForms Designer hoạt động.</summary>
        public Frm_CatLeCuonChan()
            : this(string.Empty, 0, 0, 0, 0, 0, null)
        {
        }

        internal Frm_CatLeCuonChan(
            string tenSP,
            int chieuDai1Cuon,
            int tonThucTe,
            int datTruoc,
            int soCuonNguyen,
            int soCuonDaLay,
            IEnumerable<LapKeHoachCatDay_NhomCat> nhomCatHienTai)
        {
            InitializeComponent();

            _tenSP = (tenSP ?? string.Empty).Trim();
            _chieuDai1Cuon = chieuDai1Cuon;
            _tonThucTe = tonThucTe;
            _datTruoc = datTruoc;
            _soCuonNguyen = soCuonNguyen;
            _soCuonDaLay = soCuonDaLay;
            _duLieuGoc = (nhomCatHienTai ?? Enumerable.Empty<LapKeHoachCatDay_NhomCat>())
                .Select(x => x.Clone())
                .ToList();
            _duLieuLamViec = _duLieuGoc.Select(x => x.Clone()).ToList();

            CauHinhForm();
            GanSuKien();
            NapThongTinChung();
            NapGrid();
        }

        private void CauHinhForm()
        {
            // Giữ nguyên ClientSize được thiết kế trong Designer.
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            grvThongTinCatLe.AllowUserToAddRows = false;
            grvThongTinCatLe.AllowUserToDeleteRows = false;
            grvThongTinCatLe.MultiSelect = false;
            grvThongTinCatLe.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grvThongTinCatLe.RowHeadersVisible = false;
            grvThongTinCatLe.AutoGenerateColumns = false;

            grvThongTinCatLe.RowTemplate.Height = 33;

            var gridFont = new Font("Tahoma", 11F, FontStyle.Regular, GraphicsUnit.Point);
            grvThongTinCatLe.DefaultCellStyle.Font = gridFont;
            grvThongTinCatLe.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 11F, FontStyle.Regular, GraphicsUnit.Point);

            stt.ReadOnly = true;
            tongCD.ReadOnly = true;
            cdConLai.ReadOnly = true;
            btnXoa.Text = "Xóa";
            btnXoa.UseColumnTextForButtonValue = false;
            btnXoa.FlatStyle = FlatStyle.Flat;
            btnXoa.DefaultCellStyle.BackColor = Color.FromArgb(220, 53, 69);
            btnXoa.DefaultCellStyle.ForeColor = Color.White;
            btnXoa.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 53, 69);
            btnXoa.DefaultCellStyle.SelectionForeColor = Color.White;

            tbxChieuDai1C.ReadOnly = true;
            tbxTonThucTe.ReadOnly = true;
            tbxDatTruoc.ReadOnly = true;
            tbxSoCuonNguyen.ReadOnly = true;
        }

        private void GanSuKien()
        {
            grvThongTinCatLe.CellBeginEdit += GrvThongTinCatLe_CellBeginEdit;
            grvThongTinCatLe.CellEndEdit += GrvThongTinCatLe_CellEndEdit;
            grvThongTinCatLe.CellContentClick += GrvThongTinCatLe_CellContentClick;
            btnXacNhan.Click += BtnXacNhan_Click;
            btnReset.Click += BtnReset_Click;
        }

        private void NapThongTinChung()
        {
            lblTieuDe.Text = string.IsNullOrWhiteSpace(_tenSP)
                ? "KHAI BÁO CẮT LẺ"
                : "KHAI BÁO CẮT LẺ - " + _tenSP;

            tbxChieuDai1C.Text = _chieuDai1Cuon.ToString(CultureInfo.InvariantCulture);
            tbxTonThucTe.Text = _tonThucTe.ToString(CultureInfo.InvariantCulture);
            tbxDatTruoc.Text = _datTruoc.ToString(CultureInfo.InvariantCulture);
            tbxSoCuonNguyen.Text = _soCuonNguyen.ToString(CultureInfo.InvariantCulture);
            CapNhatTongHop();
        }

        private void NapGrid()
        {
            _dangNapGrid = true;
            try
            {
                grvThongTinCatLe.Rows.Clear();
                foreach (var group in _duLieuLamViec)
                    ThemDongDuLieu(group);
                ThemDongTrongCuoi();
                DanhLaiSTT();
                CapNhatTongHop();
            }
            finally
            {
                _dangNapGrid = false;
            }
        }

        private void ThemDongDuLieu(LapKeHoachCatDay_NhomCat group)
        {
            string chuoi = string.Join(";", group.ChiTiet
                .Where(x => !x.DaThucHien)
                .Select(x => x.ChieuDai.ToString(CultureInfo.InvariantCulture)));
            int tong = group.ChiTiet.Where(x => !x.DaThucHien).Sum(x => x.ChieuDai);
            int rowIndex = grvThongTinCatLe.Rows.Add(null, chuoi, tong, Math.Max(0, _chieuDai1Cuon - tong), "Xóa");
            grvThongTinCatLe.Rows[rowIndex].Tag = group;
            ApDungStyleNutXoa(grvThongTinCatLe.Rows[rowIndex], true);
        }

        private void ThemDongTrongCuoi()
        {
            int rowIndex = grvThongTinCatLe.Rows.Add(null, string.Empty, null, null, string.Empty);
            grvThongTinCatLe.Rows[rowIndex].Tag = null;
            ApDungStyleNutXoa(grvThongTinCatLe.Rows[rowIndex], false);
        }

        private void GrvThongTinCatLe_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != cdCat.Index)
                return;

            _giaTriTruocSua = Convert.ToString(grvThongTinCatLe.Rows[e.RowIndex].Cells[cdCat.Index].Value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private void GrvThongTinCatLe_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_dangNapGrid || e.RowIndex < 0 || e.ColumnIndex != cdCat.Index)
                return;

            DataGridViewRow gridRow = grvThongTinCatLe.Rows[e.RowIndex];
            string raw = Convert.ToString(gridRow.Cells[cdCat.Index].Value, CultureInfo.InvariantCulture) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                // Dòng trống cuối được phép rỗng. Với dòng đã có dữ liệu, muốn loại bỏ phải dùng nút Xóa.
                if (gridRow.Tag != null)
                    gridRow.Cells[cdCat.Index].Value = _giaTriTruocSua;
                return;
            }

            if (!TryParseChieuDai(raw, out string normalized, out List<int> values))
            {
                gridRow.Cells[cdCat.Index].Value = _giaTriTruocSua;
                return;
            }

            if (values.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Không xác định được chiều dài cần cắt. Vui lòng kiểm tra lại dữ liệu đã nhập.");
                gridRow.Cells[cdCat.Index].Value = _giaTriTruocSua;
                return;
            }

            int tong = values.Sum();
            if (_chieuDai1Cuon <= 0 || tong > _chieuDai1Cuon)
            {
                FrmWaiting.ShowGifAlert("Tổng chiều dài các đoạn cắt không hợp lệ");
                gridRow.Cells[cdCat.Index].Value = _giaTriTruocSua;
                return;
            }

            bool laDongMoi = gridRow.Tag == null;
            int soNhomSauKhiLuu = _duLieuLamViec.Count + (laDongMoi ? 1 : 0);
            if (!KiemTraSoCuonKhaDung(soNhomSauKhiLuu))
            {
                FrmWaiting.ShowGifAlert("Số cuộn khả dụng không đủ");
                gridRow.Cells[cdCat.Index].Value = _giaTriTruocSua;
                return;
            }

            LapKeHoachCatDay_NhomCat group;
            if (laDongMoi)
            {
                group = new LapKeHoachCatDay_NhomCat();
                _duLieuLamViec.Add(group);
                gridRow.Tag = group;
            }
            else
            {
                group = (LapKeHoachCatDay_NhomCat)gridRow.Tag;
            }

            group.ChiTiet = values.Select(x => new LapKeHoachCatDay_ChiTietCat
            {
                ChieuDai = x,
                DaThucHien = false
            }).ToList();

            gridRow.Cells[cdCat.Index].Value = normalized;
            gridRow.Cells[tongCD.Index].Value = tong;
            gridRow.Cells[cdConLai.Index].Value = Math.Max(0, _chieuDai1Cuon - tong);
            gridRow.Cells[btnXoa.Index].Value = "Xóa";
            ApDungStyleNutXoa(gridRow, true);

            if (laDongMoi)
                ThemDongTrongCuoi();

            DanhLaiSTT();
            CapNhatTongHop();
        }

        private void GrvThongTinCatLe_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != btnXoa.Index)
                return;

            DataGridViewRow gridRow = grvThongTinCatLe.Rows[e.RowIndex];
            if (!(gridRow.Tag is LapKeHoachCatDay_NhomCat group))
                return;

            DialogResult answer = MessageBox.Show(
                "Bạn muốn xóa cuộn cắt lẻ này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            // Chỉ thao tác collection trong memory. Không có câu lệnh DB trong form này.
            _duLieuLamViec.Remove(group);
            grvThongTinCatLe.Rows.RemoveAt(e.RowIndex);
            DamBaoDongTrongCuoi();
            DanhLaiSTT();
            CapNhatTongHop();
        }

        private void BtnXacNhan_Click(object sender, EventArgs e)
        {
            if (!ValidateTatCaDong())
                return;

            if (!KiemTraSoCuonKhaDung(_duLieuLamViec.Count))
            {
                FrmWaiting.ShowGifAlert("Số cuộn khả dụng không đủ");
                return;
            }

            KetQua = _duLieuLamViec.Select(x => x.Clone()).ToList();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            // Caller chỉ nhận KetQua khi DialogResult.OK. Hủy luôn giữ nguyên dữ liệu caller.
            KetQua = _duLieuGoc.Select(x => x.Clone()).ToList();
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidateTatCaDong()
        {
            foreach (DataGridViewRow row in grvThongTinCatLe.Rows)
            {
                if (row.Tag == null)
                    continue;

                string raw = Convert.ToString(row.Cells[cdCat.Index].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!TryParseChieuDai(raw, out string normalized, out List<int> values))
                    return false;

                if (values.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không xác định được chiều dài cần cắt. Vui lòng kiểm tra lại dữ liệu đã nhập.");
                    return false;
                }

                if (_chieuDai1Cuon <= 0 || values.Sum() > _chieuDai1Cuon)
                {
                    FrmWaiting.ShowGifAlert("Tổng chiều dài các đoạn cắt không hợp lệ");
                    return false;
                }

                row.Cells[cdCat.Index].Value = normalized;
            }

            return true;
        }

        private bool TryParseChieuDai(string raw, out string normalized, out List<int> values)
        {
            bool ok = LapKeHoachCatDay_ChieuDaiParser.TryNormalize(
                raw,
                out normalized,
                out values,
                out string invalid,
                out bool syntaxError);

            if (ok)
                return true;

            FrmWaiting.ShowGifAlert(LapKeHoachCatDay_ChieuDaiParser.TaoThongBaoLoi(invalid, syntaxError));

            return false;
        }

        private bool KiemTraSoCuonKhaDung(int soCuonCatLe)
        {
            int soCuonNguyenConPhaiLay = Math.Max(0, _soCuonNguyen - _soCuonDaLay);
            int khaDung = Math.Max(0, _tonThucTe - _datTruoc);
            return soCuonNguyenConPhaiLay + soCuonCatLe <= khaDung;
        }

        private void ApDungStyleNutXoa(DataGridViewRow row, bool enabled)
        {
            DataGridViewCell cell = row.Cells[btnXoa.Index];
            Color backColor = enabled ? Color.FromArgb(220, 53, 69) : Color.FromArgb(200, 200, 200);
            Color foreColor = enabled ? Color.White : Color.FromArgb(100, 100, 100);
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
            cell.Style.SelectionBackColor = backColor;
            cell.Style.SelectionForeColor = foreColor;
            cell.ReadOnly = !enabled;
            if (!enabled)
                cell.Value = string.Empty;
        }

        private void DamBaoDongTrongCuoi()
        {
            if (grvThongTinCatLe.Rows.Count == 0 || grvThongTinCatLe.Rows[grvThongTinCatLe.Rows.Count - 1].Tag != null)
                ThemDongTrongCuoi();
        }

        private void DanhLaiSTT()
        {
            int sttValue = 1;
            foreach (DataGridViewRow row in grvThongTinCatLe.Rows)
            {
                if (row.Tag is LapKeHoachCatDay_NhomCat)
                    row.Cells[stt.Index].Value = sttValue++;
                else
                    row.Cells[stt.Index].Value = string.Empty;
            }
        }

        private void CapNhatTongHop()
        {
            int soCuonCatLe = _duLieuLamViec.Count;
            lblTongCuonCatLe.Text = "Tổng cuộn cắt lẻ: " + soCuonCatLe.ToString(CultureInfo.InvariantCulture) + " cuộn";
            lblTongCuonSuDung.Text = "Tổng số cuộn sử dụng: " + (_soCuonNguyen + soCuonCatLe).ToString(CultureInfo.InvariantCulture) + " cuộn";
        }
    }
}
