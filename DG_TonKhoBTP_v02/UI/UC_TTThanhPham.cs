using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Helper; 
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
    public partial class UC_TTThanhPham : UserControl, ISectionProvider<ThongTinSP>
    {
        public UC_TTThanhPham()
        {
            InitializeComponent();
        }

        public void ChonMay(string value)
        {
            may.Text = value;
        }

        private void maHanhTrinh_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttCongDoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void sttLo_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        private void soBin_ValueChanged(object sender, EventArgs e)
        {
            maBin.Text = Helper.Helper.LOTGenerated(may, maHanhTrinh, sttCongDoan, sttLo, soBin);
        }

        #region AI generated
        public string SectionName => nameof(UC_TTThanhPham);

        public ThongTinSP GetSectionData()
        {
            return new ThongTinSP
            {
                // DanhSachSP_ID, ThongTinCaLamViec_ID: thường set ở bước lưu DB sau (FK),
                // ở snapshot UI bạn có thể để 0, rồi binding khi lưu thật.
                DanhSachSP_ID = 0,
                ThongTinCaLamViec_ID = 0,

                MaBin = maBin?.Text ?? string.Empty,
                KhoiLuongTruoc = 0, // nếu không có input, để 0
                KhoiLuongSau = (double)khoiLuong.Value,
                ChieuDaiTruoc = 0, // nếu không có input, để 0
                ChieuDaiSau = (double)chieuDai.Value,
                Phe = (double)phe.Value,
                GhiChu = GhiChu?.Text ?? string.Empty,
                DateInsert = DateTime.Now
            };
        }
        #endregion
    }
}
