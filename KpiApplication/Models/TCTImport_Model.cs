using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KpiApplication.Models
{
    public class TCTRaw
    {
        public string ModelName { get; set; }
        public string Type { get; set; }
        public double? Cutting { get; set; }
        public double? Stitching { get; set; }
        public double? Assembly { get; set; }
        public double? Stockfitting { get; set; }
    }

    public class TCTImport_Model
    {
        public string ModelName { get; set; }
        public string Type { get; set; }
        public string Process { get; set; }
        public double? TCT { get; set; }
        public string Notes { get; set; }
    }
}
