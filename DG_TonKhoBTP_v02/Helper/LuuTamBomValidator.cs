using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Kiểm tra quan hệ Thành phẩm - Nguyên vật liệu theo BOM dành riêng cho Lưu tạm.
    /// Không gọi KiemTraSoLuongBin và không thay đổi validator của Lưu chính thức.
    /// </summary>
    public static class LuuTamBomValidator
    {
        public static List<string> LayDanhSachLoi(
            TTThanhPham thanhPham,
            List<TTNVLRow> nguyenVatLieu,
            CongDoan congDoan)
        {
            var errors = new List<string>();

            // Công đoạn 9 không sử dụng NVL.
            if (congDoan?.Id == 9)
                return errors;

            // Giữ nguyên policy hiện tại: công đoạn 10 (Chiếu Xạ) bỏ qua BOM.
            if (!CongDoanPolicy.CanKiemTraBom(congDoan))
                return errors;

            List<BomComponentData> bom = (thanhPham?.BomComponents ?? new List<BomComponentData>())
                .Where(x => x != null)
                .ToList();

            // Quyết định Lưu tạm: không có BOM thì bỏ qua hoàn toàn, kể cả IsCorrect=false.
            if (bom.Count == 0)
                return errors;

            // Điều kiện "phải có NVL" được kiểm riêng ở LuuTamValidator.
            // Ở đây chỉ kiểm quan hệ BOM khi đã có dữ liệu để so sánh.
            if (nguyenVatLieu == null || nguyenVatLieu.Count == 0)
                return errors;

            // Công đoạn 0 giữ nguyên cách xác định BOM đặc biệt theo tên/kích thước.
            if (congDoan?.Id == 0)
            {
                LoaiBomCongDoan0 loaiBom =
                    KiemTraBomCongDoan0Helper.XacDinhLoaiBom(bom);

                if (loaiBom == LoaiBomCongDoan0.KhongXacDinh)
                {
                    errors.Add("Công đoạn 0: không xác định được loại BOM.");
                    return ChuanHoa(errors);
                }

                foreach (TTNVLRow nvl in nguyenVatLieu)
                {
                    if (nvl == null ||
                        !KiemTraBomCongDoan0Helper.TenNguyenVatLieuPhuHop(
                            loaiBom,
                            nvl.TenNVL))
                    {
                        errors.Add(
                            $"Công đoạn 0: NVL '{nvl?.TenNVL ?? string.Empty}' không phù hợp loại BOM {loaiBom}.");
                    }
                }

                return ChuanHoa(errors);
            }

            // Công đoạn 1 giữ nguyên ngoại lệ hiện tại: không kiểm component BOM
            // và không dùng IsCorrect để cảnh báo khi Lưu tạm.
            if (congDoan?.Id == 1)
                return errors;

            var componentIdsThucTe = new HashSet<int>(
                nguyenVatLieu
                    .Where(nvl => nvl?.DanhSachMaSP_ID != null)
                    .Select(nvl => nvl.DanhSachMaSP_ID.Value));

            List<BomComponentData> componentBiThieu = bom
                .Where(LaComponentBatBuoc)
                .GroupBy(x => x.ComponentId)
                .Select(g => g.First())
                .Where(component => !componentIdsThucTe.Contains(component.ComponentId))
                .ToList();

            foreach (BomComponentData component in componentBiThieu)
            {
                errors.Add(
                    $"Thiếu component BOM '{component.ComponentTen ?? string.Empty}' " +
                    $"(ComponentId = {component.ComponentId}).");
            }

            // Với các công đoạn so sánh BOM thông thường, IsCorrect=false nghĩa là
            // NVL/BTP đã được đánh giá là khác BOM. Chỉ ghi nhận để UI hỏi một lần.
            foreach (TTNVLRow nvl in nguyenVatLieu.Where(x => x != null && x.IsCorrect == false))
            {
                errors.Add(
                    $"NVL/BTP khác BOM: Bin='{nvl.BinNVL ?? string.Empty}', " +
                    $"MaNVL='{nvl.MaNVL ?? string.Empty}', " +
                    $"DanhSachMaSP_ID={FormatNullableId(nvl.DanhSachMaSP_ID)}.");
            }

            return ChuanHoa(errors);
        }

        /// <summary>
        /// Giữ nguyên định nghĩa hiện tại của dự án:
        /// - Component khác KieuSP = NVL luôn bắt buộc.
        /// - Component NVL chỉ bắt buộc khi Active trong DanhSachNVLBatBuoc.
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

        private static string FormatNullableId(int? value)
        {
            return value.HasValue ? value.Value.ToString() : "NULL";
        }

        private static List<string> ChuanHoa(IEnumerable<string> errors)
        {
            return (errors ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
