using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class KeHoach
    {
        public class ExplosionResult
        {
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public string ProductType { get; set; }
            public decimal TotalRequirement { get; set; }  // Tổng nhu cầu
            public int Level { get; set; }  // Cấp trong BOM (0=TP, 1=BTP cấp 1, ...)
        }

        public class BOMItem
        {
            public int ComponentId { get; set; }
            public decimal Ratio { get; set; }
            public string ComponentCode { get; set; }
            public string ComponentName { get; set; }
            public string ComponentType { get; set; }
        }
    }
}
