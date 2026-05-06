// ============================================================
// DB_KeHoach.cs
// Bảng liên quan: DanhSachMaSP, BOMStructure
// Chức năng: Tính toán nguyên vật liệu cần thiết theo kế hoạch sản xuất
// ============================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DB_KeHoach
    {
        #region KẾ hoạch ===================

        public static Dictionary<string, decimal> LayNVLCanTheoKeHoach(Dictionary<string, decimal> demandPlan, bool kTinhTon)
        {
            using var connection = DB_Base.OpenConnection();

            // 1. Lấy danh sách mã sản phẩm từ demandPlan
            var demandKeys = string.Join(",", demandPlan.Keys.Select(key => $"'{key}'"));

            // 2. SQL để lấy dữ liệu từ bảng DanhSachMaSP và BOMStructure
            string sql = $@"
                SELECT sp.Ma, sp.DonVi, sp.KieuSP, sp.Ten, sp.id, bom.ParentProduct, bom.Component, bom.TyLe, bom.TyLeHoanDoi
                FROM DanhSachMaSP sp
                INNER JOIN BOMStructure bom ON bom.Component = sp.id
                WHERE sp.Ten IN ({demandKeys}) AND sp.KieuSP = 'TP'";

            var command = new SQLiteCommand(sql, connection);
            var reader = command.ExecuteReader();

            // 3. Tạo dictionary chứa thông tin về vật liệu (donvi, tyle, v.v.)
            var materials = new Dictionary<string, (string DonVi, string KieuSP, decimal TyLe, decimal TyLeHoanDoi)>();

            while (reader.Read())
            {
                string ma = reader["Ma"].ToString();
                string donVi = reader["DonVi"].ToString();
                string kieuSP = reader["KieuSP"].ToString();
                decimal tyLe = Convert.ToDecimal(reader["TyLe"]);
                decimal tyLeHoanDoi = Convert.ToDecimal(reader["TyLeHoanDoi"]);

                materials[ma] = (donVi, kieuSP, tyLe, tyLeHoanDoi);
            }

            // 4. Tính toán nguyên vật liệu (NVL) cần thiết dựa trên demandPlan
            var result = new Dictionary<string, decimal>();

            foreach (var demand in demandPlan)
            {
                string ten = demand.Key;
                decimal quantity = demand.Value;

                // Kiểm tra nếu vật liệu có trong dữ liệu
                if (materials.ContainsKey(ten))
                {
                    var material = materials[ten];

                    if (kTinhTon)
                    {
                        // Trường hợp 1: Tính toán nguyên vật liệu trực tiếp
                        // Tính toán theo TyLe và TyLeHoanDoi
                        decimal nvlRequired = quantity * material.TyLe;
                        decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                        result[ten] = nvlConverted;
                    }
                    else
                    {
                        // Trường hợp 2: Tính toán với BTP đã sản xuất
                        // Trừ đi lượng BTP đã sản xuất (dựa vào KhoiLuongSau hoặc ChieuDaiSau)
                        decimal btpProduced = GetProducedBTP(ten, connection); // Truyền kết nối đã mở
                        decimal btpRequired = quantity - btpProduced;
                        decimal nvlRequired = btpRequired * material.TyLe;
                        decimal nvlConverted = nvlRequired * material.TyLeHoanDoi;
                        result[ten] = nvlConverted;
                    }
                }
            }

            return result;
        }

        private static decimal GetProducedBTP(string maSP, SQLiteConnection connection)
        {
            // Sử dụng kết nối đã mở thay vì mở kết nối mới
            string sql = $@"
                SELECT 
                    CASE 
                        WHEN sp.DonVi = 'KG' THEN sp.KhoiLuongSau
                        WHEN sp.DonVi = 'M' THEN sp.ChieuDaiSau
                        ELSE 0 
                    END AS ProducedQuantity
                FROM DanhSachMaSP sp
                WHERE sp.Ma = '{maSP}'";

            var command = new SQLiteCommand(sql, connection);
            var result = command.ExecuteScalar();

            // Nếu có giá trị trả về, chuyển sang kiểu decimal, nếu không trả về 0
            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        // Hàm hiển thị kết quả (có thể thay thế bằng logic giao diện người dùng của bạn)
        private static void DisplayResults(Dictionary<string, decimal> result)
        {
            foreach (var res in result)
            {
                Console.WriteLine($"Vật liệu: {res.Key}, Cần thiết: {res.Value} kg");
            }
        }

        #endregion
    }
}
