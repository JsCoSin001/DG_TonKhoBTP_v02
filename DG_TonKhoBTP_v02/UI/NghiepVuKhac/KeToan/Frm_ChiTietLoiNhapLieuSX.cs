using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Models.KeToan;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class Frm_ChiTietLoiNhapLieuSX : Form
    {
        private readonly int _idLoi;
        private readonly int _ttThanhPhamId;
        private readonly string _tenThanhPham;
        private bool _confirmed;

        public bool Confirmed
        {
            get { return _confirmed; }
        }

        private const string ColComponentId = "colComponentId";
        private const string ColTenNLBom = "colTenNLBom";
        private const string ColTenNLThucTe = "colTenNLThucTe";
        private const string ColLotThucTe = "colLotThucTe";

        public Frm_ChiTietLoiNhapLieuSX(
            int idLoi,
            int ttThanhPhamId,
            string tenThanhPham,
            bool confirmed)
        {
            InitializeComponent();

            _idLoi = idLoi;
            _ttThanhPhamId = ttThanhPhamId;
            _tenThanhPham = tenThanhPham ?? string.Empty;
            _confirmed = confirmed;

            KhoiTaoBangChiTiet();
            CapNhatTrangThaiNutXacNhan();
            Load += Frm_ChiTietLoiNhapLieuSX_Load;
        }

        private async void Frm_ChiTietLoiNhapLieuSX_Load(object sender, EventArgs e)
        {
            lblTieuDe.Text = string.IsNullOrWhiteSpace(_tenThanhPham)
                ? "CHI TIẾT BOM VÀ NGUYÊN LIỆU THỰC TẾ"
                : "CHI TIẾT BOM VÀ NGUYÊN LIỆU THỰC TẾ - " + _tenThanhPham;

            await TaiChiTietAsync();
        }

        private void KhoiTaoBangChiTiet()
        {
            grvChiTiet.AutoGenerateColumns = false;
            grvChiTiet.AllowUserToAddRows = false;
            grvChiTiet.AllowUserToDeleteRows = false;
            grvChiTiet.AllowUserToOrderColumns = false;
            grvChiTiet.MultiSelect = false;
            grvChiTiet.ReadOnly = true;
            grvChiTiet.RowHeadersVisible = false;
            grvChiTiet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grvChiTiet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grvChiTiet.Columns.Clear();

            grvChiTiet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColComponentId,
                HeaderText = "STT_BOM",
                Width = 110,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvChiTiet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColTenNLBom,
                HeaderText = "Tên NL BOM",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvChiTiet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColTenNLThucTe,
                HeaderText = "Tên NL thực tế",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvChiTiet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColLotThucTe,
                HeaderText = "LOT thực tế",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private async Task TaiChiTietAsync()
        {
            DatTrangThaiThaoTac(false);

            try
            {
                List<ChiTietLoiNhapLieuSX_Model> danhSach =
                    await WaitingHelper.RunWithWaiting<List<ChiTietLoiNhapLieuSX_Model>>(
                        () => Task.Run(() =>
                            DanhSachLoiNhapLieuSX_DB.LayChiTietBomVaNguyenLieuThucTe(
                                _ttThanhPhamId)),
                        "ĐANG LẤY CHI TIẾT BOM VÀ NGUYÊN LIỆU THỰC TẾ...");

                grvChiTiet.Rows.Clear();

                foreach (ChiTietLoiNhapLieuSX_Model item in danhSach)
                {
                    int rowIndex = grvChiTiet.Rows.Add();
                    DataGridViewRow row = grvChiTiet.Rows[rowIndex];

                    row.Cells[ColComponentId].Value = item.ComponentId.HasValue
                        ? item.ComponentId.Value.ToString()
                        : string.Empty;
                    row.Cells[ColTenNLBom].Value = item.TenNLBom;
                    row.Cells[ColTenNLThucTe].Value = item.TenNLThucTe;
                    row.Cells[ColLotThucTe].Value = item.LotThucTe;

                    DinhDangDongKhacBiet(row, item);
                }

                if (danhSach.Count == 0)
                {
                    FrmWaiting.ShowGifAlert(
                        "Không tìm thấy dữ liệu BOM hoặc nguyên liệu thực tế cho thành phẩm này.",
                        "THÔNG BÁO",
                        "warning");
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    "Không thể lấy dữ liệu chi tiết.\n" + ex.Message,
                    "LỖI",
                    "warning");
            }
            finally
            {
                DatTrangThaiThaoTac(true);
            }
        }

        private void DatTrangThaiThaoTac(bool enabled)
        {
            grvChiTiet.Enabled = enabled;
            btnXuatExcel.Enabled = enabled;
            btnXacNhan.Enabled = enabled;
            btnDong.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        private void CapNhatTrangThaiNutXacNhan()
        {
            btnXacNhan.Text = _confirmed ? "OK" : "Xác nhận";
        }

        private async void btnXacNhan_Click(object sender, EventArgs e)
        {
            bool trangThaiMoi = !_confirmed;

            if (_confirmed)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "Bạn có chắc chắn muốn chuyển trạng thái về chưa xác nhận?",
                    "XÁC NHẬN HOÀN TÁC",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            DatTrangThaiThaoTac(false);

            try
            {
                bool updated = await WaitingHelper.RunWithWaiting<bool>(
                    () => Task.Run(() =>
                        DanhSachLoiNhapLieuSX_DB.CapNhatConfirmed(_idLoi, trangThaiMoi)),
                    trangThaiMoi
                        ? "ĐANG XÁC NHẬN DỮ LIỆU..."
                        : "ĐANG HOÀN TÁC XÁC NHẬN...");

                if (!updated)
                {
                    FrmWaiting.ShowGifAlert(
                        "Database không cập nhật được bản ghi. Trạng thái hiện tại được giữ nguyên.",
                        "KHÔNG THỂ CẬP NHẬT",
                        "warning");
                    return;
                }

                _confirmed = trangThaiMoi;
                CapNhatTrangThaiNutXacNhan();

                
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    "Không thể cập nhật trạng thái xác nhận.\n" + ex.Message,
                    "LỖI",
                    "warning");
            }
            finally
            {
                DatTrangThaiThaoTac(true);
            }
        }

        private static void DinhDangDongKhacBiet(
            DataGridViewRow row,
            ChiTietLoiNhapLieuSX_Model item)
        {
            if (item.CoTrongBom && item.CoTrongThucTe)
            {
                return;
            }

            // Một phía bị thiếu sẽ được để trống và tô nền để người dùng dễ nhận biết.
            row.DefaultCellStyle.BackColor = item.CoTrongBom
                ? Color.LightYellow
                : Color.MistyRose;
        }

        private void btnDong_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
