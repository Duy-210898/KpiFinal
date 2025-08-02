using KpiApplication.Common;
using System;

namespace KpiApplication.Models
{
    public class IEPPHDataForUser_Model
    {
        public string ArticleName { get; set; }
        public string ModelName { get; set; }
        public string PCSend { get; set; }
        public string PersonIncharge { get; set; }
        public string NoteForPC { get; set; }

        public bool OutsourcingAssemblingBool { get; set; }
        public bool OutsourcingStitchingBool { get; set; }
        public bool OutsourcingStockFittingBool { get; set; }

        public string OutsourcingAssembling => OutsourcingAssemblingBool ? "YES" : "";
        public string OutsourcingStitching => OutsourcingStitchingBool ? "YES" : "";
        public string OutsourcingStockFitting => OutsourcingStockFittingBool ? "YES" : "";

        public string DataStatus { get; set; }

        public string Process { get; set; }
        public int? TargetOutputPC { get; set; }
        public int? AdjustOperatorNo { get; set; }

        private double? _iePPHValue;
        public double? IEPPHValue
        {
            get => _iePPHValue;
            set
            {
                _iePPHValue = value;
                THTValue = value.HasValue ? (double?)Math.Round(3600 / value.Value, 0) : null;
            }
        }

        public double? THTValue { get; set; }

        public string TypeName { get; set; }
        public string IsSigned { get; set; }
    }
}
