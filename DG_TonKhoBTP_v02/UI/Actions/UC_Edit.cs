using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models.SanXuat;
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
            cbxTimQr.KeyDown += cbxTimQr_KeyDown;
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
                DataTable dt = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() =>
                    {
                        DataTable loaded = Database.DatabaseHelper.GetDataByID(
                            stt.ToString(), _cd, kieuEdit);

                        if (loaded == null || loaded.Rows.Count == 0)
                            return loaded;

                        int productId = ReadProductId(loaded.Rows[0]);
                        List<BomComponentData> bomComponents =
                            Database.DatabaseHelper.GetActiveBomComponents(productId);

                        loaded.ExtendedProperties[BomDataTableProperties.Loaded] = true;
                        if (bomComponents != null)
                        {
                            loaded.ExtendedProperties[BomDataTableProperties.Components] =
                                bomComponents;
                        }

                        long ttThanhPhamId = ReadTTThanhPhamId(loaded.Rows[0]);
                        loaded.ExtendedProperties["LoiDungMay_TTThanhPhamId"] = ttThanhPhamId;
                        loaded.ExtendedProperties["LoiDungMay_Loaded"] = true;
                        loaded.ExtendedProperties["LoiDungMay_Items"] =
                            kieuEdit == 2
                                ? LoiDungMay_DB.GetDanhSachDaLuuTheoTTThanhPhamId(ttThanhPhamId)
                                : new List<DanhSachLoiDungMay_Model>();

                        return loaded;
                    }),
                    "ĐANG TÌM KIẾM, VUI LÒNG ĐỢI...");

                if (dt == null || dt.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("STT KHÔNG TỒN TẠI!");
                    return;
                }

                DataTableSubmitted?.Invoke(this, new DataTableEventArgs(dt, kieuEdit));
            }
            catch
            {
                FrmWaiting.ShowGifAlert(
                    "Cơ sở dữ liệu đang bận, thử lại sau ít phút",
                    "LỖI",
                    EnumStore.Icon.Warning);
            }
            finally
            {
                btnTim.Enabled = true;
            }
        }

        private async void cbxTimQr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            string rawQr = (cbxTimQr.Text ?? string.Empty).Trim();
            if (rawQr.Length == 0) return;

            // Hỗ trợ cả MaBin cũ và QR mới: cuontp;[mabin];[du-lieu-bo-sung].
            // Với mã không có cấu trúc, cbxTimQr xác định mặc định đây là CuonTP.
            QrParseResult qr = QrCodeHelper.Parse(rawQr, QrCodeType.CuonTP);
            if (!qr.IsValid)
            {
                FrmWaiting.ShowGifAlert(qr.ErrorMessage, "MÃ QR KHÔNG HỢP LỆ");
                return;
            }

            string maBin = qr.SearchValue;

            cbxTimQr.Enabled = false;
            try
            {

                LuuTam_DB.DraftLookupResult lookup = await Task.Run(() =>
                {
                    if (long.TryParse(maBin, out long id) && id > 0)
                    {
                        return LuuTam_DB.FindById(id);
                    }

                    return LuuTam_DB.FindByMaBin(maBin);
                });


                if (!lookup.Found)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu lưu tạm");
                    return;
                }
                if (lookup.Temp == 0)
                {
                    FrmWaiting.ShowGifAlert("MaBin đã là dữ liệu chính thức. Vui lòng sử dụng chức năng Sửa.");
                    return;
                }

                DataTable dt = await WaitingHelper.RunWithWaiting(
                    () => Task.Run(() =>
                    {
                        DataTable loaded = Database.DatabaseHelper.GetDataByID(
                            lookup.Id.ToString(), _cd, (int)DataLoadMode.Draft);
                        if (loaded == null || loaded.Rows.Count == 0) return loaded;

                        int productId = ReadProductId(loaded.Rows[0]);
                        List<BomComponentData> bomComponents = Database.DatabaseHelper.GetActiveBomComponents(productId);
                        loaded.ExtendedProperties[BomDataTableProperties.Loaded] = true;
                        loaded.ExtendedProperties[BomDataTableProperties.Components] = bomComponents ?? new List<BomComponentData>();
                        loaded.ExtendedProperties["DraftId"] = lookup.Id;
                        loaded.ExtendedProperties["DraftMaBin"] = lookup.MaBin;
                        loaded.ExtendedProperties["LoiDungMay_TTThanhPhamId"] = lookup.Id;
                        loaded.ExtendedProperties["LoiDungMay_Loaded"] = true;
                        loaded.ExtendedProperties["LoiDungMay_Items"] = LoiDungMay_DB.GetDanhSachDaLuuTheoTTThanhPhamId(lookup.Id);
                        return loaded;
                    }),
                    "ĐANG TẢI DỮ LIỆU LƯU TẠM...");

                if (dt == null || dt.Rows.Count == 0)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu lưu tạm");
                    return;
                }

                cbxTimQr.Text = lookup.MaBin;
                DataTableSubmitted?.Invoke(this, new DataTableEventArgs(dt, (int)DataLoadMode.Draft));
            }
            catch
            {
                FrmWaiting.ShowGifAlert("Cơ sở dữ liệu đang bận, thử lại sau ít phút", "LỖI", EnumStore.Icon.Warning);
            }
            finally
            {
                cbxTimQr.Enabled = true;
            }
        }

        private static long ReadTTThanhPhamId(DataRow row)
        {
            if (row?.Table == null)
                throw new InvalidOperationException("Không xác định được TTThanhPham_ID.");

            DataColumn column = row.Table.Columns.Cast<DataColumn>()
                .FirstOrDefault(x => string.Equals(
                    x.ColumnName,
                    "STT",
                    StringComparison.OrdinalIgnoreCase));

            if (column == null || row[column] == DBNull.Value)
                throw new InvalidOperationException("Không xác định được TTThanhPham_ID.");

            long id = Convert.ToInt64(row[column]);
            if (id <= 0)
                throw new InvalidOperationException("TTThanhPham_ID không hợp lệ.");

            return id;
        }

        private static int ReadProductId(DataRow row)
        {
            if (row?.Table == null)
                throw new InvalidOperationException("Không xác định được thành phẩm.");

            DataColumn column = row.Table.Columns.Cast<DataColumn>()
                .FirstOrDefault(x => string.Equals(
                    x.ColumnName,
                    "DanhSachMaSP_ID",
                    StringComparison.OrdinalIgnoreCase));

            if (column == null || row[column] == DBNull.Value)
                throw new InvalidOperationException("Không xác định được thành phẩm.");

            int productId = Convert.ToInt32(row[column]);
            if (productId <= 0)
                throw new InvalidOperationException("Không xác định được thành phẩm.");

            return productId;
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
        }

        private void nbrSaoChep_Click(object sender, EventArgs e)
        {
        }
    }
}