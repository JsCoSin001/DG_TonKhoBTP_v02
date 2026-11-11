using DG_TonKhoBTP_v02.Models.Kho.TonKho;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.TonKho
{
    /// <summary>
    /// Hiển thị trực tiếp trên các control đã khai báo trong Frm_ChiTietTonKho.Designer.cs.
    /// Phần 1: các TextBox của dòng tồn. Phần 2: dgvChiTiet theo loại cuộn.
    /// Không tạo thêm form hoặc DataGridView mới; không sửa dữ liệu trong DB.
    /// </summary>
    public partial class Frm_ChiTietTonKho : Form
    {
        private readonly ToolTip _canhBaoToolTip;

        // Constructor không tham số dành cho Windows Forms Designer.
        public Frm_ChiTietTonKho()
        {
            InitializeComponent();
            if (components == null)
                components = new System.ComponentModel.Container();
            _canhBaoToolTip = new ToolTip(components);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimumSize = new Size(940, 500);
            Text = "Chi tiết dòng tồn kho";
            label1.Text = "BẢNG BÁO CÁO CHI TIẾT";
            groupBox1.Text = "Thông tin tồn kho";
            groupBox2.Text = "Chi tiết";
            foreach (var control in new TextBox[]
            {
                tbxTenSP, tbxMaSP, tbxLOT, tbLoai, tbxSoCuon,
                tbChieuDai1C, tbxTonThucTe, tbxDatTruoc, tbxTonKhaDung,
                tbxSoDau, tbxSoCuoi
            })
            {
                control.ReadOnly = true;
                control.TabStop = false;
                control.BackColor = SystemColors.Window;
            }

            dgvChiTiet.ReadOnly = true;
            dgvChiTiet.AllowUserToAddRows = false;
            dgvChiTiet.AllowUserToDeleteRows = false;
            dgvChiTiet.RowHeadersVisible = false;
            dgvChiTiet.MultiSelect = false;
            dgvChiTiet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvChiTiet.AutoGenerateColumns = false;
            dgvChiTiet.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvChiTiet.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvChiTiet.AllowUserToOrderColumns = false;
            dgvChiTiet.BackgroundColor = SystemColors.Window;
        }

        // Chỉ dùng từ nút Xem ở grvShowBaoCao (báo cáo tổng hợp hoặc tìm kiếm).
        internal Frm_ChiTietTonKho(BCTonKho_ExcelDong dong) : this()
        {
            if (dong == null) throw new ArgumentNullException(nameof(dong));
            if (dong.ChiTiet == null)
                throw new ArgumentException("Dòng báo cáo chưa được tải dữ liệu chi tiết.", nameof(dong));
            NapDuLieu(dong);
        }

        private void NapDuLieu(BCTonKho_ExcelDong dong)
        {
            string donVi = string.IsNullOrWhiteSpace(dong.DonVi) ? "m" : dong.DonVi.Trim();
            bool laCuonChan = string.Equals(dong.Loai, "Cuộn", StringComparison.OrdinalIgnoreCase);

            // PHẦN 1: Điền đúng các TextBox do Designer tạo, không ghép thành TextBox đa dòng.
            tbxTenSP.Text = dong.TenSanPham ?? string.Empty;
            tbxMaSP.Text = dong.MaSanPham ?? string.Empty;
            tbxLOT.Text = dong.LotHienThiBang ?? string.Empty;
            tbLoai.Text = dong.Loai ?? string.Empty;
            tbxSoCuon.Text = So(dong.SoCuon);
            tbChieuDai1C.Text = dong.ThieuChieuDaiHienTai ? string.Empty : So(dong.ChieuDaiMoiCuon);
            tbxTonThucTe.Text = dong.ThieuChieuDaiHienTai ? string.Empty : So(dong.ChieuDaiTonThucTe);
            tbxDatTruoc.Text = So(dong.ChieuDaiTonDatTruoc);
            tbxTonKhaDung.Text = dong.ThieuChieuDaiHienTai ? string.Empty : So(dong.ChieuDaiTonKhaDung);
            tbxSoDau.Text = laCuonChan ? string.Empty : SoNullable(dong.SoDau);
            tbxSoCuoi.Text = laCuonChan ? string.Empty : SoNullable(dong.SoCuoi);

            label7.Text = laCuonChan ? "Chiều dài 1 cuộn (" + donVi + ")" : "Chiều dài còn (" + donVi + ")";
            label8.Text = "Tồn thực tế (" + donVi + ")";
            label9.Text = "Đặt trước (" + donVi + ")";
            label10.Text = "Tồn khả dụng (" + donVi + ")";

            if (dong.LaBatThuong)
            {
                groupBox1.Text = "Thông tin tồn kho (CÓ CẢNH BÁO)";
                _canhBaoToolTip.SetToolTip(groupBox1, dong.CanhBao);
                foreach (var box in new[] { tbxTonThucTe, tbxDatTruoc, tbxTonKhaDung })
                    _canhBaoToolTip.SetToolTip(box, dong.CanhBao);
            }

            // PHẦN 2: Chỉ dùng dgvChiTiet có sẵn, thay đổi cột theo loại cuộn.
            if (laCuonChan) NapCuonChan(dong);
            else NapCuonLe(dong);
            dgvChiTiet.ClearSelection();
        }

        private void NapCuonChan(BCTonKho_ExcelDong dong)
        {
            groupBox2.Text = "Chi tiết LOT góp tồn và đặt trước FIFO (dự kiến)";
            TaoCot(4, true,
                ("LOT", 270), ("Số cuộn còn", 160),
                ("Chiều dài 1 cuộn", 200), ("Đặt trước", 460));

            // FIFO theo nguồn nhập; chỉ sau đó mới gộp các nguồn của cùng một LOT.
            var sources = dong.ChiTiet!.Nguon
                .Where(x => x.SoCuonGop > 0)
                .OrderBy(x => x.NgayNhapKho ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => x.TTCuonDayId)
                .ToList();
            decimal choPhanBo = dong.ChieuDaiMoiCuon > 0
                ? dong.ChieuDaiTonDatTruoc / dong.ChieuDaiMoiCuon : 0m;
            var theoNguon = new Dictionary<long, decimal>();
            foreach (var source in sources)
            {
                decimal duKien = Math.Min(Math.Max(choPhanBo, 0m), Math.Max(source.SoCuonGop, 0));
                theoNguon[source.TTCuonDayId] = duKien;
                choPhanBo -= duKien;
            }

            foreach (var lot in sources.GroupBy(x => x.MaBin ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                long soCuonCon = lot.Sum(x => x.SoCuonGop);
                decimal daDatTruoc = lot.Sum(x => theoNguon[x.TTCuonDayId]);
                dgvChiTiet.Rows.Add(lot.Key, So(soCuonCon),
                    dong.ChieuDaiMoiCuon > 0 ? So(dong.ChieuDaiMoiCuon) : string.Empty,
                    daDatTruoc > 0 ? "Dự kiến: " + SoCuon(daDatTruoc) + " cuộn" : string.Empty);
            }

            if (choPhanBo > 0)
            {
                groupBox2.Text = "Chi tiết LOT (CẢNH BÁO: số cuộn dự kiến vượt nguồn tồn)";
                _canhBaoToolTip.SetToolTip(groupBox2,
                    "Chưa phân bổ FIFO: " + SoCuon(choPhanBo) + " cuộn. " +
                    "Nhu cầu vượt nguồn tồn hoặc thiếu bản ghi nguồn. Không tự phân bổ vào LOT cuối.");
            }
            else if (sources.Count == 0)
            {
                groupBox2.Text = "Chi tiết LOT (không có nguồn đang góp tồn)";
            }
        }

        private void NapCuonLe(BCTonKho_ExcelDong dong)
        {
            groupBox2.Text = "Chi tiết lịch sử cắt và đặt trước";
            TaoCot(9, false,
                ("LOT / Mã cuộn", 205), ("CD trước cắt", 130),
                ("Trước cắt", 145), ("Chiều dài cắt", 120),
                ("Đoạn đã cắt", 160), ("Sau cắt", 145),
                ("Thời gian cắt", 175), ("Người cắt", 125), ("Đặt trước", 320));

            // Đặt trước: từng đoạn chưa có lịch sử cắt; phân cách bằng dấu ';'.
            // Đây là nhu cầu HIỆN TẠI, không lặp lại ở mọi lần cắt trong quá khứ.
            var chiTiet = dong.ChiTiet!;
            string datTruoc = string.Join("; ", chiTiet.DoanDatTruoc
                .OrderBy(x => x.KeHoachCatChiTietId)
                .Select(x => x.ChieuDai.HasValue
                    ? x.ChieuDai.Value.ToString("#,##0.############################", Vi) + " m"
                    : "Chưa xác định"));

            string maCuon = (dong.MaBinLot ?? string.Empty).Trim();
            string lotNguon = (dong.LotNguon ?? string.Empty).Trim();
            string hienThiLot = string.IsNullOrWhiteSpace(lotNguon) ||
                string.Equals(lotNguon, maCuon, StringComparison.OrdinalIgnoreCase)
                ? maCuon : lotNguon + " / " + maCuon;

            var lanCat = chiTiet.LichSuCat
                .OrderByDescending(x => x.ThoiGian ?? string.Empty, StringComparer.Ordinal)
                .ThenByDescending(x => x.Id)
                .ToList();
            if (lanCat.Count == 0)
            {
                // Không tự suy diễn chiều dài trước cắt, thời gian hoặc người cắt.
                dgvChiTiet.Rows.Add(hienThiLot, "", "", "", "", "", "", "", datTruoc);
                groupBox2.Text = "Chi tiết (chưa có dữ liệu lịch sử cắt)";
                return;
            }

            bool dongMoiNhat = true;
            foreach (var lan in lanCat)
            {
                // Chiều dài ngay trước lần cắt được tính từ hai mốc trước cắt.
                string chieuDaiTruoc = lan.SoDauTruoc.HasValue && lan.SoCuoiTruoc.HasValue
                    ? So(Math.Abs((decimal)lan.SoCuoiTruoc.Value - lan.SoDauTruoc.Value))
                    : string.Empty;
                int row = dgvChiTiet.Rows.Add(hienThiLot, chieuDaiTruoc,
                    Moc(lan.SoDauTruoc, lan.SoCuoiTruoc), SoNullable(lan.ChieuDaiCat),
                    Moc(lan.SoDauCat, lan.SoCuoiCat), Moc(lan.SoDauSau, lan.SoCuoiSau),
                    DinhDangThoiGian(lan.ThoiGian), lan.NguoiThucHien ?? string.Empty,
                    dongMoiNhat ? datTruoc : string.Empty);
                if (string.Equals(lan.LoaiNguon, "CUON_CHAN", StringComparison.OrdinalIgnoreCase))
                {
                    dgvChiTiet.Rows[row].Cells[0].ToolTipText =
                        "Lần cắt chuyển từ cuộn chẵn thành cuộn lẻ";
                }
                dongMoiNhat = false;
            }
            if (lanCat.Any(x => string.Equals(x.LoaiNguon, "CUON_CHAN", StringComparison.OrdinalIgnoreCase)))
                groupBox2.Text = "Chi tiết lịch sử cắt (bao gồm chuyển cuộn chẵn thành cuộn lẻ)";
        }

        private void TaoCot(int expectedCount, bool fill, params (string Header, int Width)[] columns)
        {
            dgvChiTiet.DataSource = null;
            dgvChiTiet.Rows.Clear();
            dgvChiTiet.Columns.Clear();
            if (columns.Length != expectedCount)
                throw new ArgumentException("Số cột chi tiết không đúng thiết kế.", nameof(columns));
            dgvChiTiet.AutoSizeColumnsMode = fill
                ? DataGridViewAutoSizeColumnsMode.Fill : DataGridViewAutoSizeColumnsMode.None;
            for (int i = 0; i < columns.Length; i++)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = "colChiTiet" + i,
                    HeaderText = columns[i].Header,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ReadOnly = true,
                    Width = columns[i].Width,
                    MinimumWidth = 90
                };
                if (fill) col.FillWeight = columns[i].Width;
                dgvChiTiet.Columns.Add(col);
            }
        }

        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private static string So(decimal value) => value.ToString("N0", Vi);
        private static string SoCuon(decimal value) => value.ToString("#,##0.##", Vi);
        private static string SoNullable(long? value) => value.HasValue ? So(value.Value) : string.Empty;
        private static string Moc(long? dau, long? cuoi) => dau.HasValue && cuoi.HasValue
            ? So(dau.Value) + " – " + So(cuoi.Value) : string.Empty;
        private static string DinhDangThoiGian(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (DateTime.TryParseExact(raw, new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
                return time.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.GetCultureInfo("vi-VN"));
            return raw;
        }
    }
}
