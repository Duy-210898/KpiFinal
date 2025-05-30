using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KpiApplication.Models
{
    public class TCTData
    {
        public int TCTID { get; set; }
        public string ModelName { get; set; }
        public string Type { get; set; }
        public string Process { get; set; }
        public double? TCTValue { get; set; }
    }
}
