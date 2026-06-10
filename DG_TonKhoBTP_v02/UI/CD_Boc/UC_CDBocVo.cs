using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_CDBocVo : UserControl, ISectionProvider<CD_BocVo>, IDataReceiver, IFormSection
    {
        // Dữ liệu đóng gói tạm thời, để GetSectionData() đưa vào snap.
        // Sau khi lưu thành công hoặc khi sao chép dữ liệu, danh sách này phải được clear riêng
        // vì ControlCleaner chỉ clear control UI, không clear được biến private.
        private List<ThongTinCuonDay> _thongTinCuonDay = new List<ThongTinCuonDay>();

        public UC_CDBocVo()
        {
            InitializeComponent();
            UpdateDongGoiButtonText();
        }

        public string SectionName => nameof(UC_CDBocVo);

        public CD_BocVo GetSectionData()
        {
            return new CD_BocVo
            {
                TTThanhPhan_ID = null,
                DayVoTB = dayVoTB.Value == 0m ? null : (double?)dayVoTB.Value,
                InAn = string.IsNullOrEmpty(inAn.Text) ? null : inAn.Text,

                TTCuonDay_CD = CloneThongTinCuonDay(_thongTinCuonDay)
            };
        }

        public void LoadData(DataTable dt, int kieuEdit)
        {
            // Mỗi lần tìm kiếm dữ liệu mới cần clear trước để tránh giữ lại dữ liệu đóng gói của lần trước.
            ClearInputs();

            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "DayVoTB", val => dayVoTB.Value = Convert.ToDecimal(val));
            CoreHelper.SetIfPresent(row, "InAn", val => inAn.Text = Convert.ToString(val));

            if (kieuEdit == 1)
            {
                // Sao chép: không lấy dữ liệu đóng gói cũ, yêu cầu người dùng nhập mới hoàn toàn.
                ClearThongTinCuonDay();
                return;
            }

            if (kieuEdit == 2)
            {
                // Sửa: lấy STT từ DataTable, STT chính là TTThanhPham.id đã được xác nhận.
                if (!row.Table.Columns.Contains("STT") || row["STT"] == DBNull.Value)
                {
                    ClearThongTinCuonDay();
                    return;
                }

                long ttThanhPhamId = Convert.ToInt64(row["STT"]);
                List<ThongTinCuonDay> dsCuonDay = DatabaseHelper.LayTTCuonDayCDTheoTTThanhPhamId(ttThanhPhamId);
                SetThongTinCuonDay(dsCuonDay);
                return;
            }

            ClearThongTinCuonDay();
        }

        public object GetData()
        {
            return GetSectionData();
        }

        public void ClearInputs()
        {
            if (dayVoTB != null)
            {
                decimal value = 0m;
                if (value < dayVoTB.Minimum) value = dayVoTB.Minimum;
                if (value > dayVoTB.Maximum) value = dayVoTB.Maximum;
                dayVoTB.Value = value;
            }

            if (inAn != null)
            {
                inAn.SelectedIndex = -1;
                inAn.Text = string.Empty;
            }

            ClearThongTinCuonDay();
        }

        private void ClearThongTinCuonDay()
        {
            _thongTinCuonDay = new List<ThongTinCuonDay>();
            UpdateDongGoiButtonText();
        }

        public void SetThongTinCuonDay(List<ThongTinCuonDay> data)
        {
            _thongTinCuonDay = CloneThongTinCuonDay(data);
            UpdateDongGoiButtonText();
        }

        private void btnDongGoi_Click(object sender, EventArgs e)
        {
            using (Frm_DLCuon frm = new Frm_DLCuon(_thongTinCuonDay))
            {
                if (frm.ShowDialog() != DialogResult.OK)
                    return;

                _thongTinCuonDay = CloneThongTinCuonDay(frm.ThongTinCuon);
                UpdateDongGoiButtonText();
            }
        }

        private void UpdateDongGoiButtonText()
        {
            if (btnDongGoi == null) return;

            int count = _thongTinCuonDay == null ? 0 : _thongTinCuonDay.Count;
            btnDongGoi.Text = count > 0 ? $"Đã nhập ({count})" : "Nhập";
        }

        private static List<ThongTinCuonDay> CloneThongTinCuonDay(List<ThongTinCuonDay> source)
        {
            if (source == null)
                return new List<ThongTinCuonDay>();

            return source.Select(x => new ThongTinCuonDay
            {
                TTLo_ID = x.TTLo_ID,
                SoCuon = x.SoCuon,
                TongChieuDai = x.TongChieuDai,
                SoDau = x.SoDau,
                soCuoi = x.soCuoi,
                Ghichu = x.Ghichu
            }).ToList();
        }
    }
}
