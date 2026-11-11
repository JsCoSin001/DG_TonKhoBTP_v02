#nullable enable

using System;
using System.Data.SQLite;
using DG_TonKhoBTP_v02.Core;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    internal static class CongDoanThanhPham_DB
    {
        internal const int MaGhepLoi = 6;
        private const int MaBocVo = 5;
        internal const string DanhSachIdCoChiTietGhepLoiSql =
            "(SELECT TTThanhPham_ID FROM CD_GhepLoiQB WHERE TTThanhPham_ID IS NOT NULL)";

        internal static int XacDinhMaLuuChinhThuc(
            SQLiteConnection conn, SQLiteTransaction tx, int maCongDoan, int danhSachSpId, object? chiTietCongDoan)
        {
            if (maCongDoan != MaGhepLoi)
                return maCongDoan;

            using var cmd = new SQLiteCommand("SELECT Ma FROM DanhSachMaSP WHERE id = @id;", conn, tx);
            cmd.Parameters.AddWithValue("@id", danhSachSpId);
            string? maSanPham = cmd.ExecuteScalar() as string;
            if (maSanPham?.Trim().StartsWith("TP.", StringComparison.OrdinalIgnoreCase) != true)
                return maCongDoan;

            if (!(chiTietCongDoan is CD_GhepLoiQB))
                throw new InvalidOperationException("Thiếu chi tiết Ghép lõi để lưu thành phẩm.");

            return MaBocVo;
        }

        // Điều kiện dùng bí danh ttp cho TTThanhPham trong các truy vấn thao tác và sản xuất.
        internal static string TaoDieuKienCongDoanThucHien(int maCongDoan)
        {
            const string coChiTietGhepLoi = "(ttp.id IN " + DanhSachIdCoChiTietGhepLoiSql + ")";

            switch (maCongDoan)
            {
                case MaGhepLoi:
                    return $"(ttp.CongDoan = {MaGhepLoi} OR (ttp.CongDoan = {MaBocVo} AND {coChiTietGhepLoi}))";
                case MaBocVo:
                    return $"(ttp.CongDoan = {MaBocVo} AND NOT {coChiTietGhepLoi})";
                default:
                    return $"ttp.CongDoan = {maCongDoan}";
            }
        }
    }
}
