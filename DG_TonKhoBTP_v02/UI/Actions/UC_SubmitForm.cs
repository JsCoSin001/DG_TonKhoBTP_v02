using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.NghiepVuKhac.SanXuat;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;
using PrinterModel = DG_TonKhoBTP_v02.Models.PrinterModel;
using Validator = DG_TonKhoBTP_v02.Helper.Validator;

namespace DG_TonKhoBTP_v02.UI
{
    public partial class UC_SubmitForm : UserControl, IFormSection
    {
        public string SectionName => nameof(UC_SubmitForm);

        private readonly Timer _timerThongBao = new Timer();
        private readonly Action _onSaveSuccess;
        private static readonly string _printer = Properties.Settings.Default.PrinterName;

        // Chỉ cần thêm constant lỗi vào danh sách này nếu lỗi đó không cần
        // hiển thị hộp thoại xác nhận. Lỗi vẫn được lưu xuống database.
        private static readonly HashSet<string> DanhSachLoiKhongCanXacNhan =
            new HashSet<string>
            {
                // Ví dụ:
                 DanhSachLoiNhapLieuSX.Loi_BomNull,
                 DanhSachLoiNhapLieuSX.Loi_KhongXacDinh,
                 DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen,
                 DanhSachLoiNhapLieuSX.Loi_KhongDongBoTen,
            };

        private CongDoan _Cd;

        public UC_SubmitForm(CongDoan cd, Action onSaveSuccess = null)
        {
            InitializeComponent();

            bool inTem = _printer != "";
            cbInTem.Checked = inTem;

            _Cd = cd;
            _onSaveSuccess = onSaveSuccess;

            if (_printer != "")
            {
                cbInTem.Text = "In tem đầu ra";
                cbInTemNVL.Text = "In tem đầu vào";
            }
            else
            {
                cbInTem.Checked = true;
                cbInTem.Enabled = false;
                cbInTem.Text = "Không in tem";

                cbInTemNVL.Checked = false;
                cbInTemNVL.Enabled = false;
                cbInTemNVL.Text = "Không in tem";
            }

            // Công đoạn 9 không sử dụng và không in tem NVL.
            if (_Cd?.Id == 9)
            {
                cbInTemNVL.Checked = false;
                cbInTemNVL.Enabled = false;
                cbInTemNVL.Text = "Không in tem đầu vào";
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            var swTotal = Stopwatch.StartNew();
            Debug.WriteLine("=== [BTN LƯU] BẮT ĐẦU ===");

            FrmWaiting waiting = null;

            try
            {
                // Khóa nút Lưu để ngăn người dùng thực hiện nhiều lần lưu cùng lúc.
                btnLuu.Enabled = false;
                Debug.WriteLine("btnLuu.Enabled = false");

                // Kiểm tra trạng thái nghiệp vụ ban đầu trước khi xử lý Submit.
                // Nếu trạng thái hiện tại không cho phép lưu, hàm sẽ hiển thị thông báo
                // phù hợp và kết thúc luồng xử lý.
                if (!ValidateSubmitStatus(swTotal))
                    return;

                // Tìm form cha đang chứa UC_SubmitForm.
                // Form cha được dùng làm nguồn để thu thập dữ liệu từ các UserControl liên quan.
                Form host = GetHostForm(waiting, swTotal);
                if (host == null)
                    return;

                // Chụp toàn bộ dữ liệu của các section trên form cha thành một FormSnapshot.
                // Snapshot giúp các bước validate và xử lý sau đó không phải đọc trực tiếp
                // từng control trên giao diện.
                FormSnapshot snapshot = CaptureSnapshot(host, waiting, swTotal);
                if (snapshot == null)
                    return;

                // Lấy section UC_Edit từ snapshot và ép kiểu sang EditModel.
                // Section này cho biết thao tác hiện tại là thêm mới hay sửa dữ liệu,
                // đồng thời cung cấp ID của bản ghi cần sửa.
                if (!TryGetRequiredSection(
                        snapshot,
                        "UC_Edit",
                        out EditModel editModel,
                        out string sectionError))
                {
                    // Hiển thị lỗi cấu trúc khi snapshot thiếu UC_Edit
                    // hoặc dữ liệu section không đúng kiểu EditModel.
                    ShowStructureError(waiting, sectionError);
                    return;
                }

                // Khi KieuXuLy == 2, người dùng đang sửa một bản ghi đã tồn tại.
                // Các trường hợp còn lại được xem là thêm mới và sử dụng ID bằng 0.
                int idEdit = editModel.KieuXuLy == 2 ? editModel.Id : 0;

                // Xác nhận mã tổ trưởng ngay khi xác định đây là thao tác sửa.
                // Username chỉ tồn tại trong phạm vi lần bấm Lưu hiện tại để dùng cho bước sau.
                string confirmedUsername = null;

                if (idEdit != 0)
                {
                    using (var frmXacNhan = new Frm_ToTruongXacNhan())
                    {
                        DialogResult confirmResult = frmXacNhan.ShowDialog(host);

                        // Người dùng đóng form, mã không hợp lệ hoặc lỗi kiểm tra:
                        // dừng toàn bộ luồng và tuyệt đối không update.
                        if (confirmResult != DialogResult.OK)
                            return;

                        confirmedUsername = frmXacNhan.ConfirmedUsername;
                    }

                    if (string.IsNullOrWhiteSpace(confirmedUsername))
                    {
                        FrmWaiting.ShowGifAlert(
                            "Không xác định được username của tổ trưởng xác nhận.",
                            "LỖI");
                        return;
                    }

                    confirmedUsername = confirmedUsername.Trim();
                    Debug.WriteLine($"Tổ trưởng xác nhận: {confirmedUsername}");
                }

                // Chỉ hiển thị form chờ sau khi bước xác nhận edit đã thành công.
                waiting = ShowWaiting();
                Debug.WriteLine($"Hiển thị waiting: {swTotal.ElapsedMilliseconds} ms");

                // Thu thập thêm các section nằm bên trong UC_TTSanPham
                // và gộp chúng vào snapshot hiện tại.
                // Bước này bảo đảm snapshot có đầy đủ thông tin thành phẩm
                // và chi tiết công đoạn trước khi validate.
                MergeProductSections(host, snapshot, swTotal);

                // Validate toàn bộ dữ liệu trong snapshot và chuyển chúng thành
                // SubmitFormData hoàn chỉnh để sử dụng cho quá trình lưu và in tem.
                //
                // Dữ liệu được xử lý tại đây gồm:
                // - Thông tin ca làm việc.
                // - Thông tin nguyên vật liệu.
                // - Thông tin thành phẩm.
                // - Chi tiết công đoạn.
                // - Thông tin thêm mới hoặc chỉnh sửa.
                // - Các tùy chọn in tem.
                //
                // Nếu có lỗi nghiệp vụ, hàm sẽ hiển thị thông báo và trả về null.
                SubmitFormData submitData =
                    BuildSubmitData(snapshot, idEdit, confirmedUsername, waiting, swTotal);

                if (submitData == null)
                    return;

                // Nếu phát hiện bất thường trong mối quan hệ giữa nguyên vật liệu
                // và thành phẩm, đóng form chờ trước khi hiển thị hộp thoại xác nhận
                // để bảo đảm thông báo không bị che.
                if (!ConfirmLoiNhapLieu(host, submitData, ref waiting))
                    return;

                // Cập nhật nội dung form chờ theo tác vụ sắp thực hiện.
                // Nếu có in tem thành phẩm, thông báo sẽ thể hiện cả quá trình lưu và in.
                UpdateWaitingMessage(
                    waiting,
                    submitData.ShouldPrintThanhPham);

                // Chạy phần xử lý database và in tem trên worker thread
                // để không làm treo giao diện WinForms.
                //
                // Sau khi ExecuteSubmit hoàn tất, await sẽ đưa luồng xử lý
                // trở lại UI thread để đóng form chờ và hiển thị kết quả.
                SubmitProcessResult result =
                    await Task.Run(() => ExecuteSubmit(submitData, swTotal));

                // Đóng form chờ sau khi toàn bộ quá trình lưu và in tem đã hoàn tất.
                CloseWaitingSafe(waiting);

                // Đặt lại biến để khối finally không tiếp tục đóng cùng một form lần nữa.
                waiting = null;

                // Hiển thị thông báo kết quả dựa trên trạng thái lưu,
                // lỗi database và lỗi in tem trong SubmitProcessResult.
                ShowSubmitResult(result, submitData.IdEdit);

                // Chỉ thông báo cho màn hình cha khi dữ liệu đã được lưu thành công.
                // Callback thường được dùng để tải lại danh sách hoặc đóng màn hình nhập liệu.
                if (result.SaveSuccess)
                    _onSaveSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception trong btnLuu_Click: {ex}");
                CloseWaitingSafe(waiting);
                waiting = null;
                FrmWaiting.ShowGifAlert("LỖI: " + ex.Message, "LỖI");
            }
            finally
            {
                CloseWaitingSafe(waiting);
                btnLuu.Enabled = true;
                Debug.WriteLine($"=== [BTN LƯU] KẾT THÚC: {swTotal.ElapsedMilliseconds} ms ===");
            }
        }

        private FrmWaiting ShowWaiting()
        {
            var waiting = new FrmWaiting("ĐANG XỬ LÝ...");
            waiting.ShowAndRefresh();
            return waiting;
        }

        private static void CloseWaitingSafe(FrmWaiting waiting)
        {
            try
            {
                if (waiting != null && !waiting.IsDisposed)
                    waiting.CloseAndDispose();
            }
            catch
            {
                // Không để lỗi đóng waiting che mất kết quả chính.
            }
        }

        private bool ValidateSubmitStatus(Stopwatch swTotal)
        {
            string message = CoreHelper.TaoThongBao(lblTrangThai);
            Debug.WriteLine($"TaoThongBao: {swTotal.ElapsedMilliseconds} ms");

            if (string.IsNullOrEmpty(message))
                return true;

            _timerThongBao.Stop();
            _timerThongBao.Start();
            Debug.WriteLine($"Thoát sớm vì trạng thái form không hợp lệ: {swTotal.ElapsedMilliseconds} ms");
            return false;
        }

        private Form GetHostForm(FrmWaiting waiting, Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            Form host = FindForm();
            Debug.WriteLine($"FindForm: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

            if (host != null)
                return host;

            ShowStructureError(waiting, "Không tìm thấy form chứa UC_SubmitForm.");
            return null;
        }

        private FormSnapshot CaptureSnapshot(Form host, FrmWaiting waiting, Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();

            try
            {
                FormSnapshot snapshot = FormSnapshotBuilder.Capture(host);
                Debug.WriteLine($"Capture snapshot: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

                if (snapshot == null)
                {
                    ShowStructureError(waiting, "Không thể thu thập dữ liệu biểu mẫu.");
                    return null;
                }

                return snapshot;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Capture snapshot: {ex}");
                ShowStructureError(waiting, "Không thể thu thập dữ liệu biểu mẫu.\n" + ex.Message);
                return null;
            }
        }

        private void MergeProductSections(Form host, FormSnapshot snapshot, Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            var ucSanPham = CoreHelper.FindControlRecursive<UC_TTSanPham>(host);

            if (ucSanPham != null)
            {
                IDictionary<string, object> extraSections = ucSanPham.GetAggregateSections();
                foreach (KeyValuePair<string, object> section in extraSections)
                    snapshot.Sections[section.Key] = section.Value;
            }

            Debug.WriteLine($"Merge UC_TTSanPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");
        }

        /// <summary>
        /// Thu thập, kiểm tra và chuyển đổi toàn bộ dữ liệu trên form
        /// thành đối tượng SubmitFormData để phục vụ lưu dữ liệu.
        /// Trả về null nếu có bất kỳ bước kiểm tra nào không hợp lệ.
        /// </summary>
        private SubmitFormData BuildSubmitData(
            FormSnapshot snapshot,
            int idEdit,
            string confirmedUsername,
            FrmWaiting waiting,
            Stopwatch swTotal)
        {
            // =========================================================
            // 1. LẤY VÀ KIỂM TRA THÔNG TIN CA LÀM VIỆC
            // =========================================================

            if (!TryGetRequiredSection(
                    snapshot,
                    "UC_TTCaLamViec",
                    out ThongTinCaLamViec caLamViec,
                    out string sectionError))
            {
                ShowStructureError(waiting, sectionError);
                return null;
            }

            if (!ValidateCaLamViec(
                    caLamViec,
                    waiting,
                    swTotal))
            {
                return null;
            }

            // Công đoạn 9 luôn bỏ qua dữ liệu NVL,
            // áp dụng cho cả tạo mới và chỉnh sửa.
            bool laCongDoan9 =
                _Cd != null &&
                _Cd.Id == 9;

            // =========================================================
            // 2. XỬ LÝ NGUYÊN VẬT LIỆU
            // =========================================================

            List<TTNVLRow> nvlRows;
            List<TTNVL> nguyenVatLieu;

            if (laCongDoan9)
            {
                // Công đoạn 9:
                // - Không lấy dữ liệu NVL từ snapshot.
                // - Không kiểm tra NVL.
                // - Không chuyển đổi NVL.
                // - Truyền danh sách rỗng xuống lớp lưu database.
                nvlRows = new List<TTNVLRow>();
                nguyenVatLieu = new List<TTNVL>();

                Debug.WriteLine(
                    "Công đoạn 9: bỏ qua toàn bộ dữ liệu và kiểm tra NVL.");
            }
            else
            {
                // Các công đoạn khác giữ nguyên luồng xử lý NVL hiện tại.
                if (!TryGetRequiredSection(
                        snapshot,
                        "UC_TTNVL",
                        out nvlRows,
                        out sectionError))
                {
                    ShowStructureError(waiting, sectionError);
                    return null;
                }

                if (!ValidateNguyenVatLieu(
                        nvlRows,
                        caLamViec,
                        waiting,
                        swTotal))
                {
                    return null;
                }

                nguyenVatLieu = nvlRows
                    .Select(row => row.ToTTNVL())
                    .ToList();
            }

            // =========================================================
            // 3. LẤY VÀ KIỂM TRA THÔNG TIN THÀNH PHẨM
            // =========================================================

            if (!TryGetRequiredSection(
                    snapshot,
                    "UC_TTThanhPham",
                    out TTThanhPham thanhPham,
                    out sectionError))
            {
                ShowStructureError(waiting, sectionError);
                return null;
            }

            // Công đoạn 9 chỉ đánh dấu HanNoi = 1.
            // Không cập nhật số lượng còn lại của NVL.
            ApplyHanNoiRules(thanhPham);

            if (!ValidateThanhPham(
                    thanhPham,
                    waiting,
                    swTotal))
            {
                return null;
            }

            // =========================================================
            // 4. KIỂM TRA QUAN HỆ NGUYÊN VẬT LIỆU - THÀNH PHẨM
            // =========================================================

            // Công đoạn 9 không sử dụng NVL nên không áp dụng kiểm tra này.
            // Các hàm kiểm tra con hiện chỉ là phần khung và mặc định không báo lỗi.
            List<LoiNhapLieuData> danhSachLoiNhapLieu = laCongDoan9
                ? new List<LoiNhapLieuData>()
                : KiemTraMoiQuanHeNguyenLieuThanhPham( thanhPham, nvlRows, _Cd);



            // =========================================================
            // 5. LẤY CHI TIẾT CÔNG ĐOẠN
            // =========================================================

            SubmitCongDoanData congDoan = null;

            if (!laCongDoan9)
            {
                // Công đoạn khác 9 giữ nguyên việc kiểm tra
                // và tạo dữ liệu chi tiết công đoạn.
                congDoan = ValidateAndBuildCongDoanData( snapshot, waiting, swTotal);

                if (congDoan == null)
                    return null;
            }
            else
            {
                // Công đoạn 9 chỉ lưu:
                // - TTThanhPham
                // - ThongTinCaLamViec
                //
                // Vì vậy không tạo dữ liệu chi tiết công đoạn.
                Debug.WriteLine(
                    "Công đoạn 9: bỏ qua chi tiết công đoạn.");
            }

            // =========================================================
            // 6. ĐÓNG GÓI DỮ LIỆU SUBMIT
            // =========================================================

            return new SubmitFormData
            {
                IdEdit = idEdit,

                ConfirmedUsername = confirmedUsername,

                CongDoanId = _Cd.Id,

                ThongTinCaLamViec = caLamViec,

                ThongTinThanhPham = thanhPham,

                // Công đoạn 9 sẽ nhận danh sách rỗng.
                NguyenVatLieuRows = nvlRows,

                // Công đoạn 9 sẽ nhận danh sách rỗng.
                NguyenVatLieu = nguyenVatLieu,

                // Danh sách constant mô tả các bất thường giữa NVL và thành phẩm.
                // Công đoạn 9 luôn nhận danh sách rỗng.
                DanhSachLoiNhapLieu = danhSachLoiNhapLieu,

                // Công đoạn 9 sẽ nhận null.
                CongDoan = congDoan,

                // Vẫn cho phép in tem thành phẩm nếu có cấu hình máy in
                // và người dùng đã chọn checkbox.
                ShouldPrintThanhPham =
                    _printer != "" &&
                    cbInTem.Checked,

                // Công đoạn 9 tuyệt đối không in tem NVL.
                ShouldPrintNguyenVatLieu =
                    !laCongDoan9 &&
                    cbInTemNVL.Checked &&
                    nguyenVatLieu.Count > 0
            };
        }

        private bool ValidateCaLamViec(
            ThongTinCaLamViec caLamViec,
            FrmWaiting waiting,
            Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            int errorIndex = Validator.TTCaLamViec(caLamViec);
            Debug.WriteLine($"Validator.TTCaLamViec: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

            if (errorIndex <= 0)
                return true;

            ShowValidationError(waiting, EnumStore.ErrorCaLamViec[errorIndex]);
            Debug.WriteLine($"Lỗi TTCaLamViec (sttLoi={errorIndex}), thoát: {swTotal.ElapsedMilliseconds} ms");
            return false;
        }

        private bool ValidateNguyenVatLieu(
            List<TTNVLRow> nvlRows,
            ThongTinCaLamViec caLamViec,
            FrmWaiting waiting,
            Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            string error = Validator.TTNVL(nvlRows, caLamViec.May, _Cd);
            Debug.WriteLine($"Validator.TTNVL: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

            if (string.IsNullOrEmpty(error))
                return true;

            ShowValidationError(waiting, error);
            Debug.WriteLine($"Lỗi TTNVL, thoát: {swTotal.ElapsedMilliseconds} ms");
            return false;
        }

        private void ApplyHanNoiRules(TTThanhPham thanhPham)
        {
            if (_Cd?.Id == 9)
                thanhPham.HanNoi = 1;
        }

        private bool ValidateThanhPham(
            TTThanhPham thanhPham,
            FrmWaiting waiting,
            Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            int errorIndex = Validator.TTThanhPham(thanhPham);
            Debug.WriteLine($"Validator.TTThanhPham: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

            if (errorIndex <= 0)
                return true;

            ShowValidationError(waiting, EnumStore.ErrorTP[errorIndex]);
            Debug.WriteLine($"Lỗi TTThanhPham (sttLoi={errorIndex}), thoát: {swTotal.ElapsedMilliseconds} ms");
            return false;
        }

        /// <summary>
        /// Kiểm tra các mối quan hệ nghiệp vụ giữa thành phẩm và danh sách NVL.
        /// Hàm chỉ tổng hợp lỗi, không hiển thị giao diện và không truy cập database.
        /// </summary>
        private static List<LoiNhapLieuData> KiemTraMoiQuanHeNguyenLieuThanhPham(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            var danhSachLoi = new List<LoiNhapLieuData>();
            LoiNhapLieuData loi;

            if (
                    (loi = KiemTraBomNull(thanhPham)) != null
                        ||
                    (loi = KTraSoLuongLoaiNguyenVatLieu(thanhPham, nguyenVatLieu, congDoan)) != null
                        ||
                    (loi = KiemTraSoLuongBin(thanhPham, nguyenVatLieu, congDoan)) != null
                )
            {
                ThemLoiNeuCo(danhSachLoi, loi);
            }

            return danhSachLoi;
        }

        private static void ThemLoiNeuCo(
            List<LoiNhapLieuData> danhSachLoi,
            LoiNhapLieuData loi)
        {
            if (danhSachLoi == null ||
                loi == null ||
                string.IsNullOrWhiteSpace(loi.NoiDungLoi))
            {
                return;
            }

            if (!danhSachLoi.Any(x =>
                    x != null &&
                    string.Equals(
                        x.NoiDungLoi,
                        loi.NoiDungLoi,
                        StringComparison.Ordinal)))
            {
                danhSachLoi.Add(loi);
            }
        }

        private static LoiNhapLieuData TaoLoiNhapLieu(
            string noiDungLoi,
            string lyDoLoi)
        {
            if (string.IsNullOrWhiteSpace(noiDungLoi))
                return null;

            return new LoiNhapLieuData
            {
                NoiDungLoi = noiDungLoi.Trim(),
                LyDoLoi = string.IsNullOrWhiteSpace(lyDoLoi)
                    ? string.Empty
                    : lyDoLoi.Trim()
            };
        }

        /// <summary>
        /// Kiểm tra thành phẩm có danh sách BOM để thực hiện các kiểm tra
        /// quan hệ nguyên vật liệu hay không.
        /// </summary>
        private static LoiNhapLieuData KiemTraBomNull(TTThanhPham thanhPham)
        {
            if (thanhPham?.BomComponents != null &&
                thanhPham.BomComponents.Count > 0)
            {
                return null;
            }

            string tenThanhPham = thanhPham?.TenTP ?? string.Empty;
            int danhSachMaSpId = thanhPham?.DanhSachSP_ID ?? 0;

            string lyDo =
                $"Thành phẩm {tenThanhPham} " +
                $"(DanhSachMaSP_ID = {danhSachMaSpId}) không có BOM.";

            return TaoLoiNhapLieu(
                DanhSachLoiNhapLieuSX.Loi_BomNull,
                lyDo);
        }

        private static string KiemTraThanhPhamNguyenLieuKhongKhopBOM(
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            if (nguyenVatLieu == null || nguyenVatLieu.Count == 0)
                return null;

            bool coNguyenVatLieuKhongKhop;

            if (congDoan?.Id == 0)
            {
                // Mỗi NVL đều phải chứa chính xác cụm " 8mm ".
                coNguyenVatLieuKhongKhop = nguyenVatLieu.Any(nvl =>
                    string.IsNullOrEmpty(nvl?.TenNVL) ||
                    nvl.TenNVL.IndexOf(
                        " 8.0mm ",
                        StringComparison.OrdinalIgnoreCase) < 0);
            }
            else
            {
                coNguyenVatLieuKhongKhop = nguyenVatLieu.Any(
                    nvl => nvl != null && nvl.IsCorrect == false);
            }

            return coNguyenVatLieuKhongKhop
                ? DanhSachLoiNhapLieuSX.Loi_TP_Nl_KhongKhop
                : null;
        }

        /// <summary>
        /// Xác định component BOM có bắt buộc xuất hiện hay không.
        /// Component khác KieuSP = "NVL" luôn bắt buộc. Component NVL chỉ
        /// bắt buộc khi được bật Active trong bảng DanhSachNVLBatBuoc.
        /// </summary>
        private static bool LaComponentBatBuoc(BomComponentData component)
        {
            if (component == null)
                return true;

            string kieuSP = (component.ComponentKieuSP ?? string.Empty).Trim();
            if (!string.Equals(kieuSP, "NVL", StringComparison.OrdinalIgnoreCase))
                return true;

            return component.LaNVLBatBuoc;
        }

        private static string TaoLyDoBomYeuCau(
            IEnumerable<BomComponentData> components)
        {
            List<BomComponentData> danhSach = (components ??
                    Enumerable.Empty<BomComponentData>())
                .Where(x => x != null)
                .GroupBy(x => x.ComponentId)
                .Select(g => g.First())
                .ToList();

            if (danhSach.Count == 0)
                return string.Empty;

            return "BOM yêu cầu " + string.Join(
                "; ",
                danhSach.Select(x =>
                    $"{x.ComponentTen ?? string.Empty} " +
                    $"(ComponentId = {x.ComponentId})")) + ".";
        }

        /// <summary>
        /// Kiểm tra mỗi component bắt buộc trong BOM có xuất hiện ít nhất
        /// một lần trong danh sách nguyên vật liệu thực tế hay không.
        /// </summary>
        private static LoiNhapLieuData KTraSoLuongLoaiNguyenVatLieu(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            if (nguyenVatLieu == null || nguyenVatLieu.Count == 0)
            {
                return TaoLoiNhapLieu(
                    DanhSachLoiNhapLieuSX.Loi_SoLuongNVL,
                    TaoLyDoBomYeuCau(thanhPham?.BomComponents));
            }

            // Công đoạn kéo rút:
            // Loại NVL phải tương ứng với loại được xác định từ tên component BOM.
            if (congDoan?.Id == 0)
            {
                LoaiBomCongDoan0 loaiBom =
                    KiemTraBomCongDoan0Helper.XacDinhLoaiBom(
                        thanhPham.BomComponents);

                if (loaiBom == LoaiBomCongDoan0.KhongXacDinh)
                {
                    return TaoLoiNhapLieu(
                        DanhSachLoiNhapLieuSX.Loi_KhongXacDinh,
                        "Không xác định được loại BOM.");
                }

                bool coNguyenVatLieuKhongHopLe = nguyenVatLieu.Any(nvl =>
                    nvl == null ||
                    !KiemTraBomCongDoan0Helper.TenNguyenVatLieuPhuHop(
                        loaiBom,
                        nvl.TenNVL));

                return coNguyenVatLieuKhongHopLe
                    ? TaoLoiNhapLieu(
                        DanhSachLoiNhapLieuSX.Loi_TP_Nl_KhongKhop,
                        TaoLyDoBomYeuCau(thanhPham.BomComponents))
                    : null;
            }

            // Công đoạn 1 bỏ qua kiểm tra tên nguyên liệu so với bom.
            // Các công đoạn khác kiểm tra NVL có thuộc BOM hay không.
            if (congDoan?.Id != 1)
            {
                var componentIdsThucTe = new HashSet<int>(
                    nguyenVatLieu
                        .Where(nvl => nvl?.DanhSachMaSP_ID != null)
                        .Select(nvl => nvl.DanhSachMaSP_ID.Value));

                List<BomComponentData> componentBiThieu = thanhPham.BomComponents
                    .Where(LaComponentBatBuoc)
                    .Where(component =>
                        component == null ||
                        !componentIdsThucTe.Contains(component.ComponentId))
                    .ToList();

                return componentBiThieu.Count > 0
                    ? TaoLoiNhapLieu(
                        DanhSachLoiNhapLieuSX.Loi_SoLuongNVL,
                        TaoLyDoBomYeuCau(componentBiThieu))
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra số lượng Bin theo cấu trúc tên và các quy tắc dự phòng.
        /// </summary>
        private static LoiNhapLieuData KiemTraSoLuongBin(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            string lyDoLoi;
            string noiDungLoi = KiemTraSoLuongBinHelper.KiemTra(
                thanhPham,
                nguyenVatLieu,
                congDoan,
                out lyDoLoi);

            return TaoLoiNhapLieu(noiDungLoi, lyDoLoi);
        }

        /// <summary>
        /// Khung kiểm tra khối lượng/chiều dài giữa thành phẩm và NVL.
        /// Mặc định không báo lỗi cho tới khi bổ sung quy tắc nghiệp vụ.
        /// </summary>
        private static string KiemTraKhoiLuongChieuDai(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu)
        {
            bool coLoi = false;

            if (coLoi)
                return DanhSachLoiNhapLieuSX.Loi_KhoiLuong;

            return null;
        }

        private SubmitCongDoanData ValidateAndBuildCongDoanData(
            FormSnapshot snapshot,
            FrmWaiting waiting,
            Stopwatch swTotal)
        {
            var swStep = Stopwatch.StartNew();
            List<object> chiTietCongDoan = Validator.KiemTraChiTietCongDoan(snapshot);
            Debug.WriteLine($"Validator.KiemTraChiTietCongDoan: {swStep.ElapsedMilliseconds} ms (tổng: {swTotal.ElapsedMilliseconds} ms)");

            if (chiTietCongDoan == null || chiTietCongDoan.Count == 0 || chiTietCongDoan[0] == null)
            {
                ShowValidationError(waiting, "Chi tiết công đoạn chưa hợp lệ");
                Debug.WriteLine($"Chi tiết công đoạn chưa hợp lệ, thoát: {swTotal.ElapsedMilliseconds} ms");
                return null;
            }

            return new SubmitCongDoanData
            {
                ChiTietCongDoan = chiTietCongDoan[0],
                CaiDatCDBoc = chiTietCongDoan.Count > 1
                    ? chiTietCongDoan[1] as CaiDatCDBoc
                    : null
            };
        }

        private static bool TryGetRequiredSection<T>(
            FormSnapshot snapshot,
            string sectionName,
            out T section,
            out string errorMessage)
            where T : class
        {
            section = null;
            errorMessage = null;

            if (snapshot?.Sections == null)
            {
                errorMessage = "Snapshot biểu mẫu không hợp lệ.";
                return false;
            }

            if (!snapshot.Sections.TryGetValue(sectionName, out object value))
            {
                errorMessage = $"Không tìm thấy dữ liệu {sectionName}.";
                return false;
            }

            section = value as T;
            if (section != null)
                return true;

            errorMessage = $"Dữ liệu {sectionName} không đúng kiểu yêu cầu ({typeof(T).Name}).";
            return false;
        }

        private static void ShowValidationError(FrmWaiting waiting, string message)
        {
            CloseWaitingSafe(waiting);
            FrmWaiting.ShowGifAlert(message);
        }

        private static void ShowStructureError(FrmWaiting waiting, string message)
        {
            CloseWaitingSafe(waiting);
            FrmWaiting.ShowGifAlert(message, "LỖI");
        }

        /// <summary>
        /// Xác định lỗi có cần hiển thị để người dùng xác nhận hay không.
        /// Chỉ cần thêm tên lỗi vào DanhSachLoiKhongCanXacNhan để ẩn lỗi đó
        /// khỏi popup; lỗi vẫn được giữ nguyên trong dữ liệu lưu database.
        /// </summary>
        private static bool LoiNhapLieuCanXacNhan(string loi)
        {
            return !string.IsNullOrWhiteSpace(loi) &&
                   !DanhSachLoiKhongCanXacNhan.Contains(loi);
        }

        /// <summary>
        /// Hiển thị danh sách bất thường để người dùng quyết định có tiếp tục lưu hay không.
        /// Form chờ được đóng trước khi hiển thị và chỉ mở lại khi người dùng chọn Yes.
        /// </summary>
        private bool ConfirmLoiNhapLieu(
            Form host,
            SubmitFormData submitData,
            ref FrmWaiting waiting)
        {
            if (submitData == null || submitData.CongDoanId == 9)
                return true;

            List<string> danhSachLoi = submitData.DanhSachLoiNhapLieu?
                .Where(x => x != null && LoiNhapLieuCanXacNhan(x.NoiDungLoi))
                .Select(x => x.NoiDungLoi)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList()
                ?? new List<string>();

            if (danhSachLoi.Count == 0)
                return true;

            CloseWaitingSafe(waiting);
            waiting = null;

            string noiDungLoi = string.Join(
                Environment.NewLine,
                danhSachLoi.Select(x => "• " + x));

            string message =
                "Phát hiện các bất thường giữa nguyên vật liệu và thành phẩm:" +
                Environment.NewLine + Environment.NewLine +
                noiDungLoi +
                Environment.NewLine + Environment.NewLine +
                "Bạn có muốn tiếp tục lưu dữ liệu không?";

            DialogResult result = MessageBox.Show(
                host,
                message,
                "XÁC NHẬN LƯU DỮ LIỆU",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
                return false;

            waiting = ShowWaiting();
            return true;
        }

        private static void UpdateWaitingMessage(FrmWaiting waiting, bool shouldPrint)
        {
            if (waiting == null || waiting.IsDisposed)
                return;

            waiting.MessageText = shouldPrint
                ? "ĐANG LƯU DỮ LIỆU VÀ IN TEM..."
                : "ĐANG LƯU DỮ LIỆU...";
        }

        private SubmitProcessResult ExecuteSubmit(SubmitFormData data, Stopwatch swTotal)
        {
            var swTask = Stopwatch.StartNew();
            Debug.WriteLine("=== [BTN LƯU] Task.Run BẮT ĐẦU ===");

            var result = new SubmitProcessResult();

            try
            {
                SaveOrUpdate(data, result);

                if (!result.SaveSuccess)
                {
                    Debug.WriteLine($"Lưu thất bại sau: {swTask.ElapsedMilliseconds} ms, lỗi: {result.SaveError}");
                    return result;
                }

                try
                {
                    PrintLabels(data);
                }
                catch (Exception exPrint)
                {
                    result.HasPrintError = true;
                    result.PrintError = exPrint.Message;
                    Debug.WriteLine($"Lỗi in tem: {exPrint.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception trong worker: {ex}");

                if (!result.SaveSuccess)
                {
                    result.SaveError = "LỖI LƯU DỮ LIỆU: " + ex.Message;
                }
                else
                {
                    result.HasPrintError = true;
                    result.PrintError = "\n" + ex.Message;
                }
            }
            finally
            {
                Debug.WriteLine($"Worker kết thúc sau: {swTask.ElapsedMilliseconds} ms; Tổng: {swTotal.ElapsedMilliseconds} ms");
            }

            return result;
        }

        private static void SaveOrUpdate(
            SubmitFormData data,
            SubmitProcessResult result)
        {
            var swDb = Stopwatch.StartNew();
            string error;

            bool laCongDoan9 =
                data.CongDoanId == 9;

            if (laCongDoan9 && data.IdEdit == 0)
            {
                result.SaveSuccess = SubmitForm_DB.SaveDataCongDoan9(data.ThongTinCaLamViec, data.ThongTinThanhPham, out error);

                Debug.WriteLine(
                    $"SaveDataCongDoan9: {swDb.ElapsedMilliseconds} ms");
            }
            else if (laCongDoan9)
            {
                result.SaveSuccess = SubmitForm_DB.UpdateDataCongDoan9(
                    data.IdEdit,
                    data.ThongTinCaLamViec,
                    data.ThongTinThanhPham,
                    data.ConfirmedUsername,
                    out error);

                Debug.WriteLine(
                    $"UpdateDataCongDoan9: {swDb.ElapsedMilliseconds} ms");
            }
            else if (data.IdEdit == 0)
            {
                result.SaveSuccess = SubmitForm_DB.SaveDataSanPhamVoiDanhSachLoiNhapLieu(
                    data.ThongTinCaLamViec,
                    data.ThongTinThanhPham,
                    data.NguyenVatLieu,
                    data.CongDoan,
                    data.DanhSachLoiNhapLieu,
                    out error);

                Debug.WriteLine(
                    $"SaveDataSanPhamVoiDanhSachLoiNhapLieu: {swDb.ElapsedMilliseconds} ms");
            }
            else
            {
                result.SaveSuccess = SubmitForm_DB.UpdateDataSanPhamVoiDanhSachLoiNhapLieu(
                    data.IdEdit,
                    data.ThongTinCaLamViec,
                    data.ThongTinThanhPham,
                    data.NguyenVatLieu,
                    data.CongDoan,
                    data.DanhSachLoiNhapLieu,
                    data.ConfirmedUsername,
                    out error);

                Debug.WriteLine(
                    $"UpdateDataSanPhamVoiDanhSachLoiNhapLieu: {swDb.ElapsedMilliseconds} ms");
            }

            if (!result.SaveSuccess)
            {
                result.SaveError = string.IsNullOrEmpty(error)
                    ? "LƯU KHÔNG THÀNH CÔNG."
                    : error;
            }
        }


        private void PrintLabels(SubmitFormData data)
        {
            if (data.ShouldPrintThanhPham)
            {
                var swPrint = Stopwatch.StartNew();
                PrinterModel printer = BuildThanhPhamPrinter(data);
                PrintHelper.PrintLabel(printer);
                Debug.WriteLine($"In tem thành phẩm: {swPrint.ElapsedMilliseconds} ms");
            }

            PrintNguyenVatLieuLabels(data);
        }

        private PrinterModel BuildThanhPhamPrinter(SubmitFormData data)
        {
            string ghiChu = data.ThongTinThanhPham.GhiChu;

            if (EnumStore.MayTheoCongDoan.TryGetValue("Ben_CU_AL", out var dsMay)
                && dsMay.Contains(data.ThongTinCaLamViec.May, StringComparer.OrdinalIgnoreCase)
                && data.CongDoan?.ChiTietCongDoan is CD_BenRuot benRuot)
            {
                ghiChu = $"{benRuot.DKSoi}x{benRuot.SoSoi?.ToString() ?? ""} sợi\n" + ghiChu;
            }

            string mau = string.Empty;
            if (data.CongDoanId == 3)
                mau = ((CD_BocMach)data.CongDoan.ChiTietCongDoan).Mau;

            return new PrinterModel
            {
                NgaySX = DateTime.ParseExact(
                        data.ThongTinCaLamViec.Ngay,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                    .ToString("dd/MM/yyyy"),
                CaSX = data.ThongTinCaLamViec.Ca,
                Mau = mau,
                KhoiLuong = data.ThongTinThanhPham.KhoiLuongSau.ToString(),
                ChieuDai = data.ThongTinThanhPham.ChieuDaiSau.ToString(),
                TenSP = data.ThongTinThanhPham.TenTP,
                MaBin = data.ThongTinThanhPham.MaBin,
                MaSP = data.ThongTinThanhPham.MaTP,
                DanhGia = "",
                TenCN = data.ThongTinCaLamViec.NguoiLam,
                GhiChu = ghiChu
            };
        }

        private static void PrintNguyenVatLieuLabels(SubmitFormData data)
        {
            if (data == null || data.CongDoanId == 9)
                return;

            if (!data.ShouldPrintNguyenVatLieu)
                return;

            if (data.CongDoanId == 0)
                return;

            List<string> dsBin = BuildNguyenVatLieuBinList(data.NguyenVatLieu);
            if (dsBin.Count == 0)
                return;

            var swGetPrinterData = Stopwatch.StartNew();
            List<PrinterModel> printers = SubmitForm_DB.GetPrinterDataByListBin(dsBin);
            Debug.WriteLine($"GetPrinterDataByListBin: {swGetPrinterData.ElapsedMilliseconds} ms");

            if (printers == null || printers.Count == 0)
                return;

            var swPrint = Stopwatch.StartNew();
            foreach (PrinterModel printer in printers)
                PrintHelper.PrintLabel(printer);

            Debug.WriteLine($"In tem NVL: {swPrint.ElapsedMilliseconds} ms");
        }

        private static List<string> BuildNguyenVatLieuBinList(List<TTNVL> nguyenVatLieu)
        {
            var result = new List<string>();

            if (nguyenVatLieu == null || nguyenVatLieu.Count == 0)
                return result;

            foreach (TTNVL nvl in nguyenVatLieu)
            {
                bool isEmptyKg = nvl.DonVi == "KG" && nvl.KlConLai == 0;
                bool isEmptyMeter = nvl.DonVi == "M" && nvl.CdConLai == 0;
                bool isTemporary = nvl.Id < 0;

                if (isEmptyKg || isEmptyMeter || isTemporary)
                    continue;

                result.Add(nvl.BinNVL);
            }

            return result;
        }

        private static void ShowSubmitResult(SubmitProcessResult result, int idEdit)
        {
            string message = idEdit > 0 ? "SỬA" : "LƯU";
            string icon = EnumStore.Icon.Warning;

            if (result.SaveSuccess)
            {
                message += " THÀNH CÔNG ";
                icon = EnumStore.Icon.Success;
            }
            else
            {
                message += " KHÔNG THÀNH CÔNG\nLỗi: " + result.SaveError;
            }

            FrmWaiting.ShowGifAlert(message, "THÔNG BÁO", icon);

            if (result.SaveSuccess
                && result.HasPrintError
                && !string.IsNullOrEmpty(result.PrintError))
            {
                FrmWaiting.ShowGifAlert(result.PrintError.ToUpper(), "LỖI IN");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            var host = FindForm();
            if (host == null)
                return;

            ControlCleaner.ClearAll(host);

            foreach (Control control in host.Controls)
                ClearSectionRecursive(control);
        }

        private void ClearSectionRecursive(Control root)
        {
            if (root is IFormSection section)
                section.ClearInputs();

            foreach (Control child in root.Controls)
                ClearSectionRecursive(child);
        }

        public object GetData()
        {
            return new Submit
            {
                IsInTemTP = cbInTem.Checked,
                IsInTemNVL = cbInTemNVL.Checked
            };
        }

        public void ClearInputs()
        {
            cbInTem.Checked = true;
            cbInTemNVL.Checked = false;
        }

        private void UC_SubmitForm_Load(object sender, EventArgs e)
        {
            _timerThongBao.Interval = 5000;
            _timerThongBao.Tick += (s, args) =>
            {
                lblTrangThai.Visible = false;
                _timerThongBao.Stop();
            };

            if (_Cd?.Id == 9)
            {
                cbInTemNVL.Checked = false;
                cbInTemNVL.Enabled = false;
                cbInTemNVL.Text = "Không in tem đầu vào";
            }

            if (_Cd?.Id == 0 || _Cd?.Id == 1)
                cbInTemNVL.Checked = false;
        }
    }
}