using DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho
{
    public partial class UC_NhapKho : UserControl
    {
        private UC_NhapKhoNVL _ucNhapKhoNVL;
        private UC_NhapKhoTP _ucNhapKhoTP;

        public UC_NhapKho()
        {
            InitializeComponent();
            KhoiTaoGiaoDienNhapKho();
        }

        private void KhoiTaoGiaoDienNhapKho()
        {
            tabThanhPham.SuspendLayout();
            tabNhapNVL.SuspendLayout();
            tabNhapTP.SuspendLayout();

            try
            {
                tabNhapNVL.Controls.Clear();
                tabNhapTP.Controls.Clear();

                _ucNhapKhoNVL = new UC_NhapKhoNVL
                {
                    Dock = DockStyle.Fill
                };

                _ucNhapKhoTP = new UC_NhapKhoTP
                {
                    Dock = DockStyle.Fill
                };

                tabNhapNVL.Controls.Add(_ucNhapKhoNVL);
                tabNhapTP.Controls.Add(_ucNhapKhoTP);

                tabThanhPham.SelectedIndex = 0;
            }
            finally
            {
                tabNhapTP.ResumeLayout(true);
                tabNhapNVL.ResumeLayout(true);
                tabThanhPham.ResumeLayout(true);
            }
        }
    }
}