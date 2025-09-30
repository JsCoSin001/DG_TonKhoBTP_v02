// File: Core/DataGridViewUtils.cs
using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Core
{
    public static class DataGridViewUtils
    {
        /// <summary>
        /// [1] Xoá sạch dữ liệu cho mọi kiểu bind.
        /// Ưu tiên: BindingSource/List -> Clear tại nguồn; DataTable -> Rows.Clear();
        /// Nếu không rõ nguồn mà có Tag=Type -> tạo DataTable rỗng theo model & gán lại.
        /// Nếu unbound -> Rows.Clear().
        /// </summary>
        public static void ClearSmart(DataGridView dgv)
        {
            if (dgv == null) return;

            // 1) Data-bound?
            if (dgv.DataSource != null)
            {
                switch (dgv.DataSource)
                {
                    case BindingSource bs:
                        if (bs.DataSource is DataTable dtFromBs)
                        {
                            dtFromBs.Rows.Clear();
                            bs.ResetBindings(false);
                            return;
                        }
                        if (bs.List is IList list && !list.IsReadOnly)
                        {
                            list.Clear();
                            bs.ResetBindings(false);
                            return;
                        }
                        // Không rõ kiểu nguồn → nếu có Tag=Type thì bind bảng rỗng
                        if (dgv.Tag is Type tModel1)
                        {
                            BindEmptyTable(dgv, tModel1);
                            return;
                        }
                        // Cuối cùng: tháo nguồn
                        dgv.DataSource = null;
                        return;

                    case DataTable dt:
                        dt.Rows.Clear();
                        return;

                    case DataView dv:
                        dv.Table?.Rows.Clear();
                        return;

                    default:
                        // IList (List<T>, BindingList<T>, …)
                        if (dgv.DataSource is IList list2 && !list2.IsReadOnly)
                        {
                            list2.Clear();
                            return;
                        }
                        // Có Tag=Type → bind bảng rỗng
                        if (dgv.Tag is Type tModel2)
                        {
                            BindEmptyTable(dgv, tModel2);
                            return;
                        }
                        // Không đoán được → tháo nguồn
                        dgv.DataSource = null;
                        return;
                }
            }

            // 2) Unbound
            // Nếu có Tag=Type → đảm bảo cột đúng cấu trúc model bằng cách bind bảng rỗng
            if (dgv.Tag is Type tModel)
            {
                BindEmptyTable(dgv, tModel);
                return;
            }

            // 3) Unbound & không có Tag → chỉ cần xoá hàng
            dgv.Rows.Clear();
        }

        /// <summary>
        /// [2] Tạo DataTable rỗng theo public properties của model (string, số, bool, DateTime, …).
        /// </summary>
        public static DataTable CreateEmptyTableFromType(Type modelType)
        {
            var dt = new DataTable(modelType.Name);

            var props = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead)
                                 .ToArray();

            foreach (var p in props)
            {
                var u = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                // Giới hạn kiểu cột hợp lý cho DataTable
                var colType =
                    u == typeof(string) ? typeof(string) :
                    u == typeof(int) ? typeof(int) :
                    u == typeof(long) ? typeof(long) :
                    u == typeof(short) ? typeof(short) :
                    u == typeof(double) ? typeof(double) :
                    u == typeof(float) ? typeof(float) :
                    u == typeof(decimal) ? typeof(decimal) :
                    u == typeof(bool) ? typeof(bool) :
                    u == typeof(DateTime) ? typeof(DateTime) :
                    typeof(string); // fallback cho kiểu lạ

                dt.Columns.Add(p.Name, colType);
            }

            return dt;
        }

        /// <summary>
        /// [3] Gán DataTable rỗng (đúng cấu trúc) làm DataSource.
        /// </summary>
        public static void BindEmptyTable(DataGridView dgv, Type modelType)
        {
            var dt = CreateEmptyTableFromType(modelType);
            // Tùy bạn: bật/tắt AutoGenerateColumns, ở đây bật cho nhanh
            dgv.AutoGenerateColumns = true;
            dgv.DataSource = dt;
        }
    }
}
