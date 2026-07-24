using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models.KeToan;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_KiemTraDuLieu : UserControl
    {
        private const string ColChon = "colChon";
        private const string ColIdLoi = "colIdLoi";
        private const string ColTTThanhPhamId = "colTTThanhPhamId";
        private const string ColLotThanhPham = "colLotThanhPham";
        private const string ColNgay = "colNgay";
        private const string ColMay = "colMay";
        private const string ColCa = "colCa";
        private const string ColNguoiLam = "colNguoiLam";
        private const string ColTenCongDoan = "colTenCongDoan";
        private const string ColTenThanhPham = "colTenThanhPham";
        private const string ColNoiDungLoi = "colNoiDungLoi";
        private const string ColConfirmed = "colConfirmed";
        private const string ColXacNhan = "colXacNhan";
        private const string ColDetail = "colDetail";

        private DataGridViewCell _cellDangChinhSua;

        public UC_KiemTraDuLieu()
        {
            InitializeComponent();
            KhoiTaoBangDanhSachLoi();
            grvDsLoiNhapLieu.CellContentClick += grvDsLoiNhapLieu_CellContentClick;
            grvDsLoiNhapLieu.CellDoubleClick += grvDsLoiNhapLieu_CellDoubleClick;
            grvDsLoiNhapLieu.CellEndEdit += grvDsLoiNhapLieu_CellEndEdit;
            grvDsLoiNhapLieu.CurrentCellDirtyStateChanged += grvDsLoiNhapLieu_CurrentCellDirtyStateChanged;
        }

        private void KhoiTaoBangDanhSachLoi()
        {
            grvDsLoiNhapLieu.AutoGenerateColumns = false;
            grvDsLoiNhapLieu.AllowUserToAddRows = false;
            grvDsLoiNhapLieu.AllowUserToDeleteRows = false;
            grvDsLoiNhapLieu.AllowUserToOrderColumns = false;
            grvDsLoiNhapLieu.MultiSelect = false;
            grvDsLoiNhapLieu.ReadOnly = false;
            grvDsLoiNhapLieu.RowHeadersVisible = false;
            grvDsLoiNhapLieu.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grvDsLoiNhapLieu.EditMode = DataGridViewEditMode.EditProgrammatically;
            grvDsLoiNhapLieu.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grvDsLoiNhapLieu.RowTemplate.MinimumHeight = 42;
            grvDsLoiNhapLieu.Columns.Clear();

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = ColChon,
                HeaderText = string.Empty,
                Width = 45,
                ReadOnly = false,
                TrueValue = true,
                FalseValue = false,
                IndeterminateValue = false,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColIdLoi,
                Visible = false,
                ReadOnly = true
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColTTThanhPhamId,
                HeaderText = "STT",
                Width = 90,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColNgay,
                HeaderText = "Ngày",
                Width = 110,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColMay,
                HeaderText = "Máy",
                Width = 100,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColCa,
                HeaderText = "Ca",
                Width = 80,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColNguoiLam,
                HeaderText = "Người làm",
                Width = 160,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColLotThanhPham,
                HeaderText = "LOT_TP",
                Width = 190,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True,
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColTenCongDoan,
                HeaderText = "Công đoạn",
                Width = 190,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColTenThanhPham,
                HeaderText = "Tên thành phẩm",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColNoiDungLoi,
                HeaderText = "Nội dung lỗi",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 50,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True
                },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColConfirmed,
                Visible = false,
                ReadOnly = true
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewButtonColumn
            {
                Name = ColXacNhan,
                HeaderText = "Xác nhận",
                Width = 110,
                ReadOnly = true,
                UseColumnTextForButtonValue = false,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grvDsLoiNhapLieu.Columns.Add(new DataGridViewButtonColumn
            {
                Name = ColDetail,
                HeaderText = "Chi tiết",
                Text = "Detail",
                Width = 90,
                ReadOnly = true,
                UseColumnTextForButtonValue = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private async void btnLayDS_Click(object sender, EventArgs e)
        {
            await TaiDanhSachLoiAsync();
        }

        private async void btnXuatExcel_Click(object sender, EventArgs e)
        {
            await XuatCacDongDuocChonAsync();
        }

        private async Task TaiDanhSachLoiAsync()
        {
            DatTrangThaiThaoTac(false);

            try
            {
                List<DanhSachLoiNhapLieuSX_Model> danhSach =
                    await WaitingHelper.RunWithWaiting<List<DanhSachLoiNhapLieuSX_Model>>(
                        () => Task.Run(DanhSachLoiNhapLieuSX_DB.LayDanhSachChuaXacNhan),
                        "ĐANG LẤY DANH SÁCH LỖI NHẬP LIỆU...");

                grvDsLoiNhapLieu.Rows.Clear();

                foreach (DanhSachLoiNhapLieuSX_Model item in danhSach)
                {
                    int rowIndex = grvDsLoiNhapLieu.Rows.Add();
                    DataGridViewRow row = grvDsLoiNhapLieu.Rows[rowIndex];

                    row.Cells[ColChon].Value = false;
                    row.Cells[ColIdLoi].Value = item.IdLoi;
                    row.Cells[ColTTThanhPhamId].Value = item.TTThanhPhamId;
                    row.Cells[ColLotThanhPham].Value = item.LotThanhPham;
                    row.Cells[ColNgay].Value = item.Ngay;
                    row.Cells[ColMay].Value = item.May;
                    row.Cells[ColCa].Value = item.Ca;
                    row.Cells[ColNguoiLam].Value = item.NguoiLam;
                    row.Cells[ColTenCongDoan].Value = item.TenCongDoan;
                    row.Cells[ColTenThanhPham].Value = item.TenThanhPham;
                    row.Cells[ColNoiDungLoi].Value = item.NoiDungLoi;
                    row.Cells[ColConfirmed].Value = item.Confirmed;
                    row.Cells[ColXacNhan].Value = item.Confirmed ? "OK" : "Xác nhận";
                }

                if (danhSach.Count == 0)
                {
                    HienThiThongBaoGif(
                        "Không có lỗi nhập liệu chưa xác nhận.",
                        "THÔNG BÁO",
                        "warning");
                }
            }
            catch (Exception ex)
            {
                HienThiThongBaoGif(
                    "Không thể lấy danh sách lỗi nhập liệu.\n" + ex.Message,
                    "LỖI",
                    "warning");
            }
            finally
            {
                DatTrangThaiThaoTac(true);
            }
        }

        private async Task XuatCacDongDuocChonAsync()
        {
            grvDsLoiNhapLieu.EndEdit();

            DataTable table = TaoBangDuLieuXuatExcel();
            if (table.Rows.Count == 0)
            {
                HienThiThongBaoGif(
                    "Vui lòng chọn ít nhất một dòng để xuất Excel.",
                    "XUẤT EXCEL",
                    "warning");
                return;
            }

            string filePath;
            using (var saveDialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = $"DanhSachLoiNhapLieuSX_{DateTime.Now:yyyyMMdd_HHmm}"
            })
            {
                Form owner = FindForm();
                DialogResult result = owner == null
                    ? saveDialog.ShowDialog()
                    : saveDialog.ShowDialog(owner);

                if (result != DialogResult.OK)
                {
                    HienThiThongBaoGif(
                        "Đã hủy quá trình xuất Excel.",
                        "XUẤT EXCEL",
                        "warning");
                    return;
                }

                filePath = saveDialog.FileName;
            }

            DatTrangThaiThaoTac(false);

            try
            {
                await WaitingHelper.RunWithWaiting<bool>(
                    () => Task.Run(() =>
                    {
                        ExcelExporter.ExportToPath(
                            table,
                            filePath,
                            ExcelExportTextFormat.Unicode);

                        return true;
                    }),
                    "ĐANG XUẤT DỮ LIỆU RA EXCEL...");

                BoChonCacDongDaXuat();

                HienThiThongBaoGif(
                    "Đã xuất Excel thành công!",
                    "XUẤT EXCEL",
                    "success");
            }
            catch (Exception ex)
            {
                HienThiThongBaoGif(
                    "Không thể xuất Excel.\n" + ex.Message,
                    "LỖI XUẤT EXCEL",
                    "warning");
            }
            finally
            {
                DatTrangThaiThaoTac(true);
            }
        }

        private DataTable TaoBangDuLieuXuatExcel()
        {
            var table = new DataTable("DanhSachLoiNhapLieuSX");
            table.Columns.Add("STT", typeof(string));
            table.Columns.Add("Ngày", typeof(string));
            table.Columns.Add("Máy", typeof(string));
            table.Columns.Add("Ca", typeof(string));
            table.Columns.Add("Người làm", typeof(string));
            table.Columns.Add("LOT_TP", typeof(string));
            table.Columns.Add("Công đoạn", typeof(string));
            table.Columns.Add("Tên thành phẩm", typeof(string));
            table.Columns.Add("Nội dung lỗi", typeof(string));

            foreach (DataGridViewRow row in grvDsLoiNhapLieu.Rows)
            {
                if (row.IsNewRow || !GetBool(row.Cells[ColChon].Value))
                {
                    continue;
                }

                table.Rows.Add(
                    GetCellString(row, ColTTThanhPhamId),
                    GetCellString(row, ColNgay),
                    GetCellString(row, ColMay),
                    GetCellString(row, ColCa),
                    GetCellString(row, ColNguoiLam),
                    GetCellString(row, ColLotThanhPham),
                    GetCellString(row, ColTenCongDoan),
                    GetCellString(row, ColTenThanhPham),
                    GetCellString(row, ColNoiDungLoi));
            }

            return table;
        }

        private void BoChonCacDongDaXuat()
        {
            foreach (DataGridViewRow row in grvDsLoiNhapLieu.Rows)
            {
                if (!row.IsNewRow && GetBool(row.Cells[ColChon].Value))
                {
                    row.Cells[ColChon].Value = false;
                }
            }
        }

        private void grvDsLoiNhapLieu_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (!grvDsLoiNhapLieu.IsCurrentCellDirty || grvDsLoiNhapLieu.CurrentCell == null)
            {
                return;
            }

            if (grvDsLoiNhapLieu.CurrentCell.OwningColumn.Name == ColChon)
            {
                grvDsLoiNhapLieu.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void grvDsLoiNhapLieu_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewCell cell = grvDsLoiNhapLieu.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (!(cell is DataGridViewTextBoxCell) || !LaCotChoPhepChinhSuaTamThoi(cell.OwningColumn.Name))
            {
                return;
            }

            // Chỉ mở chế độ edit trên lưới. Giá trị chỉnh sửa không được ghi xuống database.
            _cellDangChinhSua = cell;
            cell.ReadOnly = false;
            grvDsLoiNhapLieu.CurrentCell = cell;

            if (grvDsLoiNhapLieu.BeginEdit(true))
            {
                TextBox textBox = grvDsLoiNhapLieu.EditingControl as TextBox;
                if (textBox != null)
                {
                    textBox.SelectAll();
                }
            }
        }

        private void grvDsLoiNhapLieu_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_cellDangChinhSua == null)
            {
                return;
            }

            // Khóa lại ô sau khi edit; nội dung mới chỉ tồn tại trên DataGridView hiện tại.
            _cellDangChinhSua.ReadOnly = true;
            _cellDangChinhSua = null;
        }

        private static bool LaCotChoPhepChinhSuaTamThoi(string columnName)
        {
            return columnName == ColLotThanhPham ||
                   columnName == ColTenCongDoan ||
                   columnName == ColTenThanhPham ||
                   columnName == ColNoiDungLoi;
        }

        private async void grvDsLoiNhapLieu_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            string columnName = grvDsLoiNhapLieu.Columns[e.ColumnIndex].Name;
            DataGridViewRow row = grvDsLoiNhapLieu.Rows[e.RowIndex];

            if (columnName == ColXacNhan)
            {
                await XuLyXacNhanAsync(row);
                return;
            }

            if (columnName == ColDetail)
            {
                MoChiTiet(row);
            }
        }

        private async Task XuLyXacNhanAsync(DataGridViewRow row)
        {
            int idLoi;
            if (!TryGetInt(row.Cells[ColIdLoi].Value, out idLoi) || idLoi <= 0)
            {
                HienThiThongBaoGif(
                    "Không xác định được ID của lỗi cần cập nhật.",
                    "LỖI DỮ LIỆU",
                    "warning");
                return;
            }

            bool confirmed = GetBool(row.Cells[ColConfirmed].Value);
            bool trangThaiMoi = !confirmed;

            if (confirmed)
            {
                DialogResult answer = HienThiXacNhan(
                    "Bạn có chắc chắn muốn chuyển trạng thái về chưa xác nhận?",
                    "XÁC NHẬN HOÀN TÁC");

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
                        DanhSachLoiNhapLieuSX_DB.CapNhatConfirmed(idLoi, trangThaiMoi)),
                    trangThaiMoi
                        ? "ĐANG XÁC NHẬN DỮ LIỆU..."
                        : "ĐANG HOÀN TÁC XÁC NHẬN...");

                if (!updated)
                {
                    HienThiThongBaoGif(
                        "Database không cập nhật được bản ghi. Trạng thái trên lưới được giữ nguyên.",
                        "KHÔNG THỂ CẬP NHẬT",
                        "warning");
                    return;
                }

                row.Cells[ColConfirmed].Value = trangThaiMoi;
                row.Cells[ColXacNhan].Value = trangThaiMoi ? "OK" : "Xác nhận";
            }
            catch (Exception ex)
            {
                HienThiThongBaoGif(
                    "Không thể cập nhật trạng thái xác nhận.\n" + ex.Message,
                    "LỖI",
                    "warning");
            }
            finally
            {
                DatTrangThaiThaoTac(true);
            }
        }

        private void MoChiTiet(DataGridViewRow row)
        {
            int idLoi;
            if (!TryGetInt(row.Cells[ColIdLoi].Value, out idLoi) || idLoi <= 0)
            {
                HienThiThongBaoGif(
                    "Không xác định được ID của lỗi cần xem chi tiết.",
                    "LỖI DỮ LIỆU",
                    "warning");
                return;
            }

            int ttThanhPhamId;
            if (!TryGetInt(row.Cells[ColTTThanhPhamId].Value, out ttThanhPhamId) ||
                ttThanhPhamId <= 0)
            {
                HienThiThongBaoGif(
                    "Không xác định được TTThanhPham_ID của dòng đang chọn.",
                    "LỖI DỮ LIỆU",
                    "warning");
                return;
            }

            string tenThanhPham = GetCellString(row, ColTenThanhPham);
            bool confirmed = GetBool(row.Cells[ColConfirmed].Value);

            using (var form = new Frm_ChiTietLoiNhapLieuSX(
                idLoi,
                ttThanhPhamId,
                tenThanhPham,
                confirmed))
            {
                Form owner = FindForm();
                if (owner == null)
                {
                    form.ShowDialog();
                }
                else
                {
                    form.ShowDialog(owner);
                }

                row.Cells[ColConfirmed].Value = form.Confirmed;
                row.Cells[ColXacNhan].Value = form.Confirmed ? "OK" : "Xác nhận";
            }
        }

        private void DatTrangThaiThaoTac(bool enabled)
        {
            btnLayDS.Enabled = enabled;
            btnXuatExcel.Enabled = enabled;
            grvDsLoiNhapLieu.Enabled = enabled;
        }

        private static void HienThiThongBaoGif(string message, string title, string iconName)
        {
            FrmWaiting.ShowGifAlert(message, title, iconName);
        }

        private DialogResult HienThiXacNhan(string message, string title)
        {
            Form owner = FindForm();
            return owner == null
                ? MessageBox.Show(
                    message,
                    title,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question)
                : MessageBox.Show(
                    owner,
                    message,
                    title,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
        }

        private static string GetCellString(DataGridViewRow row, string columnName)
        {
            object value = row.Cells[columnName].Value;
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value);
        }

        private static bool TryGetInt(object value, out int result)
        {
            if (value == null || value == DBNull.Value)
            {
                result = 0;
                return false;
            }

            return int.TryParse(Convert.ToString(value), out result);
        }

        private static bool GetBool(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            int number;
            return int.TryParse(Convert.ToString(value), out number) && number == 1;
        }
    }
}