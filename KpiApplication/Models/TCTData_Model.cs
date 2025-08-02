using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KpiApplication.Models
{
    public class TCTData_Model
    {
        public int TCTID { get; set; }
        public string ModelName { get; set; }
        public string Type { get; set; }
        public string Process { get; set; }
        public double? TCTValue { get; set; }
        public string Notes { get; set; }

        public DateTime? LastUpdatedAt{ get; set; }
    }
    public class TCTData_Pivoted
    {
        public string ModelName { get; set; }
        public string Type { get; set; }

        public string OriginalModelName { get; set; }
        public string OriginalType { get; set; }

        public double? Cutting { get; set; }
        public double? Stitching { get; set; }
        public double? Assembly { get; set; }
        public double? StockFitting { get; set; }

        public double? TotalTCT
        {
            get
            {
                double sum = 0;
                int count = 0;

                if (Cutting.HasValue) { sum += Cutting.Value; count++; }
                if (Stitching.HasValue) { sum += Stitching.Value; count++; }
                if (Assembly.HasValue) { sum += Assembly.Value; count++; }
                if (StockFitting.HasValue) { sum += StockFitting.Value; count++; }

                return count > 0 ? (double?)sum : null;
            }
        }
        public DateTime? LastUpdatedAt { get; set; }
        public string Notes { get; set; }

        public TCTData_Pivoted Clone()
        {
            return new TCTData_Pivoted
            {
                ModelName = this.ModelName,
                Type = this.Type,
                OriginalModelName = this.OriginalModelName,
                OriginalType = this.OriginalType,
                Cutting = this.Cutting,
                Stitching = this.Stitching,
                Assembly = this.Assembly,
                StockFitting = this.StockFitting,
                LastUpdatedAt = this.LastUpdatedAt,
                Notes = this.Notes
            };
        }
    }
}
