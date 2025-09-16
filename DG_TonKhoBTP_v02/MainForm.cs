using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02
{
    public partial class MainForm : Form
    {
        private string _URL = "D:\\Database\\QLSX_DG_v2.db"; 
        public MainForm()
        {
            InitializeComponent();

            this.Visible = false;       // ẩn trong lúc chuẩn bị
            this.SuspendLayout();
            this.ResumeLayout(true);
            this.Shown += (_, __) => this.Visible = true; // hiện khi đã xong

        }

        private void btnKeoRut_Click(object sender, EventArgs e)
        {
            pnShow.Controls.Clear();
            List<string> dsMay = new List<string>()
            {
                "MAY01", "MAY02", "MAY03", "MAY04", "MAY05",
                "MAY06", "MAY07", "MAY08", "MAY09", "MAY10",
                "MAY11", "MAY12", "MAY13", "MAY14", "MAY15",
                "MAY16", "MAY17", "MAY18", "MAY19", "MAY20",
            };

            string tieuDe = "BÁO CÁO CÔNG ĐOẠN KÉO - RÚT";

            Uc_TTCaLamViec uc_caLamViec = new Uc_TTCaLamViec(dsMay, _URL, tieuDe);
            uc_caLamViec.Dock = DockStyle.Top;

            ColumnDef[] schemaChiTiet = new[]
            {
                new ColumnDef("ĐK trục X",   typeof(decimal), true,  "N2"),
                new ColumnDef("ĐK trục Y",   typeof(decimal), true,  "N2"),
                new ColumnDef("Ngoại Quan",  typeof(Boolean), true, null, new[] { "Yes", "No" }),
                new ColumnDef("Tốc độ",      typeof(decimal), true, "N2"),
                new ColumnDef("Điện áp Ủ",   typeof(decimal), true, "N2"),
                new ColumnDef("Dòng điện Ủ", typeof(decimal), true, "N2"),
                new ColumnDef("Trọng lượng", typeof(decimal), true, "N2"),
            };


            UC_TTSanPham uC_TTSanPham = new UC_TTSanPham(schemaChiTiet);
            uC_TTSanPham.Dock = DockStyle.Top;
            this.pnShow.Controls.Add(uC_TTSanPham);

            this.pnShow.Controls.Add(uc_caLamViec);

        }
        
    }
}
