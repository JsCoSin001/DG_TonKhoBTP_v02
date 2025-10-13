using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Dictionary;
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
    public partial class UC_CDKeoRut : UserControl, ISectionProvider<CD_KeoRut>
    {
        public UC_CDKeoRut()
        {
            InitializeComponent();
        }

        public string SectionName => nameof(UC_CDKeoRut);


        public CD_KeoRut GetSectionData()
        {
            return new CD_KeoRut
            {
                TTThanhPhan_ID = 0, // bind FK khi lưu DB
                DKTrucX = (double)dkTrucX.Value,
                DKTrucY = (double)dkTrucY.Value,
                NgoaiQuan = ngoaiQuan.Text,
                TocDo = (double)tocDo.Value,
                DienApU = (double)dienApU.Value,
                DongDienU = (double)dongDienU.Value
            };
        }


    }
}
