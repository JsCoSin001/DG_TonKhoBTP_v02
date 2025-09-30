// File: Core/SectionMerger.cs
// [Luồng 3] Gộp nhiều "partial T" theo quy tắc mặc định.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DG_TonKhoBTP_v02.Core
{
    public static class SectionMerger
    {
        /// <summary>
        /// [Luồng 3.1] Gộp các phần T theo thứ tự providers.
        /// Quy tắc mặc định:
        /// - numeric: lấy giá trị đầu tiên != 0
        /// - bool?: lấy giá trị đầu tiên != null
        /// - bool: true nếu có ít nhất 1 true
        /// - string: lấy chuỗi đầu tiên không null/empty
        /// - các loại khác: lấy giá trị đầu tiên != default(TProp)
        /// </summary>
        public static T MergeParts<T>(IEnumerable<ISectionProvider<T>> providers) where T : new()
        {
            var result = new T();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.CanWrite).ToArray();

            foreach (var p in props)
            {
                object chosen = GetDefault(p.PropertyType);
                bool assigned = false;

                foreach (var prov in providers)
                {
                    var part = prov.GetSectionData();
                    if (part == null) continue;

                    var val = p.GetValue(part);

                    if (ShouldTake(p.PropertyType, val, chosen, out var final))
                    {
                        chosen = final;
                        assigned = true;
                        // có thể break sớm nếu bạn muốn “lấy đầu tiên hợp lệ”
                        // nhưng để linh hoạt, ta không break: nếu quy tắc thay đổi theo attribute thì dễ mở rộng
                    }
                }

                if (assigned) p.SetValue(result, chosen);
                else p.SetValue(result, chosen); // vẫn set default để nhất quán
            }

            return result;
        }

        // ===== Helpers =====

        private static bool ShouldTake(Type t, object candidate, object current, out object chosen)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            chosen = current;

            // null xử lý chung
            if (candidate == null) return false;

            // string
            if (t == typeof(string))
            {
                var s = candidate as string;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    chosen = s;
                    return true;
                }
                return false;
            }

            // bool?
            if (Nullable.GetUnderlyingType(candidate.GetType()) == typeof(bool))
            {
                // candidate là bool?
                chosen = candidate; // lấy cái đầu tiên != null
                return true;
            }

            // bool
            if (t == typeof(bool))
            {
                // nếu đã có true thì giữ true; nếu chưa có true mà candidate=true -> lấy
                if ((bool)candidate)
                {
                    chosen = true;
                    return true;
                }
                // nếu current chưa set gì (default false) và candidate=false thì không cần thay
                return false;
            }

            // numeric
            if (IsNumeric(t))
            {
                // quy ước: 0 là "không có giá trị", >0 hoặc <0 là "có giá trị"
                if (!IsZero(candidate))
                {
                    chosen = candidate;
                    return true;
                }
                return false;
            }

            // loại khác: chọn giá trị đầu tiên khác default
            var def = GetDefault(t);
            if (!Equals(candidate, def))
            {
                chosen = candidate;
                return true;
            }

            return false;
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(double) || t == typeof(float) || t == typeof(decimal);
        }

        private static bool IsZero(object v)
        {
            switch (v)
            {
                case int i: return i == 0;
                case long l: return l == 0L;
                case short s: return s == (short)0;
                case double d: return Math.Abs(d) < double.Epsilon;
                case float f: return Math.Abs(f) < float.Epsilon;
                case decimal m: return m == 0m;
                default: return false;
            }
        }

        private static object GetDefault(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}
