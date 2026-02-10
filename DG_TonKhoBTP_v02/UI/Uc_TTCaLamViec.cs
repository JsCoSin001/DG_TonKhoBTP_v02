using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;


namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTCaLamViec : UserControl, IFormSection, IDataReceiver
    {
        private string _URL;
        private CongDoan _CD;
        public string MayText => cbMay?.Text?.Trim() ?? "";
        public event Action<string> Event_ChonMay;
        public UC_TTCaLamViec()
        {
            InitializeComponent();
        }

        public UC_TTCaLamViec(List<string> dsMay, string uRL, CongDoan cd)
        {
            InitializeComponent();
            _CD = cd;
            this.StartForm(dsMay, uRL, _CD.TenCongDoan);
        }
        

        private void StartForm(List<string> dsMay, string url, string tieuDe)
        {
            _URL = url;

            lblTieuDe.Text = ("báo cáo công đoạn " + tieuDe).ToUpper();

            cbMay.Items.Clear();
            cbMay.Items.AddRange(dsMay.ToArray());

            string ca = CoreHelper.GetShiftValue();
            this.ca.SelectedItem = ca;

            ngay.Value = DateTime.Parse(CoreHelper.GetNgayHienTai());
        }

        private void cbMay_SelectedIndexChanged(object sender, EventArgs e)
        {
            Event_ChonMay?.Invoke(MayText);
        }

        #region Lấy và load dữ liệu vào form
        public string SectionName => nameof(UC_TTCaLamViec);

        public object GetData()
        {
            return new ThongTinCaLamViec
            {
                Id = _CD.Id,
                Ngay = ngay.Value.ToString("yyyy-MM-dd"),
                May = cbMay?.Text ?? string.Empty,
                Ca = ca?.Text ?? string.Empty,
                NguoiLam = nguoiLam?.Text ?? string.Empty,
                ToTruong = toTruong?.Text ?? string.Empty,
                QuanDoc = quanDoc?.Text ?? string.Empty
            };
        }

        public void ClearInputs()
        {
            ngay.Value = DateTime.Today;
            cbMay.SelectedIndex = -1; cbMay.Text = string.Empty;
            ca.SelectedIndex = -1; ca.Text = CoreHelper.GetShiftValue();
            nguoiLam.Clear(); toTruong.Clear(); quanDoc.Clear();
        }
        #endregion


        #region Hiển thị dữ liệu từ DataTable
        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            CoreHelper.SetIfPresent(row, "Ngay", val => ngay.Value = Convert.ToDateTime(val));
            CoreHelper.SetIfPresent(row, "May", val => cbMay.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "Ca", val => ca.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "NguoiLam", val => nguoiLam.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "ToTruong", val => toTruong.Text = Convert.ToString(val));
            CoreHelper.SetIfPresent(row, "QuanDoc", val => quanDoc.Text = Convert.ToString(val));

        }

        
        #endregion
    }
}
