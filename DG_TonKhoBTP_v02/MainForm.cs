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

            this.startScreen();



            //UC_TTNVL uc_TTThanhPham = new UC_TTNVL();
            //uc_TTThanhPham.Dock = DockStyle.Top;
            //this.pnShow.Controls.Add(uc_TTThanhPham);



        }

        private void startScreen()
        {
            //#region Tạo UI cho Công đoạn kéo rút
            List<string> dsMay = new List<string>()
            {
                "MAY01", "MAY02", "MAY03", "MAY04", "MAY05",
                "MAY06", "MAY07", "MAY08", "MAY09", "MAY10",
                "MAY11", "MAY12", "MAY13", "MAY14", "MAY15",
                "MAY16", "MAY17", "MAY18", "MAY19", "MAY20",
            };

            string tieuDe = "BÁO CÁO CÔNG ĐOẠN KÉO - RÚT";
            UC_TTCaLamViec uc_caLamViec = new UC_TTCaLamViec(dsMay, _URL, tieuDe);
            uc_caLamViec.Dock = DockStyle.Top;
            //#endregion

            //#region Tạo UI cho Thành Phẩm
            UC_TTThanhPham uC_TTThanhPham = new UC_TTThanhPham();
            uC_TTThanhPham.Dock = DockStyle.Top;
            //#endregion

            Panel pnLeft = new Panel();

            UC_TTSanPham uC_TTSanPham = new UC_TTSanPham();
            uC_TTSanPham.Dock = DockStyle.Top;

            UC_Edit uC_Edit = new UC_Edit();
            uC_Edit.Dock = DockStyle.Left;
            uC_Edit.Width = 500;

            UC_Report uC_Report = new UC_Report();
            uC_Report.Dock = DockStyle.Fill;

            Panel pnEdit_Report = new Panel();
            pnEdit_Report.Dock = DockStyle.Top;
            pnEdit_Report.Height = 120;
            pnEdit_Report.Controls.Add(uC_Report);
            pnEdit_Report.Controls.Add(uC_Edit);

            pnLeft.Controls.Add(pnEdit_Report);
            pnLeft.Controls.Add(uC_TTSanPham);

            pnLeft.Dock = DockStyle.Fill;

            Panel pn = new Panel();
            pn.Dock = DockStyle.Fill;

            UC_TTNVL uC_TTNVL = new UC_TTNVL();
            uC_TTNVL.Dock = DockStyle.Left;

            pn.Controls.Add(pnLeft);
            pn.Controls.Add(uC_TTNVL);


            Panel pnTop = new Panel();
            pnTop.Dock = DockStyle.Top;
            pnTop.AutoSize = true;

            pnTop.Controls.Add(uC_TTThanhPham);
            pnTop.Controls.Add(uc_caLamViec);

            pnShow.Controls.Add(pn);
            pnShow.Controls.Add(pnTop);

        }

        private void btnKeoRut_Click(object sender, EventArgs e)
        {
            pnShow.Controls.Clear();

        }

    }
}
