using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.ThanhPhamCD
{
    public partial class Frm_BCDungMay : Form
    {
        private const string ColTenLoi = "colTenLoi";
        private const string ColThoiGianDung = "colThoiGianDung";
        private const string ColGhiChu = "colGhiChu";
        private const string ColXoa = "colXoa";
        private const string TenLoiLamViecKhac = "Làm việc khác";

        private readonly ThongTinCaLamViec _thongTinCaLamViec;
        private readonly Dictionary<int, TenLoiDungMay_Model> _loiTheoId =
            new Dictionary<int, TenLoiDungMay_Model>();


        public List<DanhSachLoiDungMay_Model> DanhSachLoiDungMay { get; private set; }

        public Frm_BCDungMay(
            ThongTinCaLamViec thongTinCaLamViec,
            IEnumerable<DanhSachLoiDungMay_Model> draft)
        {
            if (thongTinCaLamViec == null)
                throw new ArgumentNullException(nameof(thongTinCaLamViec));

            _thongTinCaLamViec = thongTinCaLamViec;
            DanhSachLoiDungMay = CloneDanhSach(draft);

            InitializeComponent();
            Text = "BÁO CÁO NGUYÊN NHÂN DỪNG MÁY";
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = false;

            label1.Text = "BÁO CÁO NGUYÊN NHÂN DỪNG MÁY";

            button1.Text = "Đóng";
            button1.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            btnLuu.Click += btnLuu_Click;

            CauHinhGridLoiDungMay();
            Load += Frm_BCDungMay_Load;
        }



        private void CauHinhGridLoiDungMay()
        {
            grvDsLoiDungMay.AutoGenerateColumns = false;
            grvDsLoiDungMay.Columns.Clear();
            grvDsLoiDungMay.AllowUserToAddRows = true;
            grvDsLoiDungMay.AllowUserToDeleteRows = false;
            grvDsLoiDungMay.AllowUserToResizeRows = false;
            grvDsLoiDungMay.RowHeadersVisible = false;
            grvDsLoiDungMay.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grvDsLoiDungMay.MultiSelect = false;
            grvDsLoiDungMay.EditMode = DataGridViewEditMode.EditOnEnter;
            grvDsLoiDungMay.RowTemplate.Height = 32;

            var tenLoiColumn = new DataGridViewComboBoxColumn
            {
                Name = ColTenLoi,
                HeaderText = "Tên lỗi",
                Width = 300,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var thoiGianDungColumn = new DataGridViewTextBoxColumn
            {
                Name = ColThoiGianDung,
                HeaderText = "Thời gian dừng (phút)",
                Width = 190,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            thoiGianDungColumn.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            var ghiChuColumn = new DataGridViewTextBoxColumn
            {
                Name = ColGhiChu,
                HeaderText = "Ghi chú",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 250,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var xoaColumn = new DataGridViewButtonColumn
            {
                Name = ColXoa,
                HeaderText = "Xoá",
                Text = "Xoá",
                UseColumnTextForButtonValue = true,
                Width = 80,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            grvDsLoiDungMay.Columns.AddRange(
                tenLoiColumn,
                thoiGianDungColumn,
                ghiChuColumn,
                xoaColumn);

            grvDsLoiDungMay.CellContentClick += GrvDsLoiDungMay_CellContentClick;
            grvDsLoiDungMay.EditingControlShowing += GrvDsLoiDungMay_EditingControlShowing;
            grvDsLoiDungMay.CellValidated += GrvDsLoiDungMay_CellValidated;
            grvDsLoiDungMay.DataError += (s, e) => e.ThrowException = false;
        }

        private void Frm_BCDungMay_Load(object sender, EventArgs e)
        {
            try
            {
                if (_thongTinCaLamViec.Id <= 0)
                    throw new InvalidOperationException("Công đoạn không hợp lệ.");

                if (_thongTinCaLamViec.DanhSachMayId <= 0)
                    throw new InvalidOperationException("Vui lòng chọn máy trước khi nhập báo cáo dừng máy.");

                List<TenLoiDungMay_Model> danhSachLoi =
                    LoiDungMay_DB.GetDanhSachTenLoiTheoMaCongDoan(_thongTinCaLamViec.Id)
                    ?? new List<TenLoiDungMay_Model>();

                _loiTheoId.Clear();
                foreach (TenLoiDungMay_Model item in danhSachLoi)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.TenLoi))
                        continue;

                    _loiTheoId[item.Id] = item;
                }

                var colTenLoi = grvDsLoiDungMay.Columns[ColTenLoi] as DataGridViewComboBoxColumn;

                if (colTenLoi != null)
                {
                    var danhSachHienThi = new List<TenLoiDungMay_Model>
                    {
                        new TenLoiDungMay_Model
                        {
                            Id = 0,
                            TenLoi = "-- Vui lòng chọn lỗi --"
                        }
                    };

                    danhSachHienThi.AddRange(
                        _loiTheoId.Values
                            .OrderBy(x => x.Id));

                    colTenLoi.DisplayMember = nameof(TenLoiDungMay_Model.TenLoi);
                    colTenLoi.ValueMember = nameof(TenLoiDungMay_Model.Id);
                    colTenLoi.ValueType = typeof(int);

                    colTenLoi.DataSource = danhSachHienThi;

                    // Khi cell chưa có giá trị vẫn hiển thị hướng dẫn
                    colTenLoi.DefaultCellStyle.NullValue = "-- Vui lòng chọn lỗi --";
                }

                NapDraftLenGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void NapDraftLenGrid()
        {
            grvDsLoiDungMay.Rows.Clear();

            foreach (DanhSachLoiDungMay_Model item in DanhSachLoiDungMay)
            {
                int rowIndex = grvDsLoiDungMay.Rows.Add();
                DataGridViewRow row = grvDsLoiDungMay.Rows[rowIndex];

                row.Tag = CloneItem(item);
                row.Cells[ColTenLoi].Value = item.TenLoiDungMayId;
                row.Cells[ColThoiGianDung].Value = item.ThoiGianDung;
                row.Cells[ColGhiChu].Value = item.GhiChu;
            }
        }

        private void GrvDsLoiDungMay_EditingControlShowing(
         object sender,
         DataGridViewEditingControlShowingEventArgs e)
        {
            ComboBox combo = e.Control as ComboBox;
            if (combo == null)
                return;

            // Luôn gỡ trước để tránh đăng ký event nhiều lần
            combo.SelectionChangeCommitted -= ComboTenLoi_SelectionChangeCommitted;

            if (grvDsLoiDungMay.CurrentCell?.OwningColumn?.Name != ColTenLoi)
                return;

            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.SelectionChangeCommitted += ComboTenLoi_SelectionChangeCommitted;
        }

        private void ComboTenLoi_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (grvDsLoiDungMay.CurrentCell == null)
                return;

            int rowIndex = grvDsLoiDungMay.CurrentCell.RowIndex;

            if (rowIndex < 0 || rowIndex >= grvDsLoiDungMay.Rows.Count)
                return;

            BeginInvoke(new Action(() =>
            {
                DataGridViewRow row = grvDsLoiDungMay.Rows[rowIndex];

                if (row.IsNewRow)
                    return;

                grvDsLoiDungMay.CurrentCell = row.Cells[ColThoiGianDung];

                // Đưa ngay ô Thời gian dừng vào trạng thái nhập
                grvDsLoiDungMay.BeginEdit(true);
            }));
        }

        private void GrvDsLoiDungMay_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grvDsLoiDungMay.Rows.Count)
                return;

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow)
                return;

            ValidateRow(row, true, out _, out _, out _);
        }

        private void GrvDsLoiDungMay_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (grvDsLoiDungMay.Columns[e.ColumnIndex].Name != ColXoa)
                return;

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow)
                return;

            bool laDongCuoiCung = DemSoDongTrenGrid() == 1;
            string message = laDongCuoiCung
                ? "Nếu tiếp tục thì toàn bộ dữ liệu báo cáo dừng máy sẽ bị xoá khỏi draft."
                : "Bạn có chắc chắn muốn xoá dòng lỗi dừng máy này không?";

            DialogResult result = MessageBox.Show(
                message,
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
                grvDsLoiDungMay.Rows.RemoveAt(e.RowIndex);
        }

        private int DemSoDongTrenGrid()
        {
            return grvDsLoiDungMay.Rows
                .Cast<DataGridViewRow>()
                .Count(x => !x.IsNewRow && DongCoDuLieu(x));
        }

        private bool DongCoDuLieu(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
                return false;

            return row.Cells[ColTenLoi].Value != null
                || !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColThoiGianDung].Value))
                || !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColGhiChu].Value));
        }

        private bool ValidateRow(
            DataGridViewRow row,
            bool setError,
            out int tenLoiId,
            out int soPhutDung,
            out string ghiChu)
        {
            tenLoiId = 0;
            soPhutDung = 0;
            ghiChu = string.Empty;

            if (row == null || row.IsNewRow || !DongCoDuLieu(row))
                return true;

            ClearErrors(row);

            object tenLoiValue = row.Cells[ColTenLoi].Value;
            if (tenLoiValue == null
                || !int.TryParse(Convert.ToString(tenLoiValue), out tenLoiId)
                || !_loiTheoId.ContainsKey(tenLoiId))
            {
                SetError(row, ColTenLoi, "Tên lỗi không được để trống.", setError);
                return false;
            }

            string durationText =
                (Convert.ToString(row.Cells[ColThoiGianDung].Value) ?? string.Empty).Trim();
            if (!int.TryParse(durationText, out soPhutDung) || soPhutDung <= 0)
            {
                SetError(
                    row,
                    ColThoiGianDung,
                    "Thời gian dừng phải là số phút nguyên lớn hơn 0.",
                    setError);
                return false;
            }

            ghiChu = (Convert.ToString(row.Cells[ColGhiChu].Value) ?? string.Empty).Trim();
            if (IsLamViecKhac(_loiTheoId[tenLoiId].TenLoi) && string.IsNullOrWhiteSpace(ghiChu))
            {
                SetError(
                    row,
                    ColGhiChu,
                    "Khi chọn 'Làm việc khác' bắt buộc phải nhập ghi chú.",
                    setError);
                return false;
            }

            return true;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                grvDsLoiDungMay.EndEdit();
                var result = new List<DanhSachLoiDungMay_Model>();

                foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
                {
                    if (row.IsNewRow || !DongCoDuLieu(row))
                        continue;

                    int tenLoiId;
                    int soPhutDung;
                    string ghiChu;
                    if (!ValidateRow(row, true, out tenLoiId, out soPhutDung, out ghiChu))
                    {
                        throw new InvalidOperationException(
                            string.Format("Dòng {0} có dữ liệu chưa hợp lệ.", row.Index + 1));
                    }

                    DanhSachLoiDungMay_Model oldItem = row.Tag as DanhSachLoiDungMay_Model;
                    bool durationKhongDoi = oldItem != null && oldItem.ThoiGianDung == soPhutDung;

                    result.Add(new DanhSachLoiDungMay_Model
                    {
                        Id = oldItem?.Id ?? 0,
                        TenLoiDungMayId = tenLoiId,
                        TenLoi = _loiTheoId[tenLoiId].TenLoi,
                        DanhSachMayId = _thongTinCaLamViec.DanhSachMayId,
                        ThoiGianBatDau = durationKhongDoi ? oldItem?.ThoiGianBatDau : null,
                        ThoiGianKetThuc = durationKhongDoi ? oldItem?.ThoiGianKetThuc : null,
                        ThoiGianDung = soPhutDung,
                        GhiChu = ghiChu,
                        MaCongDoan = _thongTinCaLamViec.Id,
                        TTThanhPhamId = oldItem?.TTThanhPhamId
                    });
                }

                DanhSachLoiDungMay = result;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "KHÔNG THỂ LƯU", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static bool IsLamViecKhac(string tenLoi)
        {
            return string.Equals(
                (tenLoi ?? string.Empty).Trim(),
                TenLoiLamViecKhac,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SetError(
            DataGridViewRow row,
            string columnName,
            string message,
            bool setError)
        {
            if (setError)
                row.Cells[columnName].ErrorText = message;
        }

        private static void ClearErrors(DataGridViewRow row)
        {
            foreach (DataGridViewCell cell in row.Cells)
                cell.ErrorText = string.Empty;
        }

        private static List<DanhSachLoiDungMay_Model> CloneDanhSach(
            IEnumerable<DanhSachLoiDungMay_Model> source)
        {
            return source == null
                ? new List<DanhSachLoiDungMay_Model>()
                : source.Select(CloneItem).ToList();
        }

        private static DanhSachLoiDungMay_Model CloneItem(DanhSachLoiDungMay_Model item)
        {
            if (item == null)
                return new DanhSachLoiDungMay_Model();

            return new DanhSachLoiDungMay_Model
            {
                Id = item.Id,
                TenLoiDungMayId = item.TenLoiDungMayId,
                TenLoi = item.TenLoi,
                DanhSachMayId = item.DanhSachMayId,
                ThoiGianBatDau = item.ThoiGianBatDau,
                ThoiGianKetThuc = item.ThoiGianKetThuc,
                ThoiGianDung = item.ThoiGianDung,
                GhiChu = item.GhiChu,
                MaCongDoan = item.MaCongDoan,
                TTThanhPhamId = item.TTThanhPhamId
            };
        }
    }
}
