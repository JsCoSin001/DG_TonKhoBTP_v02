using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class Uc_TTCaLamViec : UserControl
    {
        private string _URL;
        public Uc_TTCaLamViec()
        {
            InitializeComponent();
        }

        public Uc_TTCaLamViec(List<string> dsMay, string uRL, string tieuDe)
        {
            InitializeComponent();
            this.StartForm(dsMay, uRL, tieuDe);
        }

        private void StartForm(List<string> dsMay, string url, string tieuDe)
        {
            _URL = url;

            lblTieuDe.Text = tieuDe;

            cbMay.Items.Clear();
            cbMay.Items.AddRange(dsMay.ToArray());

            string ca = Helper.Helper.GetShiftValue();
            cbCa.SelectedItem = ca;

            dtpNgay.Value = DateTime.Parse(Helper.Helper.GetNgayHienTai());
        }
    }
}
