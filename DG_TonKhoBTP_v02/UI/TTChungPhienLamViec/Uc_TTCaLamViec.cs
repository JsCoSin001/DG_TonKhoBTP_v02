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
            dateTimePicker1.ValueChanged += ThongTinCaLamViec_ValueChanged;
            textBox1.TextChanged += ThongTinCaLamViec_ValueChanged;
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
                tbNgayBatDau.Text = CoreHelper.GetNgayHienTai();
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

        private static DateTime? ParseNullableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime parsed;
            string text = value.Trim();

            if (DateTime.TryParseExact(
                    text,
                    new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return parsed.Date;
            }

            if (DateTime.TryParse(text, out parsed))
                return parsed.Date;

            return null;
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
                tbNgayBatDau.Text = CoreHelper.GetNgayHienTai();
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
                    CoreHelper.SetIfPresent(row, "NgayBatDau", val => tbNgayBatDau.Text = Convert.ToString(val));

                    CoreHelper.SetIfPresent(row, "NgayKetThuc", val => textBox1.Text = Convert.ToString(val));

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
