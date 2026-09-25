using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
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

        public string MayText => cbMay?.Text?.Trim() ?? string.Empty;
        public event Action<string> Event_ChonMay;
        public event Action<ThongTinCaLamViec> Event_ThongTinCaLamViecChanged;

        public void SetNguoiLam(string username)
        {
            nguoiLam.Text = username ?? string.Empty;
        }

        public UC_TTCaLamViec()
        {
            InitializeComponent();
            WireModelChangedEvents();
        }

        public UC_TTCaLamViec(string uRL, CongDoan cd)
        {
            InitializeComponent();
            _CD = cd;
            WireModelChangedEvents();
            StartForm(uRL, _CD.TenCongDoan);
        }

        private void WireModelChangedEvents()
        {
            ca.SelectedIndexChanged += ThongTinCaLamViec_ValueChanged;
            nguoiLam.TextChanged += ThongTinCaLamViec_ValueChanged;
            tbNgayBatDau.TextChanged += ThongTinCaLamViec_ValueChanged;
            tbNgayBatDau.Leave += NgayTextBox_Leave;
            dateTimePicker1.ValueChanged += ThongTinCaLamViec_ValueChanged;
            textBox1.TextChanged += ThongTinCaLamViec_ValueChanged;
            textBox1.Leave += NgayTextBox_Leave;
            dateTimePicker2.ValueChanged += ThongTinCaLamViec_ValueChanged;
        }

        private void StartForm(string url, string tieuDe)
        {
            _URL = url;
            lblTieuDe.Text = ("báo cáo công đoạn " + tieuDe).ToUpper();
            nguoiLam.ReadOnly = _CD.Id == 9;

            NapDanhSachMayTheoCongDoan();

            string caHienTai = CoreHelper.GetShiftValue();
            ca.SelectedItem = caHienTai;

            if (string.IsNullOrWhiteSpace(tbNgayBatDau.Text))
                tbNgayBatDau.Text = GetNgayHienTaiDeHienThi();
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

            RaiseThongTinCaLamViecChanged();
        }

        private void RaiseThongTinCaLamViecChanged()
        {
            Event_ThongTinCaLamViecChanged?.Invoke(GetThongTinCaLamViec());
        }

        public ThongTinCaLamViec GetThongTinCaLamViec()
        {
            DateTime? ngayBatDau = ParseNullableDate(tbNgayBatDau?.Text);

            return new ThongTinCaLamViec
            {
                Id = _CD?.Id ?? 0,
                May = cbMay?.Text ?? string.Empty,
                DanhSachMayId = GetSelectedMayId(),
                Ca = ca?.Text ?? string.Empty,
                NguoiLam = nguoiLam?.Text ?? string.Empty,
                NgayBatDau = ngayBatDau,
                GioBatDau = dateTimePicker1?.Value.TimeOfDay,
                NgayKetThuc = ParseNullableDate(textBox1?.Text),
                GioKetThuc = dateTimePicker2?.Value.TimeOfDay,
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
                string tenTruong = textBox == tbNgayBatDau
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
            string value = CoreHelper.GetNgayHienTai();
            DateTime parsed;

            if (DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            return value;
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
            return GetThongTinCaLamViec();
        }

        public void ClearInputs()
        {
            bool oldState = _dangCapNhatNoiBo;
            _dangCapNhatNoiBo = true;
            try
            {
                cbMay.SelectedIndex = -1;
                ca.SelectedIndex = -1;
                ca.Text = CoreHelper.GetShiftValue();
                nguoiLam.Clear();
                tbNgayBatDau.Text = GetNgayHienTaiDeHienThi();
                textBox1.Clear();
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

                if (kieuDL == 2)
                {
                    CoreHelper.SetIfPresent(row, "Ca", val => ca.Text = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "NguoiLam", val => nguoiLam.Text = Convert.ToString(val));
                    CoreHelper.SetIfPresent(row, "NgayBatDau", val => tbNgayBatDau.Text = ChuanHoaNgayDeHienThi(val));

                    CoreHelper.SetIfPresent(row, "NgayKetThuc", val => textBox1.Text = ChuanHoaNgayDeHienThi(val));

                    CoreHelper.SetIfPresent(row, "GioBatDau", val =>
                    {
                        TimeSpan parsed;
                        if (TimeSpan.TryParse(Convert.ToString(val), out parsed))
                            dateTimePicker1.Value = DateTime.Today.Add(parsed);
                    });

                    CoreHelper.SetIfPresent(row, "GioKetThuc", val =>
                    {
                        TimeSpan parsed;
                        if (TimeSpan.TryParse(Convert.ToString(val), out parsed))
                            dateTimePicker2.Value = DateTime.Today.Add(parsed);
                    });
                }
            }
            finally
            {
                _dangCapNhatNoiBo = oldState;
            }

            RaiseThongTinCaLamViecChanged();
        }
    }
}
