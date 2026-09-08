using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models.SanXuat;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    public partial class UC_LoiDungMay : UserControl
    {
        private const string ColTenLoi = "colTenLoi";
        private const string ColThoiGianBatDau = "colThoiGianBatDau";
        private const string ColThoiGianKetThuc = "colThoiGianKetThuc";
        private const string ColThoiGianDung = "colThoiGianDung";
        private const string ColGhiChu = "colGhiChu";
        private const string ColXoa = "colXoa";
        private const string TenLoiLamViecKhac = "Làm việc khác";

        private readonly List<TenLoiDungMay_Model> danhSachLoiHienTai =
            new List<TenLoiDungMay_Model>();

        private readonly Dictionary<int, TenLoiDungMay_Model> loiTheoId =
            new Dictionary<int, TenLoiDungMay_Model>();

        private readonly Dictionary<int, string> nguoiLamDaLuuTheoId =
            new Dictionary<int, string>();

        private string nguoiLamBanDauKhiTai = string.Empty;
        private bool dangNapDanhMuc;
        private bool dangNapDuLieuTheoToHop;
        private bool dangChinhSuaDuLieuDaLuu;

        public UC_LoiDungMay()
        {
            InitializeComponent();
            CauHinhGridLoiDungMay();

            Load += UC_LoiDungMay_Load;
            congDoan.SelectedIndexChanged += CongDoan_SelectedIndexChanged;
            cbMay.SelectedIndexChanged += ThongTinToHop_SelectedValueChanged;
            ca.SelectedIndexChanged += ThongTinToHop_SelectedValueChanged;
            ngay.ValueChanged += ThongTinToHop_SelectedValueChanged;
        }

        private async void UC_LoiDungMay_Load(object sender, EventArgs e)
        {
            ngay.Value = DateTime.Today;
            ca.SelectedIndex = -1;
            await NapDanhSachCongDoanAsync();
        }

        private async Task NapDanhSachCongDoanAsync()
        {
            dangNapDanhMuc = true;
            Enabled = false;

            try
            {
                List<DanhSachCongDoan_Model> danhSachCongDoan =
                    await WaitingHelper.RunWithWaiting<List<DanhSachCongDoan_Model>>(
                        async () => await Task.Run(
                            () => LoiDungMay_DB.GetDanhSachCongDoanCoMay()),
                        "ĐANG TẢI DANH SÁCH CÔNG ĐOẠN...");

                congDoan.DataSource = null;
                congDoan.DisplayMember = nameof(DanhSachCongDoan_Model.TenCongDoan);
                congDoan.ValueMember = nameof(DanhSachCongDoan_Model.MaCongDoan);
                congDoan.DataSource = danhSachCongDoan;
                congDoan.SelectedIndex = -1;

                ResetDuLieuTheoCongDoan();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh mục lỗi dừng máy.\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
                dangNapDanhMuc = false;
            }
        }

        private async void CongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dangNapDanhMuc)
            {
                return;
            }

            dangNapDanhMuc = true;

            try
            {
                ResetDuLieuTheoCongDoan();

                DanhSachCongDoan_Model congDoanDaChon =
                    congDoan.SelectedItem as DanhSachCongDoan_Model;

                if (congDoanDaChon == null)
                {
                    return;
                }

                int maCongDoan = congDoanDaChon.MaCongDoan;
                Enabled = false;

                Tuple<List<DanhSachMay_Model>, List<TenLoiDungMay_Model>> duLieuDanhMuc =
                    await WaitingHelper.RunWithWaiting<
                        Tuple<List<DanhSachMay_Model>, List<TenLoiDungMay_Model>>>(
                        async () => await Task.Run(() =>
                        {
                            List<DanhSachMay_Model> danhSachMay =
                                DanhSachMayHelper.LayTheoMaCongDoan(maCongDoan);

                            List<TenLoiDungMay_Model> danhSachLoi =
                                LoiDungMay_DB.GetDanhSachTenLoiTheoMaCongDoan(maCongDoan);

                            return Tuple.Create(danhSachMay, danhSachLoi);
                        }),
                        "ĐANG TẢI DANH MỤC THEO CÔNG ĐOẠN...");

                cbMay.DisplayMember = nameof(DanhSachMay_Model.TenMay);
                cbMay.ValueMember = nameof(DanhSachMay_Model.Id);
                cbMay.DataSource = duLieuDanhMuc.Item1;
                cbMay.SelectedIndex = -1;

                SetDanhSachLoi(duLieuDanhMuc.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh mục theo công đoạn.\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
                dangNapDanhMuc = false;
            }
        }

        private void ResetDuLieuTheoCongDoan()
        {
            cbMay.DataSource = null;
            cbMay.Items.Clear();
            cbMay.SelectedIndex = -1;

            grvDsLoiDungMay.Rows.Clear();
            nguoiLam.Clear();
            nguoiLamDaLuuTheoId.Clear();
            nguoiLamBanDauKhiTai = string.Empty;
            dangChinhSuaDuLieuDaLuu = false;
            SetDanhSachLoi(new List<TenLoiDungMay_Model>());
        }

        /// <summary>
        /// Cột Tên lỗi bind trực tiếp ID từ TenLoiDungMay.
        /// Danh mục hiển thị hoàn toàn lấy từ TenLoiDungMay trong database.
        /// </summary>
        private void SetDanhSachLoi(List<TenLoiDungMay_Model> danhSachLoi)
        {
            danhSachLoiHienTai.Clear();
            loiTheoId.Clear();

            if (danhSachLoi != null)
            {
                foreach (TenLoiDungMay_Model item in danhSachLoi)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.TenLoi))
                    {
                        continue;
                    }

                    danhSachLoiHienTai.Add(item);
                    loiTheoId[item.Id] = item;
                }
            }

            DataGridViewComboBoxColumn colTenLoi =
                grvDsLoiDungMay.Columns[ColTenLoi] as DataGridViewComboBoxColumn;

            if (colTenLoi == null)
            {
                return;
            }

            colTenLoi.DataSource = null;
            colTenLoi.DisplayMember = nameof(TenLoiDungMay_Model.TenLoi);
            colTenLoi.ValueMember = nameof(TenLoiDungMay_Model.Id);
            colTenLoi.ValueType = typeof(int);
            colTenLoi.DataSource = new List<TenLoiDungMay_Model>(danhSachLoiHienTai);
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

            grvDsLoiDungMay.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grvDsLoiDungMay.ColumnHeadersHeight = 35;
            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.WrapMode =
                DataGridViewTriState.False;
            grvDsLoiDungMay.ColumnHeadersDefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Bold);
            grvDsLoiDungMay.DefaultCellStyle.Font =
                new Font("Tahoma", 10F, FontStyle.Regular);
            grvDsLoiDungMay.RowTemplate.Height = 32;

            DataGridViewComboBoxColumn tenLoiColumn =
                new DataGridViewComboBoxColumn
                {
                    Name = ColTenLoi,
                    HeaderText = "Tên lỗi",
                    Width = 235,
                    FlatStyle = FlatStyle.Flat,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            DataGridViewTimeColumn batDauColumn =
                new DataGridViewTimeColumn
                {
                    Name = ColThoiGianBatDau,
                    HeaderText = "Thời gian bắt đầu",
                    Width = 170,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            DataGridViewTimeColumn ketThucColumn =
                new DataGridViewTimeColumn
                {
                    Name = ColThoiGianKetThuc,
                    HeaderText = "Thời gian kết thúc",
                    Width = 170,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

            DataGridViewTextBoxColumn thoiGianDungColumn =
                new DataGridViewTextBoxColumn
                {
                    Name = ColThoiGianDung,
                    HeaderText = "Thời gian dừng",
                    Width = 135,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
            thoiGianDungColumn.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
            thoiGianDungColumn.DefaultCellStyle.BackColor = SystemColors.Control;

            DataGridViewTextBoxColumn ghiChuColumn =
                new DataGridViewTextBoxColumn
                {
                    Name = ColGhiChu,
                    HeaderText = "Ghi chú",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    MinimumWidth = 220,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

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
                thoiGianDungColumn,
                ghiChuColumn,
                xoaColumn);

            grvDsLoiDungMay.CellContentClick += GrvDsLoiDungMay_CellContentClick;
            grvDsLoiDungMay.EditingControlShowing += GrvDsLoiDungMay_EditingControlShowing;
            grvDsLoiDungMay.CellValueChanged += GrvDsLoiDungMay_CellValueChanged;
            grvDsLoiDungMay.CellValidated += GrvDsLoiDungMay_CellValidated;
            grvDsLoiDungMay.RowValidated += GrvDsLoiDungMay_RowValidated;
            grvDsLoiDungMay.CurrentCellDirtyStateChanged += GrvDsLoiDungMay_CurrentCellDirtyStateChanged;
            grvDsLoiDungMay.DataError += GrvDsLoiDungMay_DataError;
        }

        private void GrvDsLoiDungMay_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grvDsLoiDungMay.IsCurrentCellDirty)
            {
                grvDsLoiDungMay.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void GrvDsLoiDungMay_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dangNapDuLieuTheoToHop)
            {
                return;
            }

            if (e.RowIndex < 0 || e.RowIndex >= grvDsLoiDungMay.Rows.Count)
            {
                return;
            }

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow)
            {
                return;
            }

            if (e.ColumnIndex >= 0)
            {
                string colName = grvDsLoiDungMay.Columns[e.ColumnIndex].Name;

                // Cột duration được code tự tính. Bỏ qua event của chính cột này
                // để tránh vòng lặp CellValueChanged -> tính duration -> CellValueChanged.
                if (colName == ColThoiGianDung)
                {
                    return;
                }

                if (colName == ColThoiGianBatDau || colName == ColThoiGianKetThuc)
                {
                    CapNhatThoiGianDung(row);
                }
            }

            KiemTraDong(row, true, out _, out _, out _, out _, out _, out _);
        }

        private void GrvDsLoiDungMay_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (dangNapDuLieuTheoToHop)
            {
                return;
            }

            if (e.RowIndex < 0 || e.RowIndex >= grvDsLoiDungMay.Rows.Count)
            {
                return;
            }

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow)
            {
                return;
            }

            CapNhatThoiGianDung(row);
            KiemTraDong(row, true, out _, out _, out _, out _, out _, out _);
            KiemTraOverlapTrongGridVaDanhDau(row.Index);
        }

        private void GrvDsLoiDungMay_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (dangNapDuLieuTheoToHop)
            {
                return;
            }

            if (e.RowIndex < 0 || e.RowIndex >= grvDsLoiDungMay.Rows.Count)
            {
                return;
            }

            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow)
            {
                return;
            }

            CapNhatThoiGianDung(row);
            KiemTraDong(row, true, out _, out _, out _, out _, out _, out _);
            KiemTraOverlapTrongGridVaDanhDau(row.Index);
        }

        private void GrvDsLoiDungMay_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // ComboBox chỉ cho phép ID có trong danh mục. Nếu DataSource đổi,
            // không để exception binding làm văng màn hình; Save vẫn validate lại.
            e.ThrowException = false;
        }

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

        private async void GrvDsLoiDungMay_CellContentClick(
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

            if (row.IsNewRow)
            {
                return;
            }

            bool laDongCuoiCung = DemSoDongTrenGrid() == 1;

            // Khi đang chỉnh sửa dữ liệu đã có trong DB, nếu người dùng định xoá
            // dòng cuối cùng thì phải xác nhận trước khi remove khỏi GridView.
            if (dangChinhSuaDuLieuDaLuu && laDongCuoiCung)
            {
                DialogResult xoaToanBo = MessageBox.Show(
                    "Nếu tiếp tục thì toàn bộ dữ liệu sẽ bị xoá hết.",
                    "Xác nhận xoá toàn bộ dữ liệu",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (xoaToanBo != DialogResult.Yes)
                {
                    return;
                }

                int mayId;
                int caValue;

                if (!TryGetMayId(out mayId) || !TryGetCa(out caValue))
                {
                    MessageBox.Show(
                        "Không xác định được Máy/Ca để xoá dữ liệu.",
                        "Không thể xoá dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    DateTime ngayDaChon = ngay.Value.Date;
                    Enabled = false;

                    await WaitingHelper.RunWithWaiting<bool>(
                        async () => await Task.Run(() =>
                        {
                            LoiDungMay_DB.XoaToanBoTheoMayNgayCa(
                                ngayDaChon,
                                mayId,
                                caValue);
                            return true;
                        }),
                        "ĐANG XOÁ DỮ LIỆU LỖI DỪNG MÁY...");

                    ResetToanBoForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Không thể xoá toàn bộ dữ liệu.\n" + ex.Message,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Enabled = true;
                }

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
                DanhDauLaiOverlapTrongGrid();
            }
        }

        private int DemSoDongTrenGrid()
        {
            int count = 0;

            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (!row.IsNewRow)
                {
                    count++;
                }
            }

            return count;
        }

        private async void ThongTinToHop_SelectedValueChanged(object sender, EventArgs e)
        {
            if (dangNapDanhMuc || dangNapDuLieuTheoToHop)
            {
                return;
            }

            await TaiDuLieuDaLuuTheoToHopAsync();
        }

        /// <summary>
        /// Khi đã chọn đủ Ngày + Máy + Ca, tự động lấy dữ liệu đã lưu và đưa lên grid.
        /// Không có dữ liệu thì giữ grid/người làm ở trạng thái trống để nhập mới.
        /// </summary>
        private async Task TaiDuLieuDaLuuTheoToHopAsync()
        {
            dangNapDuLieuTheoToHop = true;

            try
            {
                grvDsLoiDungMay.Rows.Clear();
                nguoiLam.Clear();
                nguoiLamDaLuuTheoId.Clear();
                nguoiLamBanDauKhiTai = string.Empty;
                dangChinhSuaDuLieuDaLuu = false;

                int maCongDoan;
                int mayId;
                int caValue;

                if (!TryGetMaCongDoan(out maCongDoan) ||
                    !TryGetMayId(out mayId) ||
                    !TryGetCa(out caValue))
                {
                    return;
                }

                DateTime ngayDaChon = ngay.Value.Date;
                Enabled = false;

                List<DanhSachLoiDungMay_Model> danhSach =
                    await WaitingHelper.RunWithWaiting<List<DanhSachLoiDungMay_Model>>(
                        async () => await Task.Run(
                            () => LoiDungMay_DB.GetDanhSachDaLuuTheoMayNgayCa(
                                ngayDaChon,
                                mayId,
                                caValue)),
                        "ĐANG TẢI DỮ LIỆU LỖI DỪNG MÁY...");

                if (danhSach == null || danhSach.Count == 0)
                {
                    return;
                }

                // Không load một phần dữ liệu lỗi. Nếu có bất kỳ bản ghi nào không
                // còn phù hợp với công đoạn/danh mục hiện tại thì dừng toàn bộ.
                foreach (DanhSachLoiDungMay_Model item in danhSach)
                {
                    if (item.MaCongDoan != maCongDoan)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Bản ghi id={0} không thuộc công đoạn đang chọn.",
                                item.Id));
                    }

                    if (!loiTheoId.ContainsKey(item.TenLoiDungMayId))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Bản ghi id={0} tham chiếu lỗi dừng máy không còn hợp lệ trong công đoạn này.",
                                item.Id));
                    }

                    if (string.IsNullOrWhiteSpace(item.NguoiLam))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Bản ghi id={0} có Người làm không hợp lệ.",
                                item.Id));
                    }

                    TenLoiDungMay_Model tenLoi = loiTheoId[item.TenLoiDungMayId];
                    if (IsLamViecKhac(tenLoi.TenLoi) &&
                        string.IsNullOrWhiteSpace(item.GhiChu))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Bản ghi id={0}: 'Làm việc khác' nhưng chưa có ghi chú.",
                                item.Id));
                    }
                }

                for (int i = 0; i < danhSach.Count; i++)
                {
                    for (int j = i + 1; j < danhSach.Count; j++)
                    {
                        if (HaiKhoangThoiGianChongLan(
                            danhSach[i].Ngay,
                            danhSach[i].ThoiGianBatDau,
                            danhSach[i].ThoiGianKetThuc,
                            danhSach[j].Ngay,
                            danhSach[j].ThoiGianBatDau,
                            danhSach[j].ThoiGianKetThuc,
                            caValue))
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "Bản ghi id={0} và id={1} có thời gian dừng máy chồng lấn nhau.",
                                    danhSach[i].Id,
                                    danhSach[j].Id));
                        }
                    }
                }

                // Theo nghiệp vụ đã chốt: nếu dữ liệu cũ có nhiều Người làm khác
                // nhau thì lấy Người làm của dòng đầu tiên.
                nguoiLamBanDauKhiTai = danhSach[0].NguoiLam ?? string.Empty;
                nguoiLam.Text = nguoiLamBanDauKhiTai;

                foreach (DanhSachLoiDungMay_Model item in danhSach)
                {
                    nguoiLamDaLuuTheoId[item.Id] = item.NguoiLam ?? string.Empty;
                    int rowIndex = grvDsLoiDungMay.Rows.Add();
                    DataGridViewRow row = grvDsLoiDungMay.Rows[rowIndex];

                    // ID chỉ lưu nội bộ để xác định UPDATE/DELETE, không hiển thị.
                    row.Tag = item.Id;
                    row.Cells[ColTenLoi].Value = item.TenLoiDungMayId;
                    row.Cells[ColThoiGianBatDau].Value = item.ThoiGianBatDau;
                    row.Cells[ColThoiGianKetThuc].Value = item.ThoiGianKetThuc;
                    row.Cells[ColThoiGianDung].Value =
                        string.Format("{0} phút", item.ThoiGianDung);
                    row.Cells[ColGhiChu].Value = item.GhiChu;
                }

                dangChinhSuaDuLieuDaLuu = true;
                DanhDauLaiOverlapTrongGrid();
            }
            catch (Exception ex)
            {
                grvDsLoiDungMay.Rows.Clear();
                nguoiLam.Clear();
                nguoiLamDaLuuTheoId.Clear();
                nguoiLamBanDauKhiTai = string.Empty;
                dangChinhSuaDuLieuDaLuu = false;

                MessageBox.Show(
                    "Không thể tải dữ liệu lỗi dừng máy đã lưu.\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
                dangNapDuLieuTheoToHop = false;
            }
        }

        private void CapNhatThoiGianDung(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
            {
                return;
            }

            TimeSpan batDau;
            TimeSpan ketThuc;
            int caValue;

            if (!TryGetTime(row.Cells[ColThoiGianBatDau].Value, out batDau) ||
                !TryGetTime(row.Cells[ColThoiGianKetThuc].Value, out ketThuc) ||
                !TryGetCa(out caValue))
            {
                row.Cells[ColThoiGianDung].Value = null;
                return;
            }

            int soPhut;
            string thongBaoThoiGian;

            if (!LoiDungMay_DB.TryTinhThoiGianDungTheoCa(
                batDau,
                ketThuc,
                caValue,
                out soPhut,
                out thongBaoThoiGian))
            {
                row.Cells[ColThoiGianDung].Value = null;
                return;
            }

            row.Cells[ColThoiGianDung].Value = string.Format("{0} phút", soPhut);
        }

        private bool KiemTraDong(
            DataGridViewRow row,
            bool ganLoiLenCell,
            out int tenLoiId,
            out TimeSpan batDau,
            out TimeSpan ketThuc,
            out int soPhutDung,
            out string ghiChu,
            out string thongBao)
        {
            tenLoiId = 0;
            batDau = TimeSpan.Zero;
            ketThuc = TimeSpan.Zero;
            soPhutDung = 0;
            ghiChu = string.Empty;
            thongBao = string.Empty;

            if (row == null || row.IsNewRow || !DongCoDuLieu(row))
            {
                if (ganLoiLenCell && row != null && !row.IsNewRow)
                {
                    XoaLoiCell(row);
                }

                return true;
            }

            if (ganLoiLenCell)
            {
                XoaLoiCell(row);
            }

            object tenLoiValue = row.Cells[ColTenLoi].Value;
            if (tenLoiValue == null ||
                !int.TryParse(Convert.ToString(tenLoiValue), out tenLoiId) ||
                !loiTheoId.ContainsKey(tenLoiId))
            {
                thongBao = "Tên lỗi không được để trống.";
                GanLoiCell(row, ColTenLoi, thongBao, ganLoiLenCell);
                return false;
            }

            if (!TryGetTime(row.Cells[ColThoiGianBatDau].Value, out batDau))
            {
                thongBao = "Thời gian bắt đầu không được để trống.";
                GanLoiCell(row, ColThoiGianBatDau, thongBao, ganLoiLenCell);
                return false;
            }

            if (!TryGetTime(row.Cells[ColThoiGianKetThuc].Value, out ketThuc))
            {
                thongBao = "Thời gian kết thúc không được để trống.";
                GanLoiCell(row, ColThoiGianKetThuc, thongBao, ganLoiLenCell);
                return false;
            }

            int caValue;
            if (!TryGetCa(out caValue))
            {
                thongBao = "Vui lòng chọn ca trước khi nhập thời gian.";
                GanLoiCell(row, ColThoiGianBatDau, thongBao, ganLoiLenCell);
                GanLoiCell(row, ColThoiGianKetThuc, thongBao, ganLoiLenCell);
                row.Cells[ColThoiGianDung].Value = null;
                return false;
            }

            if (!LoiDungMay_DB.TryTinhThoiGianDungTheoCa(
                batDau,
                ketThuc,
                caValue,
                out soPhutDung,
                out thongBao))
            {
                GanLoiCell(row, ColThoiGianKetThuc, thongBao, ganLoiLenCell);
                row.Cells[ColThoiGianDung].Value = null;
                return false;
            }

            ghiChu = Convert.ToString(row.Cells[ColGhiChu].Value) ?? string.Empty;
            ghiChu = ghiChu.Trim();

            TenLoiDungMay_Model tenLoi = loiTheoId[tenLoiId];
            if (IsLamViecKhac(tenLoi.TenLoi) && string.IsNullOrWhiteSpace(ghiChu))
            {
                thongBao = "Khi chọn 'Làm việc khác' bắt buộc phải nhập ghi chú.";
                GanLoiCell(row, ColGhiChu, thongBao, ganLoiLenCell);
                return false;
            }

            return true;
        }

        private static void GanLoiCell(
            DataGridViewRow row,
            string columnName,
            string thongBao,
            bool ganLoi)
        {
            if (ganLoi)
            {
                row.Cells[columnName].ErrorText = thongBao;
            }
        }

        private static void XoaLoiCell(DataGridViewRow row)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.ErrorText = string.Empty;
            }
        }

        private static bool TryGetTime(object value, out TimeSpan result)
        {
            result = TimeSpan.Zero;

            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is TimeSpan)
            {
                result = (TimeSpan)value;
                return true;
            }

            if (value is DateTime)
            {
                result = ((DateTime)value).TimeOfDay;
                return true;
            }

            return TimeSpan.TryParse(Convert.ToString(value), out result);
        }

        private bool DongCoDuLieu(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
            {
                return false;
            }

            return row.Cells[ColTenLoi].Value != null
                || row.Cells[ColThoiGianBatDau].Value != null
                || row.Cells[ColThoiGianKetThuc].Value != null
                || !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColGhiChu].Value));
        }

        private static bool IsLamViecKhac(string tenLoi)
        {
            return string.Equals(
                (tenLoi ?? string.Empty).Trim(),
                TenLoiLamViecKhac,
                StringComparison.OrdinalIgnoreCase);
        }

        private void KiemTraOverlapTrongGridVaDanhDau(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grvDsLoiDungMay.Rows.Count)
            {
                return;
            }

            DataGridViewRow currentRow = grvDsLoiDungMay.Rows[rowIndex];
            if (currentRow.IsNewRow || !DongCoDuLieu(currentRow))
            {
                return;
            }

            int currentTenLoiId;
            TimeSpan currentStart;
            TimeSpan currentEnd;
            int currentDuration;
            string currentNote;
            string currentMessage;

            if (!KiemTraDong(
                currentRow,
                true,
                out currentTenLoiId,
                out currentStart,
                out currentEnd,
                out currentDuration,
                out currentNote,
                out currentMessage))
            {
                return;
            }

            int caValue;
            if (!TryGetCa(out caValue))
            {
                return;
            }

            foreach (DataGridViewRow otherRow in grvDsLoiDungMay.Rows)
            {
                if (otherRow.IsNewRow || otherRow.Index == rowIndex || !DongCoDuLieu(otherRow))
                {
                    continue;
                }

                int otherTenLoiId;
                TimeSpan otherStart;
                TimeSpan otherEnd;
                int otherDuration;
                string otherNote;
                string otherMessage;

                if (!KiemTraDong(
                    otherRow,
                    false,
                    out otherTenLoiId,
                    out otherStart,
                    out otherEnd,
                    out otherDuration,
                    out otherNote,
                    out otherMessage))
                {
                    continue;
                }

                if (HaiKhoangThoiGianChongLan(
                    ngay.Value.Date,
                    currentStart,
                    currentEnd,
                    ngay.Value.Date,
                    otherStart,
                    otherEnd,
                    caValue))
                {
                    currentRow.Cells[ColThoiGianDung].ErrorText =
                        string.Format("Thời gian chồng lấn với dòng {0}.", otherRow.Index + 1);
                    return;
                }
            }
        }

        private void DanhDauLaiOverlapTrongGrid()
        {
            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.Cells[ColThoiGianDung].ErrorText = string.Empty;
                }
            }

            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (!row.IsNewRow)
                {
                    KiemTraOverlapTrongGridVaDanhDau(row.Index);
                }
            }
        }

        private static bool HaiKhoangThoiGianChongLan(
            DateTime ngayA,
            TimeSpan startA,
            TimeSpan endA,
            DateTime ngayB,
            TimeSpan startB,
            TimeSpan endB,
            int caValue)
        {
            DateTime aStart;
            DateTime aEnd;
            DateTime bStart;
            DateTime bEnd;

            if (!LoiDungMay_DB.TryLayKhoangThoiGianTheoCa(
                    ngayA,
                    startA,
                    endA,
                    caValue,
                    out aStart,
                    out aEnd) ||
                !LoiDungMay_DB.TryLayKhoangThoiGianTheoCa(
                    ngayB,
                    startB,
                    endB,
                    caValue,
                    out bStart,
                    out bEnd))
            {
                return false;
            }

            return aStart < bEnd && bStart < aEnd;
        }

        private bool TryGetMayId(out int mayId)
        {
            mayId = 0;

            DanhSachMay_Model may = cbMay.SelectedItem as DanhSachMay_Model;
            if (may != null)
            {
                mayId = may.Id;
                return true;
            }

            return cbMay.SelectedValue != null
                && int.TryParse(Convert.ToString(cbMay.SelectedValue), out mayId);
        }

        private bool TryGetCa(out int caValue)
        {
            caValue = 0;
            return ca.SelectedItem != null
                && int.TryParse(Convert.ToString(ca.SelectedItem), out caValue);
        }

        private bool TryGetMaCongDoan(out int maCongDoan)
        {
            maCongDoan = -1;

            DanhSachCongDoan_Model selected =
                congDoan.SelectedItem as DanhSachCongDoan_Model;

            if (selected != null)
            {
                maCongDoan = selected.MaCongDoan;
                return true;
            }

            return false;
        }

        private bool ValidateHeader(
            out int maCongDoan,
            out int mayId,
            out int caValue,
            out string nguoiLamValue)
        {
            maCongDoan = -1;
            mayId = 0;
            caValue = 0;
            nguoiLamValue = (nguoiLam.Text ?? string.Empty).Trim();

            if (!TryGetMaCongDoan(out maCongDoan))
            {
                MessageBox.Show("Vui lòng chọn công đoạn.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                congDoan.Focus();
                return false;
            }

            if (!TryGetMayId(out mayId))
            {
                MessageBox.Show("Vui lòng chọn máy.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbMay.Focus();
                return false;
            }

            if (!TryGetCa(out caValue))
            {
                MessageBox.Show("Vui lòng chọn ca.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ca.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(nguoiLamValue))
            {
                MessageBox.Show("Vui lòng nhập người làm.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nguoiLam.Focus();
                return false;
            }

            return true;
        }

        private List<DanhSachLoiDungMay_Model> TaoDanhSachModelTuGrid(
            int maCongDoan,
            int mayId,
            int caValue,
            string nguoiLamValue)
        {
            var result = new List<DanhSachLoiDungMay_Model>();

            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (row.IsNewRow || !DongCoDuLieu(row))
                {
                    continue;
                }

                int tenLoiId;
                TimeSpan batDau;
                TimeSpan ketThuc;
                int soPhutDung;
                string ghiChu;
                string thongBao;

                if (!KiemTraDong(
                    row,
                    true,
                    out tenLoiId,
                    out batDau,
                    out ketThuc,
                    out soPhutDung,
                    out ghiChu,
                    out thongBao))
                {
                    throw new InvalidOperationException(
                        string.Format("Dòng {0}: {1}", row.Index + 1, thongBao));
                }

                int id = 0;
                if (row.Tag != null)
                {
                    int.TryParse(Convert.ToString(row.Tag), out id);
                }

                string nguoiLamCuaDong = nguoiLamValue;

                // Nếu header Người làm chưa thay đổi sau khi tải, giữ nguyên giá trị
                // gốc của từng bản ghi. Điều này đảm bảo mở rồi bấm Lưu mà không
                // chỉnh sửa sẽ không UPDATE ngầm các dòng vốn có Người làm khác nhau.
                string nguoiLamCu;
                if (id > 0 &&
                    dangChinhSuaDuLieuDaLuu &&
                    string.Equals(
                        nguoiLamValue.Trim(),
                        (nguoiLamBanDauKhiTai ?? string.Empty).Trim(),
                        StringComparison.Ordinal) &&
                    nguoiLamDaLuuTheoId.TryGetValue(id, out nguoiLamCu))
                {
                    nguoiLamCuaDong = nguoiLamCu;
                }

                result.Add(new DanhSachLoiDungMay_Model
                {
                    Id = id,
                    TenLoiDungMayId = tenLoiId,
                    TenLoi = loiTheoId[tenLoiId].TenLoi,
                    Ngay = ngay.Value.Date,
                    DanhSachMayId = mayId,
                    NguoiLam = nguoiLamCuaDong,
                    ThoiGianBatDau = batDau,
                    ThoiGianKetThuc = ketThuc,
                    ThoiGianDung = soPhutDung,
                    GhiChu = ghiChu,
                    Ca = caValue,
                    MaCongDoan = maCongDoan
                });
            }

            if (result.Count == 0)
            {
                throw new InvalidOperationException("Vui lòng nhập ít nhất một dòng lỗi dừng máy.");
            }

            return result;
        }

        private void ValidateOverlapTrongDanhSach(List<DanhSachLoiDungMay_Model> danhSach)
        {
            for (int i = 0; i < danhSach.Count; i++)
            {
                for (int j = i + 1; j < danhSach.Count; j++)
                {
                    if (HaiKhoangThoiGianChongLan(
                        danhSach[i].Ngay,
                        danhSach[i].ThoiGianBatDau,
                        danhSach[i].ThoiGianKetThuc,
                        danhSach[j].Ngay,
                        danhSach[j].ThoiGianBatDau,
                        danhSach[j].ThoiGianKetThuc,
                        danhSach[i].Ca))
                    {
                        throw new InvalidOperationException(
                            string.Format("Dòng {0} và dòng {1} có thời gian dừng máy chồng lấn nhau.", i + 1, j + 1));
                    }
                }
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                grvDsLoiDungMay.EndEdit();

                int maCongDoan;
                int mayId;
                int caValue;
                string nguoiLamValue;

                if (!ValidateHeader(out maCongDoan, out mayId, out caValue, out nguoiLamValue))
                {
                    return;
                }

                List<DanhSachLoiDungMay_Model> danhSach =
                    TaoDanhSachModelTuGrid(maCongDoan, mayId, caValue, nguoiLamValue);

                // Grid là trạng thái cuối cùng của Ngày + Máy + Ca.
                // DB layer sẽ đồng bộ UPDATE / INSERT / DELETE theo ID.
                ValidateOverlapTrongDanhSach(danhSach);

                Enabled = false;

                await WaitingHelper.RunWithWaiting<bool>(
                    async () => await Task.Run(() =>
                    {
                        LoiDungMay_DB.LuuDanhSach(danhSach);
                        return true;
                    }),
                    "ĐANG LƯU DỮ LIỆU LỖI DỪNG MÁY...");

                MessageBox.Show(
                    "Lưu dữ liệu lỗi dừng máy thành công.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ResetToanBoForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                Enabled = true;
            }
        }

        private void ResetToanBoForm()
        {
            dangNapDanhMuc = true;

            try
            {
                grvDsLoiDungMay.Rows.Clear();
                nguoiLam.Clear();
                quanDoc.Clear();
                ca.SelectedIndex = -1;
                cbMay.DataSource = null;
                cbMay.Items.Clear();
                cbMay.SelectedIndex = -1;
                congDoan.SelectedIndex = -1;
                ngay.Value = DateTime.Today;
                SetDanhSachLoi(new List<TenLoiDungMay_Model>());
                nguoiLamDaLuuTheoId.Clear();
                nguoiLamBanDauKhiTai = string.Empty;
                dangChinhSuaDuLieuDaLuu = false;
            }
            finally
            {
                dangNapDanhMuc = false;
            }
        }
    }
}
