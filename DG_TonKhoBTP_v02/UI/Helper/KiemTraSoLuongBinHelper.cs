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
            List<TTNVLRow> nguyenVatLieu, CongDoan congDoan)
        {
            var nhatKy = new StringBuilder();

            nhatKy.AppendLine("===== KIỂM TRA SỐ LƯỢNG BIN =====");
            nhatKy.AppendLine($"TP: {thanhPham.TenTP}");

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

                    // Ở dạng hậu tố do BTP quy định, mỗi cụm TP ứng với
                    // đúng 1 BTP. Hệ số trước x trong TP không được nhân.
                    CongDonSoLuong(
                        soLuongTPYeuCau,
                        tenBTPPhuHop,
                        1);

                    nhatKy.AppendLine(
                        $"Đối chiếu theo hậu tố BTP: TP " +
                        $"'{cumTP.NoiDungGoc}' => cần 1 đơn vị " +
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
        /// Nhánh dự phòng cho trường hợp TP và BTP không thể ánh xạ bằng các
        /// cấu trúc đã xác định. Hiện tại kết luận là tên TP và nguyên liệu
        /// không phù hợp. Có thể bổ sung quy tắc nghiệp vụ khác tại đây sau này.
        /// </summary>
        private static string KiemTraSoLuongBinDuPhong(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan,
            string lyDo,
            StringBuilder nhatKy)
        {
            nhatKy.AppendLine("Chuyển sang kiểm tra dự phòng.");
            nhatKy.AppendLine("Lý do: " + lyDo);
            nhatKy.AppendLine(
                $"TP dự phòng: {thanhPham.TenTP}");
            nhatKy.AppendLine(
                "BTP dự phòng: " +
                string.Join(
                    " | ",
                    nguyenVatLieu.Select(item => item.TenNVL)));
            nhatKy.AppendLine(
                $"Kết quả: {DanhSachLoiNhapLieuSX.Loi_TP_Nl_KhongKhop}");

            GhiLogKiemTraSoLuongBin(nhatKy.ToString());

            return DanhSachLoiNhapLieuSX.Loi_TP_Nl_KhongKhop;
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
