using DG_TonKhoBTP_v02.Core;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_TTCaLamViec : UserControl, IFormSection, IDataReceiver
    {
        private string _URL;

        public event Action<string> Event_ChonMay;
        public UC_TTCaLamViec()
        {
            InitializeComponent();
        }

        public UC_TTCaLamViec(List<string> dsMay, string uRL, string tieuDe)
        {
            InitializeComponent();
            this.StartForm(dsMay, uRL, tieuDe);
        }
        

        private void StartForm(List<string> dsMay, string url, string tieuDe)
        {
            _URL = url;

            lblTieuDe.Text = ("báo cáo công đoạn " + tieuDe).ToUpper();

            cbMay.Items.Clear();
            cbMay.Items.AddRange(dsMay.ToArray());

            string ca = Helper.Helper.GetShiftValue();
            this.ca.SelectedItem = ca;

            ngay.Value = DateTime.Parse(Helper.Helper.GetNgayHienTai());
        }

        private void cbMay_SelectedIndexChanged(object sender, EventArgs e)
        {
            Event_ChonMay?.Invoke(cbMay.SelectedItem?.ToString());
        }

        #region AI generated
        public string SectionName => nameof(UC_TTCaLamViec);

        public object GetData()
        {
            return new ThongTinCaLamViec
            {
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
            ca.SelectedIndex = -1; ca.Text = Helper.Helper.GetShiftValue();
            nguoiLam.Clear(); toTruong.Clear(); quanDoc.Clear();
        }
        #endregion


        #region Hiển thị dữ liệu từ DataTable
        public void LoadData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];

            Helper.Helper.SetIfPresent(row, "tclv_Ngay", val => ngay.Value = Convert.ToDateTime(val));
            Helper.Helper.SetIfPresent(row, "tclv_May", val => cbMay.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "tclv_Ca", val => ca.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "tclv_NguoiLam", val => nguoiLam.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "tclv_ToTruong", val => toTruong.Text = Convert.ToString(val));
            Helper.Helper.SetIfPresent(row, "tclv_QuanDoc", val => quanDoc.Text = Convert.ToString(val));

        }

        
        #endregion
    }
}
