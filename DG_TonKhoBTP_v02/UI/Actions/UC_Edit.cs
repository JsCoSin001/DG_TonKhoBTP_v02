using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
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
    public partial class UC_Edit : UserControl, IFormSection
    {
        CongDoan _cd;

        public event EventHandler<DataTableEventArgs> DataTableSubmitted;

        // Event để Form cha xử lý clear các UserControl khác
        public event Action RequestClearOtherSections;

        public UC_Edit(CongDoan cd)
        {
            InitializeComponent();
            _cd = cd;
        }

        private void RaiseClearOtherSections()
        {
            RequestClearOtherSections?.Invoke();
        }

        public (int kieuEdit, long stt) GetKieuEdit(decimal saoChep, decimal sua)
        {
            int kieuEdit = 0;
            long stt = 0;

            if (saoChep != 0 && sua == 0)
            {
                kieuEdit = 1;
                stt = (long)saoChep;
            }

            if (saoChep == 0 && sua != 0)
            {
                kieuEdit = 2;
                stt = (long)sua;
            }

            return (kieuEdit, stt);
        }

        private async void btnTim_Click(object sender, EventArgs e)
        {
            var type = GetKieuEdit(nbrSaoChep.Value, nbrSua.Value);

            int kieuEdit = type.kieuEdit;
            long stt = type.stt;

            btnTim.Enabled = false;

            if (stt == 0)
            {
                FrmWaiting.ShowGifAlert("VUI LÒNG NHẬP SỐ STT HỢP LỆ!");
                btnTim.Enabled = true;
                return;
            }

            try
            {
                //string soLOT = GetSoLOT?.Invoke();

                await WaitingHelper.RunWithWaiting(async () =>
                {
                    DataTable dt = await Task.Run(() =>
                        Database.DatabaseHelper.GetDataByID(stt.ToString(), _cd, kieuEdit));

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        FrmWaiting.ShowGifAlert("STT KHÔNG TỒN TẠI!");
                        return;
                    }

                    DataTableSubmitted?.Invoke(this, new DataTableEventArgs(dt, kieuEdit));

                }, "ĐANG TÌM KIẾM, VUI LÒNG ĐỢI...");
            }
            finally
            {
                btnTim.Enabled = true;
            }
        }

        public string SectionName => nameof(UC_Edit);

        public object GetData()
        {
            var type = GetKieuEdit(nbrSaoChep.Value, nbrSua.Value);

            int kieuEdit = type.kieuEdit;
            long stt = type.stt;

            return new EditModel
            {
                Id = (int)stt,
                KieuXuLy = kieuEdit,
            };
        }

        public void ClearInputs()
        {
            nbrSua.Value = 0;
            nbrSaoChep.Value = 0;
        }

        private void cbKieuXuLyDL_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void UC_Edit_Load(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void nbrSaoChep_KeyDown(object sender, KeyEventArgs e)
        {
            nbrSua.Value = 0;
        }

        private void nbrSua_KeyDown(object sender, KeyEventArgs e)
        {
            nbrSaoChep.Value = 0;
        }

        private void nbrSua_Click(object sender, EventArgs e)
        {
            RequestClearOtherSections?.Invoke();
        }

        private void nbrSaoChep_Click(object sender, EventArgs e)
        {
            RequestClearOtherSections?.Invoke();
        }
    }
}