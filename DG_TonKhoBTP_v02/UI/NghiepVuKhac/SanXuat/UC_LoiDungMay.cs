using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    public partial class UC_LoiDungMay : UserControl
    {
        private const string ColTenLoi = "colTenLoi";
        private const string ColThoiGianBatDau = "colThoiGianBatDau";
        private const string ColThoiGianKetThuc = "colThoiGianKetThuc";
        private const string ColGhiChu = "colGhiChu";
        private const string ColXoa = "colXoa";

        private readonly List<string> danhSachTenLoi = new List<string>();

        public UC_LoiDungMay()
        {
            InitializeComponent();
            CauHinhGridLoiDungMay();
        }

        /// <summary>
        /// Truyền danh sách tên lỗi cho ComboBox trong cột "Tên lỗi".
        /// Người dùng chỉ được chọn các giá trị có trong danh sách này.
        /// </summary>
        public void SetDanhSachLoi(List<string> danhSachLoi)
        {
            danhSachTenLoi.Clear();

            if (danhSachLoi != null)
            {
                danhSachTenLoi.AddRange(
                    danhSachLoi
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.CurrentCultureIgnoreCase));
            }

            DataGridViewComboBoxColumn colTenLoi =
                grvDsLoiDungMay.Columns[ColTenLoi] as DataGridViewComboBoxColumn;

            if (colTenLoi == null)
            {
                return;
            }

            // Nếu danh sách lỗi thay đổi sau khi đã nhập dữ liệu,
            // xoá các giá trị không còn hợp lệ để tránh lỗi ComboBoxCell.
            HashSet<string> validValues = new HashSet<string>(
                danhSachTenLoi,
                StringComparer.CurrentCultureIgnoreCase);

            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                object currentValue = row.Cells[ColTenLoi].Value;

                if (currentValue != null &&
                    !validValues.Contains(Convert.ToString(currentValue)))
                {
                    row.Cells[ColTenLoi].Value = null;
                }
            }

            colTenLoi.Items.Clear();

            if (danhSachTenLoi.Count > 0)
            {
                colTenLoi.Items.AddRange(
                    danhSachTenLoi.Cast<object>().ToArray());
            }
        }

        private void CauHinhGridLoiDungMay()
        {
            // ============================================================
            // CẤU HÌNH CHUNG
            // ============================================================
            grvDsLoiDungMay.AutoGenerateColumns = false;
            grvDsLoiDungMay.Columns.Clear();

            grvDsLoiDungMay.AllowUserToAddRows = true;
            grvDsLoiDungMay.AllowUserToDeleteRows = false;
            grvDsLoiDungMay.AllowUserToResizeRows = false;
            grvDsLoiDungMay.RowHeadersVisible = false;
            grvDsLoiDungMay.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grvDsLoiDungMay.MultiSelect = false;

            // Cell được chọn sẽ chuyển sang chế độ edit ngay.
            grvDsLoiDungMay.EditMode = DataGridViewEditMode.EditOnEnter;

            // ============================================================
            // HEADER - HIỂN THỊ 1 DÒNG
            // ============================================================
            grvDsLoiDungMay.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grvDsLoiDungMay.ColumnHeadersHeight = 35;

            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            // Không cho "Thời gian bắt đầu/kết thúc" tự xuống dòng.
            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.WrapMode =
                DataGridViewTriState.False;

            // FONT HEADER: thay 10F nếu muốn tăng/giảm cỡ chữ.
            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Bold);

            // FONT DỮ LIỆU: thay 10F nếu muốn tăng/giảm cỡ chữ.
            grvDsLoiDungMay.DefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Regular);

            grvDsLoiDungMay.RowTemplate.Height = 32;

            // ============================================================
            // CỘT 1: TÊN LỖI
            // ============================================================
            DataGridViewComboBoxColumn tenLoiColumn =
                new DataGridViewComboBoxColumn
                {
                    Name = ColTenLoi,
                    HeaderText = "Tên lỗi",
                    Width = 250,
                    FlatStyle = FlatStyle.Flat,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            // ============================================================
            // CỘT 2: THỜI GIAN BẮT ĐẦU
            // ============================================================
            DataGridViewTimeColumn batDauColumn =
                new DataGridViewTimeColumn
                {
                    Name = ColThoiGianBatDau,
                    HeaderText = "Thời gian bắt đầu",
                    Width = 175,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            // ============================================================
            // CỘT 3: THỜI GIAN KẾT THÚC
            // ============================================================
            DataGridViewTimeColumn ketThucColumn =
                new DataGridViewTimeColumn
                {
                    Name = ColThoiGianKetThuc,
                    HeaderText = "Thời gian kết thúc",
                    Width = 175,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            // ============================================================
            // CỘT 4: GHI CHÚ
            // ============================================================
            DataGridViewTextBoxColumn ghiChuColumn =
                new DataGridViewTextBoxColumn
                {
                    Name = ColGhiChu,
                    HeaderText = "Ghi chú",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    MinimumWidth = 220,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            // ============================================================
            // CỘT 5: XOÁ
            // ============================================================
            DataGridViewButtonColumn xoaColumn =
                new DataGridViewButtonColumn
                {
                    Name = ColXoa,
                    HeaderText = "Xoá",
                    Text = "Xoá",
                    UseColumnTextForButtonValue = true,
                    Width = 70,
                    FlatStyle = FlatStyle.Standard,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            grvDsLoiDungMay.Columns.AddRange(
                tenLoiColumn,
                batDauColumn,
                ketThucColumn,
                ghiChuColumn,
                xoaColumn);

            // ============================================================
            // EVENTS
            // ============================================================
            grvDsLoiDungMay.CellContentClick +=
                GrvDsLoiDungMay_CellContentClick;

            grvDsLoiDungMay.EditingControlShowing +=
                GrvDsLoiDungMay_EditingControlShowing;

            // Quan trọng: click một lần vào ô thời gian là bắt đầu edit ngay.
            grvDsLoiDungMay.CellMouseDown +=
                GrvDsLoiDungMay_CellMouseDown;
        }

        /// <summary>
        /// Click một lần vào cột thời gian:
        /// - chọn cell
        /// - mở editor ngay
        /// - DataGridViewTimeEditingControl sẽ tự tạo 00:00 nếu cell đang trống
        ///   và đưa focus về phần giờ.
        /// </summary>
        private void GrvDsLoiDungMay_CellMouseDown(
            object sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            string columnName = grvDsLoiDungMay.Columns[e.ColumnIndex].Name;

            bool isTimeColumn =
                columnName == ColThoiGianBatDau ||
                columnName == ColThoiGianKetThuc;

            if (!isTimeColumn)
            {
                return;
            }

            DataGridViewCell cell =
                grvDsLoiDungMay.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (cell.ReadOnly)
            {
                return;
            }

            // Đặt cell được click làm CurrentCell ngay từ MouseDown.
            grvDsLoiDungMay.CurrentCell = cell;

            // Ép DataGridView tạo DateTimePicker editor ngay trong click đầu tiên.
            grvDsLoiDungMay.BeginEdit(true);
        }

        /// <summary>
        /// Chỉ cho phép chọn Tên lỗi trong danh sách truyền vào.
        /// </summary>
        private void GrvDsLoiDungMay_EditingControlShowing(
            object sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            if (grvDsLoiDungMay.CurrentCell == null ||
                grvDsLoiDungMay.CurrentCell.OwningColumn == null ||
                grvDsLoiDungMay.CurrentCell.OwningColumn.Name != ColTenLoi)
            {
                return;
            }

            ComboBox comboBox = e.Control as ComboBox;

            if (comboBox != null)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        /// <summary>
        /// Click nút Xoá -> hỏi xác nhận -> Yes thì xoá đúng dòng.
        /// </summary>
        private void GrvDsLoiDungMay_CellContentClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (grvDsLoiDungMay.Columns[e.ColumnIndex].Name != ColXoa)
            {
                return;
            }

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];

            // Không xoá dòng NewRow tự động của DataGridView.
            if (row.IsNewRow)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xoá dòng lỗi dừng máy này không?",
                "Xác nhận xoá",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                grvDsLoiDungMay.Rows.RemoveAt(e.RowIndex);
            }
        }
    }
}
