// Mục đích: Quét cây control để tìm mọi ISectionProvider<T>, rồi trả về
// từ điển { key = tên model (T.Name), value = object dữ liệu đã merge nếu cần }.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Core
{
    public static class ProviderAggregator
    {
        /// <summary>
        /// Tự-động phát hiện tất cả ISectionProvider&lt;T&gt; ở dưới root,
        /// gộp theo từng T và trả về { "T.Name" : object }.
        /// </summary>
        public static IDictionary<string, object> AggregateSectionsFrom(Control root)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // 1) Thu mọi provider: key = typeof(TModel), value = list provider (object)
            var providersByModel = DiscoverProviders(root);

            // 2) Với mỗi TModel, nếu >1 provider -> dùng SectionMerger.MergeParts<TModel>()
            foreach (var kv in providersByModel)
            {
                var modelType = kv.Key;                   // typeof(TModel)
                var providers = kv.Value;                 // List<object> (mỗi object implements ISectionProvider<TModel>)
                var interfaceType = typeof(ISectionProvider<>).MakeGenericType(modelType);

                object dataObject;

                if (providers.Count == 1)
                {
                    // Gọi GetSectionData() trực tiếp qua interface
                    var prov = providers[0];
                    var getMethod = interfaceType.GetMethod("GetSectionData");
                    dataObject = getMethod?.Invoke(prov, null);
                }
                else
                {
                    // Tạo List<ISectionProvider<TModel>> bằng reflection
                    var listType = typeof(List<>).MakeGenericType(interfaceType);
                    var typedList = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");

                    foreach (var prov in providers)
                        addMethod.Invoke(typedList, new object[] { prov });

                    // Gọi SectionMerger.MergeParts<TModel>(IEnumerable<ISectionProvider<TModel>>)
                    var mergeGeneric = typeof(SectionMerger)
                        .GetMethod("MergeParts", BindingFlags.Public | BindingFlags.Static);
                    var mergeForModel = mergeGeneric.MakeGenericMethod(modelType);

                    dataObject = mergeForModel.Invoke(null, new object[] { typedList });
                }

                // Key mặc định: tên lớp model (vd "CaiDatCDBoc", "CD_BenRuot"...)
                var key = modelType.Name;
                result[key] = dataObject;
            }

            return result;
        }

        // Tìm mọi control implement ISectionProvider<T> (mọi T)
        private static Dictionary<Type, List<object>> DiscoverProviders(Control root)
        {
            var map = new Dictionary<Type, List<object>>();

            foreach (var ctrl in EnumerateControls(root))
            {
                var ifaces = ctrl.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISectionProvider<>));

                foreach (var iFace in ifaces)
                {
                    var modelType = iFace.GetGenericArguments()[0]; // TModel
                    if (!map.TryGetValue(modelType, out var list))
                    {
                        list = new List<object>();
                        map[modelType] = list;
                    }
                    list.Add(ctrl);
                }
            }

            return map;
        }

        private static IEnumerable<Control> EnumerateControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                if (c.HasChildren)
                {
                    foreach (var cc in EnumerateControls(c))
                        yield return cc;
                }
            }
        }
    }
}
