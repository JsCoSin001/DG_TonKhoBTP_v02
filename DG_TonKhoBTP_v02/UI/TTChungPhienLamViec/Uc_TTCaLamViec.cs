using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTCaLamViec : UserControl, IFormSection, IDataReceiver
    {
        private string _URL;
        private CongDoan _CD;
        private bool _dangCapNhatNoiBo;
        private bool _giuNgayBatDauDaLuu;
        private ErrorProvider _loiGioBatDau;
        private bool _dangHienThiLoiGio;
        private bool _ngayKetThucDaSuaTay;
        private bool _dangTraFocusKetThuc;

        public string MayText => cbMay?.Text?.Trim() ?? string.Empty;
        public event Action<string> Event_ChonMay;
        public event Action<ThongTinCaLamViec> Event_ThongTinCaLamViecChanged;

        public void SetNguoiLam(string username)
        {
            tbxNguoiBD.Text = username ?? string.Empty;
        }

        public UC_TTCaLamViec()
        {
            InitializeComponent();
            WireModelChangedEvents();
            ClearInputs();
        }

        public UC_TTCaLamViec(string uRL, CongDoan cd)
        {
            InitializeComponent();
            _CD = cd;
            WireModelChangedEvents();
            ClearInputs();
            StartForm(uRL, _CD.TenCongDoan);
        }

        private void WireModelChangedEvents()
        {
            AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            if (components == null)
                components = new System.ComponentModel.Container();
            _loiGioBatDau = new ErrorProvider(components)
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
            caBD.SelectedIndexChanged += ThongTinCaLamViec_ValueChanged;
            tbxNguoiBD.TextChanged += ThongTinCaLamViec_ValueChanged;
            tbNgayBD.TextChanged += ThongTinCaLamViec_ValueChanged;
            tbNgayBD.Leave += NgayTextBox_Leave;
            dtGioBD.ValueChanged += ThongTinCaLamViec_ValueChanged;
            dtGioBD.Validating += DtGioBD_Validating;
            tbNgayKT.TextChanged += ThongTinCaLamViec_ValueChanged;
            tbNgayKT.Validating += KetThuc_Validating;
            dtGioKT.ValueChanged += ThongTinCaLamViec_ValueChanged;
            dtGioKT.Validating += KetThuc_Validating;
            tbxNguoiKT.TextChanged += ThongTinCaLamViec_ValueChanged;
            cbxCaKT.SelectedIndexChanged += ThongTinCaLamViec_ValueChanged;
            cbxCaKT.Validating += KetThuc_Validating;
        }

        private void StartForm(string url, string tieuDe)
        {
            _URL = url;
            lblTieuDe.Text = ("báo cáo công đoạn " + tieuDe).ToUpper();
            tbxNguoiBD.ReadOnly = _CD.Id == 9;

            NapDanhSachMayTheoCongDoan();

            if (string.IsNullOrWhiteSpace(tbNgayKT.Text))
                tbNgayKT.Text = GetNgayHienTaiDeHienThi();
        }

        private void NapDanhSachMayTheoCongDoan()
        {
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                List<DanhSachMay_Model> danhSachMay =
                    LoiDungMay_DB.GetDanhSachMayTheoMaCongDoan(_CD.Id)
                    ?? new List<DanhSachMay_Model>();

                cbMay.DataSource = null;
                cbMay.DisplayMember = nameof(DanhSachMay_Model.TenMay);
                cbMay.ValueMember = nameof(DanhSachMay_Model.Id);
                cbMay.DataSource = danhSachMay;
                cbMay.SelectedIndex = -1;
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }
        }

        private void cbMay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_dangCapNhatNoiBo)
                return;

            Event_ChonMay?.Invoke(MayText);
            RaiseThongTinCaLamViecChanged();
        }

        private void ThongTinCaLamViec_ValueChanged(object sender, EventArgs e)
        {
            if (_dangCapNhatNoiBo)
                return;

            if (sender == tbNgayKT)
                _ngayKetThucDaSuaTay = true;

            if (sender == caBD || sender == dtGioBD)
            {
                CapNhatGioTheoCa(sender == caBD);
                CapNhatNgayBatDauTheoCa();
                if (CaLamViecPolicy.GioBatDauHopLe(caBD.Text, GioBatDauDaChon()))
                    _loiGioBatDau.SetError(dtGioBD, string.Empty);
            }

            if (sender == cbxCaKT || sender == dtGioKT)
                CapNhatKetThucTheoCa();
            CapNhatLoiKetThuc(out _);
            RaiseThongTinCaLamViecChanged();
            if (sender == cbxCaKT)
                KiemTraKetThuc(cbxCaKT, null);
        }

        private void CapNhatKetThucTheoCa()
        {
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                dtGioKT.Enabled = CaLamViecPolicy.GioDauCa(cbxCaKT.Text).HasValue;
                dtGioKT.ShowCheckBox = !dtGioKT.Checked;
                DateTime gioHienThi = dtGioKT.Value.Date.AddHours(dtGioKT.Value.Hour).AddMinutes(dtGioKT.Value.Minute);
                if (dtGioKT.Value != gioHienThi)
                    dtGioKT.Value = gioHienThi;
                // Không tự sửa giờ sai khi đổi ca kết thúc: người dùng phải nhập lại.
                if (!_ngayKetThucDaSuaTay)
                {
                    DateTime ngay = DateTime.Today;
                    if ((cbxCaKT.Text == "2" || cbxCaKT.Text == "3") && dtGioKT.Checked &&
                        dtGioKT.Value.TimeOfDay < TimeSpan.FromHours(6))
                        ngay = ngay.AddDays(-1);
                    tbNgayKT.Text = ngay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }
            finally { _dangCapNhatNoiBo = oldState; }
        }

        private string LayLoiKetThuc(out Control controlLoi)
        {
            controlLoi = tbNgayKT;
            if (!string.IsNullOrWhiteSpace(tbNgayKT.Text) && !ParseNullableDate(tbNgayKT.Text).HasValue)
                return "Ngày kết thúc không hợp lệ.";

            string loi = CaLamViecPolicy.LayLoiKetThuc(GetThongTinCaLamViec(), out string truong);
            switch (truong)
            {
                case nameof(ThongTinCaLamViec.CaKetThuc): controlLoi = cbxCaKT; break;
                case nameof(ThongTinCaLamViec.GioKetThuc): controlLoi = dtGioKT; break;
            }
            return loi;
        }

        private string CapNhatLoiKetThuc(out Control controlLoi)
        {
            _loiGioBatDau.SetError(tbNgayKT, string.Empty);
            _loiGioBatDau.SetError(cbxCaKT, string.Empty);
            _loiGioBatDau.SetError(dtGioKT, string.Empty);
            string loi = LayLoiKetThuc(out controlLoi);
            if (!string.IsNullOrEmpty(loi))
                _loiGioBatDau.SetError(controlLoi, loi);
            return loi;
        }

        private void KetThuc_Validating(object sender, CancelEventArgs e)
        {
            KiemTraKetThuc(sender as Control, e);
            if (!e.Cancel && sender == tbNgayKT && ParseNullableDate(tbNgayKT.Text) is DateTime ngay)
            {
                bool oldState = _dangCapNhatNoiBo;
                _dangCapNhatNoiBo = true;
                try { tbNgayKT.Text = ngay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture); }
                finally { _dangCapNhatNoiBo = oldState; }
            }
        }

        private void KiemTraKetThuc(Control nguon, CancelEventArgs e)
        {
            if (_dangCapNhatNoiBo || _dangTraFocusKetThuc)
                return;
            if (_dangHienThiLoiGio)
            {
                if (e != null) e.Cancel = true;
                return;
            }

            string loi = CapNhatLoiKetThuc(out Control controlLoi);
            // Nếu lỗi nằm ở cấp khác, cho phép chuyển focus tới đúng trường cần sửa.
            if (e != null) e.Cancel = !string.IsNullOrEmpty(loi) && controlLoi == nguon;
            if (string.IsNullOrEmpty(loi))
                return;

            _dangHienThiLoiGio = true;
            try { FrmWaiting.ShowGifAlert(loi); }
            finally { _dangHienThiLoiGio = false; }
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed && !string.IsNullOrEmpty(LayLoiKetThuc(out Control hienTai)) &&
                        hienTai == controlLoi && controlLoi.Enabled && !controlLoi.ContainsFocus)
                    {
                        // Chuyển tới trường đã báo lỗi không được mở lại cùng thông báo ở trường nguồn.
                        _dangTraFocusKetThuc = true;
                        try
                        {
                            controlLoi.Focus();
                            if (controlLoi is TextBox textBox) textBox.SelectAll();
                        }
                        finally { _dangTraFocusKetThuc = false; }
                    }
                }));
        }

        private void CapNhatGioTheoCa(bool caThayDoi)
        {
            TimeSpan? gioDauCa = CaLamViecPolicy.GioDauCa(caBD.Text);
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                dtGioBD.Enabled = gioDauCa.HasValue;
                if (caThayDoi && gioDauCa.HasValue && !dtGioBD.Checked)
                {
                    dtGioBD.Value = dtGioBD.Value.Date.Add(gioDauCa.Value);
                    dtGioBD.Checked = true;
                }
                dtGioBD.ShowCheckBox = !dtGioBD.Checked;
                // Ô giờ chỉ hiển thị HH:mm; bỏ giây ẩn khi thao tác để giờ lưu khớp giờ hiển thị.
                DateTime gioHienThi = dtGioBD.Value.Date
                    .AddHours(dtGioBD.Value.Hour).AddMinutes(dtGioBD.Value.Minute);
                if (dtGioBD.Value != gioHienThi)
                    dtGioBD.Value = gioHienThi;
                if (caThayDoi && gioDauCa.HasValue &&
                    !CaLamViecPolicy.GioBatDauHopLe(caBD.Text, dtGioBD.Value.TimeOfDay))
                    dtGioBD.Value = dtGioBD.Value.Date.Add(gioDauCa.Value);
                if (caThayDoi)
                    _loiGioBatDau.SetError(dtGioBD, string.Empty);
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }
        }

        private void DtGioBD_Validating(object sender, CancelEventArgs e)
        {
            if (_dangCapNhatNoiBo || !dtGioBD.Enabled)
                return;
            if (_dangHienThiLoiGio)
            {
                e.Cancel = true;
                return;
            }

            // Giữ giờ người dùng nhập để sửa; cả hai luồng lưu sẽ kiểm tra lại quy tắc này.
            string loi = CaLamViecPolicy.LayLoiGioBatDau(caBD.Text, GioBatDauDaChon());
            _loiGioBatDau.SetError(dtGioBD, loi);
            e.Cancel = !string.IsNullOrEmpty(loi);
            if (!e.Cancel)
                return;

            _dangHienThiLoiGio = true;
            try { FrmWaiting.ShowGifAlert(loi); }
            finally { _dangHienThiLoiGio = false; }

            // Đợi kết thúc lần chuyển focus hiện tại rồi đưa người dùng về ô giờ cần sửa.
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed && dtGioBD.Enabled &&
                        !CaLamViecPolicy.GioBatDauHopLe(caBD.Text, GioBatDauDaChon()))
                        dtGioBD.Focus();
                }));
        }

        private TimeSpan? GioBatDauDaChon()
        {
            return dtGioBD.Checked ? dtGioBD.Value.TimeOfDay : (TimeSpan?)null;
        }

        private void CapNhatNgayBatDauTheoCa()
        {
            // Báo cáo cũ/draft giữ nguyên ngày nguồn, kể cả khi đổi ca hoặc giờ.
            if (_giuNgayBatDauDaLuu)
                return;

            DateTime ngayBatDau = DateTime.Today;
            if ((caBD.Text == "2" || caBD.Text == "3") &&
                dtGioBD.Value.TimeOfDay < new TimeSpan(6, 0, 0))
                ngayBatDau = ngayBatDau.AddDays(-1);

            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                tbNgayBD.Text = ngayBatDau.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }
        }

        private void RaiseThongTinCaLamViecChanged()
        {
            Event_ThongTinCaLamViecChanged?.Invoke(GetThongTinCaLamViec());
        }

        public ThongTinCaLamViec GetThongTinCaLamViec()
        {
            DateTime? ngayBatDau = ParseNullableDate(tbNgayBD?.Text);

            return new ThongTinCaLamViec
            {
                Id = _CD?.Id ?? 0,
                May = cbMay?.Text ?? string.Empty,
                DanhSachMayId = GetSelectedMayId(),
                Ca = caBD?.Text ?? string.Empty,
                NguoiLam = tbxNguoiBD?.Text ?? string.Empty,
                NguoiKetThuc = string.IsNullOrWhiteSpace(tbxNguoiKT?.Text) ? null : tbxNguoiKT.Text.Trim(),
                CaKetThuc = string.IsNullOrWhiteSpace(cbxCaKT?.Text) ? null : cbxCaKT.Text.Trim(),
                NgayBatDau = ngayBatDau,
                GioBatDau = GioBatDauDaChon(),
                NgayKetThuc = ParseNullableDate(tbNgayKT?.Text),
                GioKetThuc = dtGioKT.Checked ? dtGioKT.Value.TimeOfDay : (TimeSpan?)null,
                ToTruong = null,
                QuanDoc = null
            };
        }

        private int GetSelectedMayId()
        {
            if (cbMay?.SelectedValue == null)
                return 0;

            int id;
            return int.TryParse(Convert.ToString(cbMay.SelectedValue), out id) ? id : 0;
        }

        private void NgayTextBox_Leave(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;

            string value = (textBox.Text ?? string.Empty).Trim();

            // Không kiểm tra bắt buộc nhập tại UC_TTCaLamViec.
            // Ô trống được giữ nguyên để UC_SubmitForm xử lý nghiệp vụ khi lưu.
            if (string.IsNullOrWhiteSpace(value))
            {
                textBox.Text = string.Empty;
                return;
            }

            DateTime ngay;
            if (!TryParseNgayNguoiDung(value, out ngay))
            {
                string tenTruong = textBox == tbNgayBD
                    ? "Ngày bắt đầu"
                    : "Ngày kết thúc";

                FrmWaiting.ShowGifAlert("Định dạng ngày bắt đầu/kết thúc không đúng");

                BeginInvoke(new Action(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }));

                return;
            }

            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                textBox.Text = ngay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }

            RaiseThongTinCaLamViecChanged();
        }

        private static bool TryParseNgayNguoiDung(string value, out DateTime ngay)
        {
            ngay = default(DateTime);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            bool coDauGachCheo = text.Contains("/");
            bool coDauGachNgang = text.Contains("-");

            // Chỉ chấp nhận một loại dấu phân cách: '/' hoặc '-'.
            if (coDauGachCheo == coDauGachNgang)
                return false;

            char separator = coDauGachCheo ? '/' : '-';
            string[] parts = text.Split(separator);

            // Chỉ chấp nhận ngày/tháng hoặc ngày/tháng/năm.
            if (parts.Length != 2 && parts.Length != 3)
                return false;

            string ngayText = parts[0].Trim();
            string thangText = parts[1].Trim();

            if (!LaChuoiSoCoDoDai(ngayText, 1, 2) ||
                !LaChuoiSoCoDoDai(thangText, 1, 2))
            {
                return false;
            }

            int ngayValue;
            int thangValue;
            if (!int.TryParse(ngayText, out ngayValue) ||
                !int.TryParse(thangText, out thangValue))
            {
                return false;
            }

            int namValue;
            if (parts.Length == 2)
            {
                namValue = DateTime.Now.Year;
            }
            else
            {
                string namText = parts[2].Trim();

                // Nếu có năm thì bắt buộc đủ 4 chữ số.
                if (!LaChuoiSoCoDoDai(namText, 4, 4) ||
                    !int.TryParse(namText, out namValue))
                {
                    return false;
                }
            }

            try
            {
                // Constructor DateTime đồng thời kiểm tra ngày thực tế:
                // 31/4, tháng 13, 29/2 ở năm không nhuận...
                ngay = new DateTime(namValue, thangValue, ngayValue);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static bool LaChuoiSoCoDoDai(string value, int minLength, int maxLength)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length < minLength ||
                value.Length > maxLength)
            {
                return false;
            }

            return value.All(char.IsDigit);
        }

        private static DateTime? ParseNullableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime parsed;

            // Dữ liệu do người dùng nhập theo quy tắc của UC_TTCaLamViec.
            if (TryParseNgayNguoiDung(value, out parsed))
                return parsed.Date;

            // Giữ khả năng đọc dữ liệu nội bộ/database theo yyyy-MM-dd.
            if (DateTime.TryParseExact(
                    value.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static string GetNgayHienTaiDeHienThi()
        {
            return DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private static string ChuanHoaNgayDeHienThi(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            string text = Convert.ToString(value) ?? string.Empty;
            DateTime parsed;

            if (TryParseNgayNguoiDung(text, out parsed))
                return parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            if (DateTime.TryParseExact(
                    text.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            return text;
        }

        private void ChonMayTheoIdHoacTen(DataRow row)
        {
            if (row?.Table == null)
                return;

            if (row.Table.Columns.Contains("DanhSachMay_ID") && row["DanhSachMay_ID"] != DBNull.Value)
            {
                int mayId;
                if (int.TryParse(Convert.ToString(row["DanhSachMay_ID"]), out mayId) && mayId > 0)
                {
                    cbMay.SelectedValue = mayId;
                    if (cbMay.SelectedIndex >= 0)
                        return;
                }
            }

            if (!row.Table.Columns.Contains("May") || row["May"] == DBNull.Value)
                return;

            string tenMay = Convert.ToString(row["May"]) ?? string.Empty;
            List<DanhSachMay_Model> danhSach = cbMay.DataSource as List<DanhSachMay_Model>;
            DanhSachMay_Model item = danhSach?.FirstOrDefault(x =>
                string.Equals(x.TenMay, tenMay, StringComparison.OrdinalIgnoreCase));

            if (item != null)
                cbMay.SelectedValue = item.Id;
        }

        public string SectionName => nameof(UC_TTCaLamViec);

        public object GetData()
        {
            // Không để ngày nhập sai trở thành null và vượt qua phép kiểm tra kết thúc tùy chọn.
            if (!string.IsNullOrWhiteSpace(tbNgayKT.Text) && !ParseNullableDate(tbNgayKT.Text).HasValue)
                throw new InvalidOperationException("Ngày kết thúc không hợp lệ.");
            return GetThongTinCaLamViec();
        }

        public void ClearInputs()
        {
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                _giuNgayBatDauDaLuu = false;
                _ngayKetThucDaSuaTay = false;
                cbMay.Enabled = true;
                cbMay.SelectedIndex = -1;
                caBD.SelectedIndex = -1;
                dtGioBD.Enabled = false;
                dtGioBD.Checked = true;
                dtGioBD.ShowCheckBox = false;
                _loiGioBatDau.SetError(dtGioBD, string.Empty);
                tbxNguoiBD.Clear();
                tbxNguoiKT.Clear();
                cbxCaKT.SelectedIndex = -1;
                dtGioKT.Enabled = false;
                dtGioKT.Checked = true;
                dtGioKT.ShowCheckBox = false;
                _loiGioBatDau.SetError(tbNgayKT, string.Empty);
                _loiGioBatDau.SetError(cbxCaKT, string.Empty);
                _loiGioBatDau.SetError(dtGioKT, string.Empty);
                tbNgayBD.Enabled = false;
                tbNgayBD.Text = DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                tbNgayKT.Text = GetNgayHienTaiDeHienThi();
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }
        }

        public void LoadData(DataTable dt, int kieuDL)
        {
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                ClearInputs();
                if (dt == null || dt.Rows.Count == 0)
                    return;

                DataRow row = dt.Rows[0];
                ChonMayTheoIdHoacTen(row);
                cbMay.Enabled = kieuDL != (int)DataLoadMode.Draft;

                if (kieuDL == (int)DataLoadMode.OfficialEdit || kieuDL == (int)DataLoadMode.Draft)
                {
                    _giuNgayBatDauDaLuu = true;
                    _ngayKetThucDaSuaTay = true;
                    // Ngày trống trong báo cáo cũ/draft cũng phải phản ánh đúng dữ liệu nguồn.
                    tbNgayBD.Clear();
                    tbNgayKT.Clear();

                    CoreHelper.SetIfPresent(row, "Ca", val => caBD.Text = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "NguoiLam", val => tbxNguoiBD.Text = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "NguoiKetThuc", val => tbxNguoiKT.Text = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "CaKetThuc", val => cbxCaKT.SelectedItem = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "NgayBatDau", val => tbNgayBD.Text = ChuanHoaNgayDeHienThi(val));

                    CoreHelper.SetIfPresent(row, "NgayKetThuc", val => tbNgayKT.Text = ChuanHoaNgayDeHienThi(val));

                    bool coGioBatDauDaLuu = false;
                    CoreHelper.SetIfPresent(row, "GioBatDau", val =>
                    {
                        TimeSpan parsed;
                        if (TimeSpan.TryParse(Convert.ToString(val), out parsed) &&
                            parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1))
                        {
                            dtGioBD.Value = DateTime.Today.Add(parsed);
                            coGioBatDauDaLuu = true;
                        }
                    });
                    // Giờ thiếu không được kế thừa từ báo cáo trước. Checkbox cho phép chọn giờ rõ ràng.
                    dtGioBD.Checked = coGioBatDauDaLuu;
                    dtGioBD.ShowCheckBox = !coGioBatDauDaLuu;

                    bool coGioKetThucDaLuu = false;
                    CoreHelper.SetIfPresent(row, "GioKetThuc", val =>
                    {
                        TimeSpan parsed;
                        if (TimeSpan.TryParse(Convert.ToString(val), out parsed) &&
                            parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1))
                        {
                            dtGioKT.Value = DateTime.Today.Add(parsed);
                            coGioKetThucDaLuu = true;
                        }
                    });
                    dtGioKT.Checked = coGioKetThucDaLuu;
                    dtGioKT.ShowCheckBox = !coGioKetThucDaLuu;
                }
                // Chỉ bật nhập giờ theo ca đã tải; không tự sửa giờ nguồn của báo cáo cũ/draft.
                dtGioBD.Enabled = CaLamViecPolicy.GioDauCa(caBD.Text).HasValue;
                dtGioKT.Enabled = CaLamViecPolicy.GioDauCa(cbxCaKT.Text).HasValue;
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }

            RaiseThongTinCaLamViecChanged();
        }
    }
}
