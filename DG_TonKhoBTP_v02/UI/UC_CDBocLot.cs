using DG_TonKhoBTP_v02.Core;
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
    public partial class UC_CDBocLot : UserControl, ISectionProvider<CaiDatCDBoc>
    {
        public UC_CDBocLot()
        {
            InitializeComponent();
        }


        #region AI generated
        public string SectionName => nameof(UC_CDBocLot);

        public CaiDatCDBoc GetSectionData()
        {
            var m = new CaiDatCDBoc();
            // Nếu bạn có property tương ứng trong CaiDatCDBoc (ví dụ DoDayTBLot), map vào:
            // m.DoDayTBLot = (double)doDayTBLot.Value;
            // Nếu chưa có, bạn có thể thêm property mới vào model.
            return m;
        }
        #endregion
    }
}
