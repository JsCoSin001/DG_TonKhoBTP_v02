using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat
{
    public partial class UC_BaoCaoDungMay : UserControl
    {
        private const string ColTenLoi = "colTenLoi";
        private const string ColThoiGianDung = "colThoiGianDung";
        private const string ColGhiChu = "colGhiChu";
        private const string ColXoa = "colXoa";

        private readonly Dictionary<int, TenLoiDungMay_Model> _loiHopLe =
            new Dictionary<int, TenLoiDungMay_Model>();
        private readonly List<TenLoiDungMay_Model> _loiHienThi =
            new List<TenLoiDungMay_Model>();

        private bool _dangNap;
        private bool _coThayDoiChuaLuu;
        private long _baoCaoId;
        private int _caDangHienThi;
        private int _congDoanDangHienThi;
        private int _mayDangHienThi;
        private DateTime _ngayDangHienThi = DateTime.Today;

        public UC_BaoCaoDungMay()
        {
            InitializeComponent();
            CauHinhGrid();
            CauHinhDieuKhien();
            GanSuKien();
            Load += UC_BaoCaoDungMay_Load;
        }

        private void CauHinhDieuKhien()
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd/MM/yyyy";
            dateTimePicker1.MinDate = DateTime.Today.AddMonths(-1).Date;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker1.Value = DateTime.Today;
            comboBox1.SelectedIndex = -1;
            button1.Enabled = false;
            btnLuu.Enabled = false;
        }

        private void GanSuKien()
        {
            comboBox1.SelectedIndexChanged += (s, e) => XuLyThayDoiKhoa(false);
            comboBox2.SelectedIndexChanged += (s, e) => XuLyThayDoiKhoa(true);
            comboBox3.SelectedIndexChanged += (s, e) => XuLyThayDoiKhoa(false);
            dateTimePicker1.ValueChanged += (s, e) => XuLyThayDoiKhoa(false);
            textBox1.TextChanged += (s, e) => DanhDauDaSua();
            btnLuu.Click += btnLuu_Click;
            button1.Click += button1_Click;

            grvDsLoiDungMay.CellContentClick += Grv_CellContentClick;
            grvDsLoiDungMay.CellValueChanged += (s, e) => DanhDauDaSua();
            grvDsLoiDungMay.RowsAdded += (s, e) => DanhDauDaSua();
            grvDsLoiDungMay.RowsRemoved += (s, e) => DanhDauDaSua();
            grvDsLoiDungMay.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grvDsLoiDungMay.IsCurrentCellDirty &&
                    grvDsLoiDungMay.CurrentCell is DataGridViewComboBoxCell)
                    DanhDauDaSua();
            };
            grvDsLoiDungMay.EditingControlShowing += Grv_EditingControlShowing;
            grvDsLoiDungMay.DataError += (s, e) => { e.ThrowException = false; };
        }




        private void Grv_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // Bắt thay đổi khi ô đang được gõ, chưa rời focus để commit.
            TextBox tb = e.Control as TextBox;
            if (tb == null) return;
            tb.TextChanged -= Grv_EditTextChanged;
            tb.TextChanged += Grv_EditTextChanged;
        }

        private void Grv_EditTextChanged(object sender, EventArgs e) => DanhDauDaSua();

        private void DanhDauDaSua()
        {
            if (!_dangNap) _coThayDoiChuaLuu = true;
        }

        private void CauHinhGrid()
        {
            grvDsLoiDungMay.AutoGenerateColumns = false;
            grvDsLoiDungMay.Columns.Clear();
            grvDsLoiDungMay.AllowUserToAddRows = true;
            grvDsLoiDungMay.AllowUserToDeleteRows = false;
            grvDsLoiDungMay.RowTemplate.Height = 35;
            grvDsLoiDungMay.MultiSelect = false;
            grvDsLoiDungMay.RowHeadersVisible = false;
            grvDsLoiDungMay.EditMode = DataGridViewEditMode.EditOnEnter;

            var tenLoi = new DataGridViewComboBoxColumn
            {
                Name = ColTenLoi,
                HeaderText = "Tên lỗi",
                Width = 310,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
                DisplayMember = nameof(TenLoiDungMay_Model.TenLoi),
                ValueMember = nameof(TenLoiDungMay_Model.Id),
                ValueType = typeof(int),
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            tenLoi.DefaultCellStyle.NullValue = "-- Vui lòng chọn lỗi --";

            var thoiGianDung = new DataGridViewTextBoxColumn
            {
                Name = ColThoiGianDung,
                HeaderText = "Thời gian dừng (phút)",
                Width = 200,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            thoiGianDung.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var ghiChu = new DataGridViewTextBoxColumn
            {
                Name = ColGhiChu,
                HeaderText = "Ghi chú",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 250,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var xoa = new DataGridViewButtonColumn
            {
                Name = ColXoa,
                HeaderText = "Xóa dòng",
                Text = "Xóa",
                UseColumnTextForButtonValue = true,
                Width = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            grvDsLoiDungMay.Columns.AddRange(tenLoi, thoiGianDung, ghiChu, xoa);
        }

        private void UC_BaoCaoDungMay_Load(object sender, EventArgs e)
        {
            if (DesignMode) return;
            try
            {
                _dangNap = true;
                comboBox2.DataSource = LoiDungMay_DB.GetDanhSachCongDoanCoMay();
                comboBox2.DisplayMember = nameof(DanhSachCongDoan_Model.TenCongDoan);
                comboBox2.ValueMember = nameof(DanhSachCongDoan_Model.Id);
                comboBox2.SelectedIndex = -1;
                comboBox3.DataSource = null;
                NapNguonLoaiLoi(new List<TenLoiDungMay_Model>());
                DatLaiTrangThaiBanDau();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh mục dừng máy: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { _dangNap = false; }
        }

        private void NapNguonLoaiLoi(List<TenLoiDungMay_Model> danhSach)
        {
            _loiHopLe.Clear();
            _loiHienThi.Clear();
            _loiHienThi.Add(new TenLoiDungMay_Model { Id = -1, TenLoi = "-- Vui lòng chọn lỗi --" });
            foreach (TenLoiDungMay_Model item in danhSach)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.TenLoi)) continue;
                _loiHopLe[item.Id] = item;
                _loiHienThi.Add(item);
            }
            GanNguonComboTenLoi();
        }

        private void GanNguonComboTenLoi()
        {
            var column = (DataGridViewComboBoxColumn)grvDsLoiDungMay.Columns[ColTenLoi];
            // Gán trực tiếp danh sách mới: DataSource = null sẽ xóa DisplayMember/ValueMember,
            // khiến tên lỗi không chuyển được về ID khi người dùng chọn trong ComboBox.
            column.DataSource = _loiHienThi.ToList();
        }

        private void NapDanhMucTheoCongDoan()
        {
            grvDsLoiDungMay.Rows.Clear();
            DanhSachCongDoan_Model cd = comboBox2.SelectedItem as DanhSachCongDoan_Model;
            if (cd == null)
            {
                comboBox3.DataSource = null;
                NapNguonLoaiLoi(new List<TenLoiDungMay_Model>());
                return;
            }
            comboBox3.DataSource = LoiDungMay_DB.GetDanhSachMayTheoMaCongDoan(cd.MaCongDoan);
            comboBox3.DisplayMember = nameof(DanhSachMay_Model.TenMay);
            comboBox3.ValueMember = nameof(DanhSachMay_Model.Id);
            comboBox3.SelectedIndex = -1;
            NapNguonLoaiLoi(LoiDungMay_DB.GetDanhSachTenLoiTheoMaCongDoan(cd.MaCongDoan));
        }

        private int CaDaChon()
        {
            int ca;
            return int.TryParse(Convert.ToString(comboBox1.SelectedItem), out ca) ? ca : 0;
        }

        private int CongDoanDaChon() => (comboBox2.SelectedItem as DanhSachCongDoan_Model)?.Id ?? 0;
        private int MayDaChon() => (comboBox3.SelectedItem as DanhSachMay_Model)?.Id ?? 0;

        private void GhiNhoKhoa()
        {
            _caDangHienThi = CaDaChon();
            _congDoanDangHienThi = CongDoanDaChon();
            _mayDangHienThi = MayDaChon();
            _ngayDangHienThi = dateTimePicker1.Value.Date;
        }

        private void KhoiPhucKhoa()
        {
            _dangNap = true;
            try
            {
                comboBox1.SelectedIndex = _caDangHienThi > 0
                    ? comboBox1.FindStringExact(_caDangHienThi.ToString()) : -1;
                comboBox2.SelectedValue = _congDoanDangHienThi > 0
                    ? (object)_congDoanDangHienThi : -1;
                comboBox3.SelectedValue = _mayDangHienThi > 0
                    ? (object)_mayDangHienThi : -1;
                dateTimePicker1.Value = _ngayDangHienThi;
            }
            finally { _dangNap = false; }
        }

        private void XuLyThayDoiKhoa(bool congDoanThayDoi)
        {
            if (_dangNap || !IsHandleCreated) return;
            if (_coThayDoiChuaLuu && MessageBox.Show(
                "Dữ liệu đã nhập hoặc chỉnh sửa chưa được lưu. Bạn có muốn bỏ thay đổi để chuyển báo cáo không?",
                "Xác nhận chuyển báo cáo", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                KhoiPhucKhoa();
                return;
            }

            try
            {
                _dangNap = true;
                if (congDoanThayDoi) NapDanhMucTheoCongDoan();
                NapBaoCaoTheoKhoa();
                GhiNhoKhoa();
                _coThayDoiChuaLuu = false;
            }
            catch (Exception ex)
            {
                btnLuu.Enabled = false;
                button1.Enabled = false;
                MessageBox.Show("Không thể tải báo cáo: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Không tự động ghi đè hoặc chỉnh sửa khi tải chưa thành công.
            }
            finally { _dangNap = false; }
        }

        private void NapBaoCaoTheoKhoa()
        {
            grvDsLoiDungMay.Rows.Clear();
            // Bỏ các mục "Lỗi" tạm từ báo cáo trước; không lẫn vào danh mục mới.
            _loiHienThi.RemoveAll(x => x.Id != -1 && !_loiHopLe.ContainsKey(x.Id));
            GanNguonComboTenLoi();
            textBox1.Clear();
            _baoCaoId = 0;
            bool dayDuKhoa = CaDaChon() > 0 && CongDoanDaChon() > 0 && MayDaChon() > 0;
            btnLuu.Enabled = dayDuKhoa;
            button1.Enabled = false;
            btnLuu.Text = "Lưu";
            groupBox1.Text = "Thông tin chung – Tạo mới";
            if (!dayDuKhoa) return;

            List<ThongTinDungMay_Model> found = LoiDungMay_DB.TimBaoCao(
                dateTimePicker1.Value.Date, CaDaChon(), CongDoanDaChon(), MayDaChon());
            if (found.Count > 1)
                throw new InvalidOperationException(
                    "Dữ liệu bị trùng.");
            if (found.Count == 0) return;

            ThongTinDungMay_Model baoCao = found[0];
            List<DSLoiDungMay_Model> danhSach = LoiDungMay_DB.GetLoiTheoBaoCao(baoCao.Id);
            foreach (DSLoiDungMay_Model item in danhSach)
            {
                if (!_loiHopLe.ContainsKey(item.TenLoiDungMayId)
                    && !_loiHienThi.Any(x => x.Id == item.TenLoiDungMayId))
                {
                    // Giữ ID trong cell, nhưng hiển thị chính xác chữ "Lỗi".
                    _loiHienThi.Add(new TenLoiDungMay_Model
                    {
                        Id = item.TenLoiDungMayId,
                        TenLoi = "Lỗi"
                    });
                }
            }
            GanNguonComboTenLoi();
            foreach (DSLoiDungMay_Model item in danhSach)
            {
                int index = grvDsLoiDungMay.Rows.Add();
                DataGridViewRow row = grvDsLoiDungMay.Rows[index];
                row.Cells[ColTenLoi].Value = item.TenLoiDungMayId;
                row.Cells[ColThoiGianDung].Value = item.ThoiGianDung;
                row.Cells[ColGhiChu].Value = item.GhiChu;
            }
            textBox1.Text = baoCao.NguoiLam;
            _baoCaoId = baoCao.Id;
            button1.Enabled = true;
            btnLuu.Text = "Cập nhật";
            groupBox1.Text = "Thông tin chung – Chỉnh sửa";
        }

        private bool DongCoDuLieu(DataGridViewRow row)
        {
            return row != null && !row.IsNewRow &&
                ((row.Cells[ColTenLoi].Value != null && Convert.ToString(row.Cells[ColTenLoi].Value) != "0")
                 || !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColThoiGianDung].Value))
                 || !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColGhiChu].Value)));
        }

        private List<DSLoiDungMay_Model> LayDanhSachNhap()
        {
            grvDsLoiDungMay.EndEdit();
            var result = new List<DSLoiDungMay_Model>();
            var daGap = new HashSet<int>();
            foreach (DataGridViewRow row in grvDsLoiDungMay.Rows)
            {
                if (!DongCoDuLieu(row)) continue;
                foreach (DataGridViewCell cell in row.Cells) cell.ErrorText = string.Empty;
                int idLoi;
                if (!int.TryParse(Convert.ToString(row.Cells[ColTenLoi].Value), out idLoi) ||
                    !_loiHopLe.ContainsKey(idLoi))
                {
                    row.Cells[ColTenLoi].ErrorText = "Vui lòng chọn loại lỗi hợp lệ.";
                    throw new InvalidOperationException("Dòng " + (row.Index + 1) + ": Tên lỗi không hợp lệ (hoặc đang hiển thị 'Lỗi').");
                }
                if (!daGap.Add(idLoi))
                {
                    row.Cells[ColTenLoi].ErrorText = "Loại lỗi trùng.";
                    throw new InvalidOperationException("Dòng " + (row.Index + 1) + ": Loại lỗi xuất hiện nhiều lần. Vui lòng sửa, không tự cộng thời gian.");
                }
                int phut;
                if (!int.TryParse((Convert.ToString(row.Cells[ColThoiGianDung].Value) ?? string.Empty).Trim(), out phut) || phut <= 0)
                {
                    row.Cells[ColThoiGianDung].ErrorText = "Phải là số phút nguyên > 0.";
                    throw new InvalidOperationException("Dòng " + (row.Index + 1) + ": Thời gian dừng phải là số phút nguyên lớn hơn 0.");
                }
                string ghiChu = (Convert.ToString(row.Cells[ColGhiChu].Value) ?? string.Empty).Trim();
                if (string.Equals(_loiHopLe[idLoi].TenLoi.Trim(), "Làm việc khác", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(ghiChu))
                {
                    row.Cells[ColGhiChu].ErrorText = "Bắt buộc nhập ghi chú cho 'Làm việc khác'.";
                    throw new InvalidOperationException("Dòng " + (row.Index + 1) + ": 'Làm việc khác' cần ghi chú.");
                }
                result.Add(new DSLoiDungMay_Model
                {
                    TenLoiDungMayId = idLoi,
                    ThoiGianDung = phut,
                    GhiChu = ghiChu
                });
            }
            if (result.Count == 0)
                throw new InvalidOperationException("Cần ít nhất 1 dòng.");
            return result;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (_dangNap) return;
            long daLuuId;
            try
            {
                if (CaDaChon() <= 0 || CongDoanDaChon() <= 0 || MayDaChon() <= 0)
                    throw new InvalidOperationException("Vui lòng chọn đủ Ca, Ngày, Công đoạn và Máy.");
                string nguoiLam = textBox1.Text.Trim();
                if (string.IsNullOrWhiteSpace(nguoiLam))
                    throw new InvalidOperationException("Vui lòng nhập Người làm.");
                var baoCao = new ThongTinDungMay_Model
                {
                    Id = _baoCaoId,
                    Ngay = dateTimePicker1.Value.Date,
                    Ca = CaDaChon(),
                    DanhSachCongDoanId = CongDoanDaChon(),
                    DanhSachMayId = MayDaChon(),
                    NguoiLam = nguoiLam
                };
                List<DSLoiDungMay_Model> danhSach = LayDanhSachNhap();
                daLuuId = LoiDungMay_DB.LuuBaoCao(baoCao, danhSach);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Không thể lưu");
                return;
            }

            // Transaction đã COMMIT; lỗi khi reset giao diện không được báo nhầm là lỗi lưu.
            _baoCaoId = daLuuId;
            _coThayDoiChuaLuu = false;
            try
            {
                _dangNap = true;
                DatLaiTrangThaiBanDau();
            }
            catch (Exception ex)
            {
                btnLuu.Enabled = false;
                FrmWaiting.ShowGifAlert("Lưu thành công. Đặt lại giao diện thất bại: " + ex.Message);
                return;
            }
            finally { _dangNap = false; }
            FrmWaiting.ShowGifAlert("Lưu báo cáo dừng máy thành công.", myIcon: EnumStore.Icon.Success);
        }

        private void Grv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                grvDsLoiDungMay.Columns[e.ColumnIndex].Name != ColXoa) return;
            DataGridViewRow row = grvDsLoiDungMay.Rows[e.RowIndex];
            if (row.IsNewRow) return;
            if (
                MessageBox.Show("Bạn có muốn xóa dòng này?",
                "Xác nhận xóa dòng", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                grvDsLoiDungMay.Rows.RemoveAt(e.RowIndex);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_baoCaoId <= 0) return;
            if (MessageBox.Show(
                "Bạn có muốn xoá báo cáo này?",
                "Xác nhận xóa báo cáo", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            try
            {
                LoiDungMay_DB.XoaBaoCao(_baoCaoId);
                _dangNap = true;
                try { DatLaiTrangThaiBanDau(); }
                finally { _dangNap = false; }
                FrmWaiting.ShowGifAlert("Đã xóa báo cáo dừng máy thành công.", myIcon: EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Không thể xoá.");
            }
        }

        private void DatLaiTrangThaiBanDau()
        {
            _baoCaoId = 0;
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox3.DataSource = null;
            dateTimePicker1.Value = DateTime.Today;
            textBox1.Clear();
            grvDsLoiDungMay.Rows.Clear();
            NapNguonLoaiLoi(new List<TenLoiDungMay_Model>());
            btnLuu.Text = "Lưu";
            btnLuu.Enabled = false;
            button1.Enabled = false;
            groupBox1.Text = "Thông tin chung – Tạo mới";
            GhiNhoKhoa();
            _coThayDoiChuaLuu = false;
        }
    }
}
