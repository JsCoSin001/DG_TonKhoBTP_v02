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

            // Ghi chú: KHÔNG tự gọi BeginEdit() hay giả lập click chuột vào
            // DateTimePicker ở đây nữa (CellMouseDown/CellMouseUp trước đây).
            // EditMode = EditOnEnter đã tự mở editor khi click vào cell rồi;
            // gọi thêm BeginEdit()/giả lập chuột ngay trong lúc DataGridView
            // đang xử lý dở sự kiện chuột gốc gây lồng message (reentrant)
            // và làm cả form bị đơ. Việc focus vào field giờ (HH) được xử lý
            // an toàn trong GrvDsLoiDungMay_EditingControlShowing bên dưới,
            // sau khi editor đã sẵn sàng hoàn toàn.
        }

        /// <summary>
        /// - Cột "Tên lỗi": ép ComboBox ở chế độ chỉ chọn (không cho gõ tự do).
        /// - Cột thời gian: cell rỗng đã tự động được set 00:00 bởi
        ///   DataGridViewTimeCell/PrepareEditingControlForEdit (không cần code
        ///   thêm ở đây). Việc còn lại là focus vào field giờ (HH) để người
        ///   dùng gõ số ngay, thực hiện AN TOÀN bằng cách trì hoãn qua
        ///   BeginInvoke — chạy sau khi message loop của cú click gốc đã xử lý
        ///   xong, tránh gọi lồng (reentrant) làm treo form.
        /// </summary>
        private void GrvDsLoiDungMay_EditingControlShowing(
            object sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            if (grvDsLoiDungMay.CurrentCell == null ||
                grvDsLoiDungMay.CurrentCell.OwningColumn == null)
            {
                return;
            }

            string columnName = grvDsLoiDungMay.CurrentCell.OwningColumn.Name;

            if (columnName == ColTenLoi)
            {
                ComboBox comboBox = e.Control as ComboBox;

                if (comboBox != null)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                }

                return;
            }

            bool isTimeColumn =
                columnName == ColThoiGianBatDau ||
                columnName == ColThoiGianKetThuc;

            if (!isTimeColumn)
            {
                return;
            }

            DataGridViewTimeEditingControl timeControl =
                e.Control as DataGridViewTimeEditingControl;

            if (timeControl == null)
            {
                return;
            }

            timeControl.BeginInvoke(new MethodInvoker(() =>
            {
                if (!timeControl.IsDisposed && timeControl.IsHandleCreated)
                {
                    timeControl.FocusHourPart();
                }
            }));
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
