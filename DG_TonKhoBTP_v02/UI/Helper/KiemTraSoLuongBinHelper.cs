using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    internal static class KiemTraSoLuongBinHelper
    {
        internal static string KiemTra(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            var nhatKy = new StringBuilder();

            nhatKy.AppendLine("===== KIỂM TRA SỐ LƯỢNG BIN =====");
            nhatKy.AppendLine($"TP: {thanhPham?.TenTP ?? "null"}");

            if (thanhPham == null ||
                nguyenVatLieu == null ||
                string.IsNullOrEmpty(thanhPham.TenTP) ||
                nguyenVatLieu.Any(
                    item => item == null ||
                            string.IsNullOrEmpty(item.TenNVL)))
            {
                return KiemTraSoLuongBinDuPhong(
                    thanhPham,
                    nguyenVatLieu,
                    congDoan,
                    "Dữ liệu TP/BTP bị null hoặc tên bị rỗng.",
                    nhatKy);
            }

            if (nguyenVatLieu.Count == 0)
            {
                nhatKy.AppendLine("Không có BTP để đối chiếu.");
                nhatKy.AppendLine(
                    $"Kết quả: {DanhSachLoiNhapLieuSX.Loi_SoLuongBin}");

                GhiLogKiemTraSoLuongBin(nhatKy.ToString());
                return DanhSachLoiNhapLieuSX.Loi_SoLuongBin;
            }

            try
            {
                KetQuaPhanTich ketQuaTP =
                    PhanTachCauTrucDay.PhanTich(
                        ChuanHoaTenTruocKhiPhanTich(thanhPham.TenTP));

                var danhSachCumTP = new List<CumQuyDoi>();

                foreach (CumCauTruc cum in ketQuaTP.DanhSachCum)
                {
                    CumQuyDoi cumQuyDoi;
                    string loiQuyDoi;

                    if (!ThuQuyDoiCum(cum, out cumQuyDoi, out loiQuyDoi))
                    {                       

                        return KiemTraSoLuongBinDuPhong(
                            thanhPham,
                            nguyenVatLieu,
                            congDoan,
                            $"Không quy đổi được cụm TP '{cum.NoiDungGoc}': " +
                            loiQuyDoi,
                            nhatKy);
                    }

                    danhSachCumTP.Add(cumQuyDoi);

                    nhatKy.AppendLine(
                        $"TP - Cụm '{cumQuyDoi.NoiDungGoc}' " +
                        $"=> cấu trúc '{cumQuyDoi.TenCauTruc}', " +
                        $"hệ số {cumQuyDoi.HeSo}");
                }

                var tongBTP = new Dictionary<string, long>(
                    StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < nguyenVatLieu.Count; i++)
                {
                    TTNVLRow dongNVL = nguyenVatLieu[i];

                    KetQuaPhanTich ketQuaBTP =
                        PhanTachCauTrucDay.PhanTich(
                            ChuanHoaTenTruocKhiPhanTich(dongNVL.TenNVL));

                    foreach (CumCauTruc cum in ketQuaBTP.DanhSachCum)
                    {
                        CumQuyDoi cumQuyDoi;
                        string loiQuyDoi;

                        if (!ThuQuyDoiCum(cum, out cumQuyDoi, out loiQuyDoi))
                        {
                            return KiemTraSoLuongBinDuPhong(
                                thanhPham,
                                nguyenVatLieu,
                                congDoan,
                                $"Không quy đổi được cụm BTP '{cum.NoiDungGoc}' " +
                                $"tại dòng {i + 1}: {loiQuyDoi}",
                                nhatKy);
                        }

                        CongDonSoLuong(
                            tongBTP,
                            cumQuyDoi.TenCauTruc,
                            cumQuyDoi.HeSo);

                        nhatKy.AppendLine(
                            $"BTP {i + 1}: '{dongNVL.TenNVL}', " +
                            $"cụm '{cumQuyDoi.NoiDungGoc}' " +
                            $"=> cấu trúc '{cumQuyDoi.TenCauTruc}', " +
                            $"đóng góp {cumQuyDoi.HeSo}");
                    }
                }

                if (tongBTP.Count == 0)
                {
                    nhatKy.AppendLine("Không có cấu trúc BTP sau khi phân tích.");
                    nhatKy.AppendLine(
                        $"Kết quả: {DanhSachLoiNhapLieuSX.Loi_SoLuongBin}");

                    GhiLogKiemTraSoLuongBin(nhatKy.ToString());
                    return DanhSachLoiNhapLieuSX.Loi_SoLuongBin;
                }

                var soLuongTPYeuCau = new Dictionary<string, long>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (CumQuyDoi cumTP in danhSachCumTP)
                {
                    // Trường hợp thông thường:
                    // TP và BTP sau khi bỏ các hệ số đầu có cùng tên cấu trúc.
                    if (tongBTP.ContainsKey(cumTP.TenCauTruc))
                    {
                        CongDonSoLuong(
                            soLuongTPYeuCau,
                            cumTP.TenCauTruc,
                            cumTP.HeSo);

                        nhatKy.AppendLine(
                            $"Đối chiếu trực tiếp: TP '{cumTP.NoiDungGoc}' " +
                            $"=> cần {cumTP.HeSo} đơn vị " +
                            $"'{cumTP.TenCauTruc}'.");

                        continue;
                    }

                    // Trường hợp BTP quy định hậu tố cho TP.
                    // Ví dụ: TP 3x25 và BTP 25R5 => TP cần 1 BTP 25R5.
                    List<string> cacCauTrucBTPPhuHop = tongBTP.Keys
                        .Where(tenBTP =>
                            LaQuanHeHauToTheoBTP(
                                cumTP.TenCauTruc,
                                tenBTP))
                        .ToList();

                    if (cacCauTrucBTPPhuHop.Count != 1)
                    {
                        string lyDo = cacCauTrucBTPPhuHop.Count == 0
                            ? $"Không tìm thấy BTP tương ứng với cụm TP " +
                              $"'{cumTP.NoiDungGoc}'."
                            : $"Cụm TP '{cumTP.NoiDungGoc}' khớp với nhiều " +
                              $"cấu trúc BTP: " +
                              string.Join(", ", cacCauTrucBTPPhuHop) + ".";

                        return KiemTraSoLuongBinDuPhong(
                            thanhPham,
                            nguyenVatLieu,
                            congDoan,
                            lyDo,
                            nhatKy);
                    }

                    string tenBTPPhuHop = cacCauTrucBTPPhuHop[0];

                    // BTP có thể bổ sung hậu tố cho cấu trúc TP.
                    // Khi ánh xạ sang tên BTP, giữ nguyên hệ số của cụm TP.
                    CongDonSoLuong(
                        soLuongTPYeuCau,
                        tenBTPPhuHop,
                        cumTP.HeSo);

                    nhatKy.AppendLine(
                    $"Đối chiếu theo hậu tố BTP: TP " +
                    $"'{cumTP.NoiDungGoc}' => cần {cumTP.HeSo} đơn vị " +
                    $"'{tenBTPPhuHop}'.");
                }

                // Nếu BTP có cấu trúc không được cụm TP nào sử dụng thì đây là
                // quan hệ khác nội dung, chuyển sang hàm dự phòng.
                List<string> cauTrucBTPKhongDuocDoiChieu = tongBTP.Keys
                    .Where(tenBTP => !soLuongTPYeuCau.ContainsKey(tenBTP))
                    .ToList();

                if (cauTrucBTPKhongDuocDoiChieu.Count > 0)
                {
                    return KiemTraSoLuongBinDuPhong(
                        thanhPham,
                        nguyenVatLieu,
                        congDoan,
                        "Có cấu trúc BTP không tương ứng với TP: " +
                        string.Join(", ", cauTrucBTPKhongDuocDoiChieu) + ".",
                        nhatKy);
                }

                bool coLoiSoLuong = false;

                foreach (string tenCauTruc in soLuongTPYeuCau.Keys
                    .Union(tongBTP.Keys, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(ten => ten, StringComparer.OrdinalIgnoreCase))
                {
                    long soLuongCan = LaySoLuong(
                        soLuongTPYeuCau,
                        tenCauTruc);

                    long soLuongCo = LaySoLuong(
                        tongBTP,
                        tenCauTruc);

                    if (soLuongCan == soLuongCo)
                    {
                        nhatKy.AppendLine(
                            $"'{tenCauTruc}': TP cần {soLuongCan}, " +
                            $"BTP có {soLuongCo} => Bằng.");
                        continue;
                    }

                    coLoiSoLuong = true;

                    string trangThai = soLuongCo < soLuongCan
                        ? $"Thiếu {soLuongCan - soLuongCo}"
                        : $"Dư {soLuongCo - soLuongCan}";

                    nhatKy.AppendLine(
                        $"'{tenCauTruc}': TP cần {soLuongCan}, " +
                        $"BTP có {soLuongCo} => {trangThai}.");
                }

                if (coLoiSoLuong)
                {
                    nhatKy.AppendLine(
                        $"Kết quả: {DanhSachLoiNhapLieuSX.Loi_SoLuongBin}");

                    GhiLogKiemTraSoLuongBin(nhatKy.ToString());
                    return DanhSachLoiNhapLieuSX.Loi_SoLuongBin;
                }

                nhatKy.AppendLine("Kết quả: Hợp lệ.");
                GhiLogKiemTraSoLuongBin(nhatKy.ToString());

                return null;
            }
            catch (ArgumentException ex)
            {
                return KiemTraSoLuongBinDuPhong(
                    thanhPham,
                    nguyenVatLieu,
                    congDoan,
                    "Không phân tích được tên TP/BTP: " + ex.Message,
                    nhatKy);
            }
            catch (OverflowException ex)
            {
                return KiemTraSoLuongBinDuPhong(
                    thanhPham,
                    nguyenVatLieu,
                    congDoan,
                    "Hệ số hoặc tổng số lượng vượt giới hạn: " + ex.Message,
                    nhatKy);
            }
        }

        /// <summary>
        /// Nhánh dự phòng khi logic đối chiếu cấu trúc chính không thể ánh xạ
        /// TP và BTP. Công đoạn 1 dùng quy tắc tính tiết diện; các công đoạn
        /// khác trả về lỗi không xác định.
        /// </summary>
        private static string KiemTraSoLuongBinDuPhong(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan,
            string lyDo,
            StringBuilder nhatKy)
        {
            nhatKy.AppendLine("Chuyển sang kiểm tra ngoại lệ.");
            nhatKy.AppendLine("Lý do: " + lyDo);
            nhatKy.AppendLine(
                $"Công đoạn: {congDoan?.Id.ToString() ?? "null"}");
            nhatKy.AppendLine(
                $"TP dự phòng: {thanhPham?.TenTP ?? "null"}");
            nhatKy.AppendLine(
                "BTP dự phòng: " +
                string.Join(
                    " | ",
                    nguyenVatLieu == null
                        ? Enumerable.Empty<string>()
                        : nguyenVatLieu.Select(
                            item => item?.TenNVL ?? "null")));

            // Nếu là công đoạn 0 - Kéo rút thì không cần kiểm tra số lượng bin
            if (congDoan.Id == 0)
            {
                nhatKy.AppendLine(
                $"Công đoạn Kéo rút không cần kiểm trả số lượng bin");
                return GhiLogVaTraKetQua(nhatKy, null);
            } 
                
            
            // Nếu công đoạn != bện rút trả về lỗi không xác định.
            if (congDoan == null || congDoan.Id != 1)
            {
                return GhiLogVaTraKetQua(
                    nhatKy,
                    DanhSachLoiNhapLieuSX.Loi_KhongXacDinh);
            }


            // Xử lý các trường hợp công đoạn là Bện rút (id = 1)
            try
            {
                if (thanhPham == null ||
                    string.IsNullOrEmpty(thanhPham.TenTP) ||
                    nguyenVatLieu == null ||
                    nguyenVatLieu.Count == 0 ||
                    nguyenVatLieu.Any(
                        item => item == null ||
                                string.IsNullOrEmpty(item.TenNVL)))
                {
                    nhatKy.AppendLine(
                        "Dữ liệu TP/BTP bị null hoặc tên bị rỗng.");

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                // So sánh toàn bộ TenNVL, không phân biệt hoa/thường và
                // không trim/chuẩn hóa chuỗi.
                string tenBTPDauTien = nguyenVatLieu[0].TenNVL;                

                double tietDienTP;
                string loiXuLyTP;

                if (!ThuTinhTongTietDienTheoTen(
                        thanhPham.TenTP,
                        out tietDienTP,
                        out loiXuLyTP,
                        nhatKy,
                        "TP"))
                {
                    nhatKy.AppendLine("Lỗi xử lý TP: " + loiXuLyTP);

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                double tietDienBTP;
                string loiXuLyBTP;

                if (!ThuTinhTongTietDienTheoTen(
                        tenBTPDauTien,
                        out tietDienBTP,
                        out loiXuLyBTP,
                        nhatKy,
                        "BTP"))
                {
                    nhatKy.AppendLine("Lỗi xử lý BTP: " + loiXuLyBTP);

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                if (!LaSoDuongHopLe(tietDienTP) ||
                    !LaSoDuongHopLe(tietDienBTP))
                {
                    nhatKy.AppendLine(
                        "Tổng tiết diện TP hoặc BTP không phải số dương hợp lệ.");

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                double tiLeSoBin = tietDienTP / tietDienBTP;

                if (!LaSoDuongHopLe(tiLeSoBin))
                {
                    nhatKy.AppendLine(
                        "Tỷ lệ tiết diện TP/BTP không hợp lệ.");

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                double soBinLamTronXuong = Math.Floor(tiLeSoBin);

                if (soBinLamTronXuong > long.MaxValue)
                {
                    nhatKy.AppendLine(
                        "Số Bin tính toán vượt giới hạn kiểu long.");

                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
                }

                long soBinTinhToan = Math.Max(
                    1L,
                    (long)soBinLamTronXuong);

                int soBinThucTe = nguyenVatLieu.Count;

                nhatKy.AppendLine(
                    $"Tổng tiết diện TP: {DinhDangSoChoLog(tietDienTP)}");
                nhatKy.AppendLine(
                    $"Tổng tiết diện BTP: {DinhDangSoChoLog(tietDienBTP)}");
                nhatKy.AppendLine(
                    $"Tỷ lệ TP/BTP: {DinhDangSoChoLog(tiLeSoBin)}");
                nhatKy.AppendLine(
                    $"Số Bin tính toán (làm tròn xuống, tối thiểu 1): " +
                    soBinTinhToan);
                nhatKy.AppendLine(
                    $"Số Bin thực tế: {soBinThucTe}");

                if (soBinThucTe < soBinTinhToan)
                {
                    return GhiLogVaTraKetQua(
                        nhatKy,
                        DanhSachLoiNhapLieuSX.Loi_SoLuongBin);
                }

                return GhiLogVaTraKetQua(nhatKy, null);
            }
            catch (ArgumentException ex)
            {
                nhatKy.AppendLine(
                    "Bất thường khi phân tích tên: " + ex.Message);

                return GhiLogVaTraKetQua(
                    nhatKy,
                    DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
            }
            catch (OverflowException ex)
            {
                nhatKy.AppendLine(
                    "Bất thường do tràn số: " + ex.Message);

                return GhiLogVaTraKetQua(
                    nhatKy,
                    DanhSachLoiNhapLieuSX.Loi_BatThuongKhiXuLyTen);
            }


        }



        /// <summary>
        /// Phân tích tên sản phẩm bằng PhanTachCauTrucDay và tính tổng tiết
        /// diện của tất cả cụm trong phần cấu trúc.
        /// </summary>
        private static bool ThuTinhTongTietDienTheoTen(
            string tenSanPham,
            out double tongTietDien,
            out string noiDungLoi,
            StringBuilder nhatKy,
            string nhanDoiTuong)
        {
            tongTietDien = 0d;
            noiDungLoi = null;

            if (string.IsNullOrEmpty(tenSanPham))
            {
                noiDungLoi = "Tên sản phẩm bị null hoặc rỗng.";
                return false;
            }

            KetQuaPhanTich ketQua = PhanTachCauTrucDay.PhanTich(
                ChuanHoaTenTruocKhiPhanTich(tenSanPham));

            if (ketQua.DanhSachCum == null ||
                ketQua.DanhSachCum.Count == 0)
            {
                noiDungLoi = "Tên không tạo được cụm cấu trúc nào.";
                return false;
            }

            foreach (CumCauTruc cum in ketQua.DanhSachCum)
            {
                CumQuyDoi cumQuyDoi;
                string loiQuyDoi;

                if (!ThuQuyDoiCum(
                        cum,
                        out cumQuyDoi,
                        out loiQuyDoi))
                {
                    noiDungLoi =
                        $"Không quy đổi được cụm '{cum.NoiDungGoc}': " +
                        loiQuyDoi;
                    return false;
                }

                double tietDienCum;
                string loiTietDien;

                if (!ThuTinhTietDienCum(
                        cumQuyDoi,
                        out tietDienCum,
                        out loiTietDien))
                {
                    noiDungLoi =
                        $"Không tính được cụm '{cum.NoiDungGoc}': " +
                        loiTietDien;
                    return false;
                }

                tongTietDien += tietDienCum;

                if (!LaSoDuongHopLe(tongTietDien))
                {
                    noiDungLoi =
                        "Tổng tiết diện không phải số dương hợp lệ.";
                    return false;
                }

                nhatKy.AppendLine(
                    $"{nhanDoiTuong} - Cụm '{cumQuyDoi.NoiDungGoc}': " +
                    $"hệ số {cumQuyDoi.HeSo}, " +
                    $"cấu trúc '{cumQuyDoi.TenCauTruc}', " +
                    $"tiết diện {DinhDangSoChoLog(tietDienCum)}");
            }

            return true;
        }

        /// <summary>
        /// Tính tiết diện một cụm đã quy đổi.
        /// - Có R/r: lấy phần số trước R và không dùng công thức hình tròn.
        /// - Không có R/r: cấu trúc phải là số thực thuần túy và dùng
        ///   PI * d * d / 4.
        /// - Có dấu / nhưng không có R/r: dữ liệu bất thường.
        /// </summary>
        private static bool ThuTinhTietDienCum(
            CumQuyDoi cum,
            out double tietDien,
            out string noiDungLoi)
        {
            tietDien = 0d;
            noiDungLoi = null;

            if (cum == null ||
                string.IsNullOrEmpty(cum.TenCauTruc) ||
                cum.HeSo <= 0)
            {
                noiDungLoi = "Cụm, tên cấu trúc hoặc hệ số không hợp lệ.";
                return false;
            }

            string tenCauTruc = cum.TenCauTruc;
            int viTriR = tenCauTruc.IndexOf(
                "R",
                StringComparison.OrdinalIgnoreCase);

            bool coR = viTriR >= 0;

            if (!coR && tenCauTruc.Contains("/"))
            {
                noiDungLoi = "Cấu trúc có dấu / nhưng không có R.";
                return false;
            }

            string chuoiGiaTri = coR
                ? tenCauTruc.Substring(0, viTriR)
                : tenCauTruc;

            double giaTriCauTruc;

            if (string.IsNullOrEmpty(chuoiGiaTri) ||
                !double.TryParse(
                    chuoiGiaTri,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out giaTriCauTruc) ||
                !LaSoDuongHopLe(giaTriCauTruc))
            {
                noiDungLoi =
                    $"Không chuyển được '{chuoiGiaTri}' thành số thực dương.";
                return false;
            }

            double tietDienCoSo = coR
                ? giaTriCauTruc
                : Math.PI * giaTriCauTruc * giaTriCauTruc / 4d;

            tietDien = cum.HeSo * tietDienCoSo;

            if (!LaSoDuongHopLe(tietDien))
            {
                noiDungLoi = "Tiết diện tính được không hợp lệ.";
                return false;
            }

            return true;
        }

        private static bool LaSoDuongHopLe(double giaTri)
        {
            return giaTri > 0d &&
                   !double.IsNaN(giaTri) &&
                   !double.IsInfinity(giaTri);
        }

        private static string DinhDangSoChoLog(double giaTri)
        {
            return giaTri.ToString(
                "0.######",
                CultureInfo.InvariantCulture);
        }

        private static string GhiLogVaTraKetQua(
            StringBuilder nhatKy,
            string ketQua)
        {
            nhatKy.AppendLine(
                string.IsNullOrEmpty(ketQua)
                    ? "Kết quả: Hợp lệ."
                    : "Kết quả: " + ketQua);

            GhiLogKiemTraSoLuongBin(nhatKy.ToString());
            return ketQua;
        }

        /// <summary>
        /// Quy đổi một cụm về tên cấu trúc cơ sở và tích các hệ số nguyên
        /// dương liên tiếp ở đầu, chỉ khi được nối bằng x hoặc X.
        ///
        /// Ví dụ:
        /// 4x2.5R2      => hệ số 4, cấu trúc 2.5R2
        /// 2x3x2.5R2   => hệ số 6, cấu trúc 2.5R2
        /// 2x10R5/T    => hệ số 2, cấu trúc 10R5/T
        /// 0.20Ax10    => hệ số 1, cấu trúc 0.20AX10
        /// </summary>
        private static bool ThuQuyDoiCum(
            CumCauTruc cum,
            out CumQuyDoi ketQua,
            out string noiDungLoi)
        {
            ketQua = null;
            noiDungLoi = null;

            if (cum.CacThanhPhan.Count == 0)
            {
                noiDungLoi = "Cụm không có thành phần.";
                return false;
            }

            if (cum.CacDauPhanCach.Count !=
                Math.Max(cum.CacThanhPhan.Count - 1, 0))
            {
                noiDungLoi = "Số dấu phân cách không khớp số thành phần.";
                return false;
            }

            long heSo = 1;
            int viTriCauTruc = 0;

            while (viTriCauTruc < cum.CacThanhPhan.Count - 1)
            {
                string dauPhanCach = ChuanHoaDauPhanCach(
                    cum.CacDauPhanCach[viTriCauTruc]);

                if (!string.Equals(
                    dauPhanCach,
                    "X",
                    StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                string thanhPhan = ChuanHoaThanhPhan(
                    cum.CacThanhPhan[viTriCauTruc]);

                long giaTriHeSo;

                if (!long.TryParse(
                    thanhPhan,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out giaTriHeSo) || giaTriHeSo <= 0)
                {
                    break;
                }

                heSo = checked(heSo * giaTriHeSo);
                viTriCauTruc++;
            }

            var tenCauTruc = new StringBuilder();

            for (int i = viTriCauTruc; i < cum.CacThanhPhan.Count; i++)
            {
                if (i > viTriCauTruc)
                {
                    tenCauTruc.Append(
                        ChuanHoaDauPhanCach(
                            cum.CacDauPhanCach[i - 1]));
                }

                tenCauTruc.Append(
                    ChuanHoaThanhPhan(cum.CacThanhPhan[i]));
            }

            string tenCauTrucDaChuanHoa = tenCauTruc.ToString();

            if (string.IsNullOrWhiteSpace(tenCauTrucDaChuanHoa))
            {
                noiDungLoi = "Tên cấu trúc sau quy đổi bị trống.";
                return false;
            }

            ketQua = new CumQuyDoi
            {
                NoiDungGoc = cum.NoiDungGoc,
                TenCauTruc = tenCauTrucDaChuanHoa,
                HeSo = heSo
            };

            return true;
        }

        /// <summary>
        /// Quan hệ đặc biệt do tên BTP quy định hậu tố cho cụm TP.
        /// Chỉ nhận hậu tố bắt đầu bằng R, ví dụ R2, R5, RC hoặc R5/T.
        /// </summary>
        private static bool LaQuanHeHauToTheoBTP(
            string tenCauTrucTP,
            string tenCauTrucBTP)
        {
            if (string.Equals(
                tenCauTrucTP,
                tenCauTrucBTP,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!tenCauTrucBTP.StartsWith(
                tenCauTrucTP,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string hauTo = tenCauTrucBTP.Substring(
                tenCauTrucTP.Length);

            return hauTo.StartsWith(
                "R",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string ChuanHoaTenTruocKhiPhanTich(string ten)
        {
            string ketQua = Regex.Replace(
                ten.Trim(),
                @"\s+",
                " ");

            int viTriKhoangTrangDauTien = ketQua.IndexOf(' ');

            if (viTriKhoangTrangDauTien < 0)
            {
                return ChuanHoaKhoangTrangQuanhDau(ketQua);
            }

            string phanDau = ketQua
                .Substring(0, viTriKhoangTrangDauTien);

            string phanSau = ketQua
                .Substring(viTriKhoangTrangDauTien + 1);

            // Ví dụ "2 x 2.5R2" hoặc "0.20A x 10" không có ThongTinDau.
            // Khi phần sau khoảng trắng đầu tiên bắt đầu bằng dấu phân cách,
            // chuẩn hóa toàn bộ chuỗi để class không hiểu nhầm phần đầu.
            if (phanSau.StartsWith("x", StringComparison.OrdinalIgnoreCase)
                || phanSau.StartsWith("+")
                || phanSau.StartsWith("/")
                || phanSau.StartsWith("-"))
            {
                return ChuanHoaKhoangTrangQuanhDau(ketQua);
            }

            // Có ThongTinDau, ví dụ "CX 3x25+10+6R5".
            // Chỉ chuẩn hóa phần cấu trúc phía sau để không làm mất khoảng trắng
            // giữa mã đầu "CX" và phần chính.
            return phanDau + " " + ChuanHoaKhoangTrangQuanhDau(phanSau);
        }

        private static string ChuanHoaKhoangTrangQuanhDau(string giaTri)
        {
            return Regex.Replace(
                giaTri,
                @"\s*([xX+/\-])\s*",
                "$1");
        }

        private static string ChuanHoaThanhPhan(string giaTri)
        {
            return Regex.Replace(
                    giaTri ?? string.Empty,
                    @"\s+",
                    string.Empty)
                .ToUpperInvariant();
        }

        private static string ChuanHoaDauPhanCach(string dau)
        {
            if (string.Equals(
                dau,
                "x",
                StringComparison.OrdinalIgnoreCase))
            {
                return "X";
            }

            return (dau ?? string.Empty).Trim();
        }

        private static void CongDonSoLuong(
            Dictionary<string, long> bangSoLuong,
            string tenCauTruc,
            long soLuong)
        {
            long hienTai;

            if (!bangSoLuong.TryGetValue(tenCauTruc, out hienTai))
            {
                hienTai = 0;
            }

            bangSoLuong[tenCauTruc] = checked(hienTai + soLuong);
        }

        private static long LaySoLuong(
            Dictionary<string, long> bangSoLuong,
            string tenCauTruc)
        {
            long soLuong;

            return bangSoLuong.TryGetValue(tenCauTruc, out soLuong)
                ? soLuong
                : 0;
        }

        private static void GhiLogKiemTraSoLuongBin(string noiDung)
        {
            Trace.WriteLine(noiDung);
        }

        private sealed class CumQuyDoi
        {
            public string NoiDungGoc { get; set; }

            public string TenCauTruc { get; set; }

            public long HeSo { get; set; }
        }
    }
}