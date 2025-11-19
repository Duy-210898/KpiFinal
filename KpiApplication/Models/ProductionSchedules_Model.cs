using DevExpress.Skins.XtraForm;
using System;
using System.Collections.Generic;

namespace KpiApplication.Models
{
    public class ProductionSchedules_Model
    {
        // ===== Overview =====
        public int SOID { get; set; }
        public string Sales_Order { get; set; }
        public string PO { get; set; }
        public string Art { get; set; }
        public string Model { get; set; }
        public string Mold { get; set; }
        public int? Overview_QTY { get; set; }


        // ===== List Schedule =====
        public int? Production_Week { get; set; }
        public string Production_Date { get; set; }
        public string Process_Type { get; set; }
        public string Line_Number { get; set; }
        public int? Planning_QTY { get; set; }
        public string Color_Name { get; set; }
        public string Finish_Date { get; set; }
        public int? Finish_Week { get; set; }
        public string Uppers_Source { get; set; }
        public string Stockfitting { get; set; }
        public string Sample_Shoes { get; set; }

        // ===== Detail =====
        public string PRODUCTION_ORDER { get; set; }
        public string MAIN_PRODUCTION_ORDER { get; set; }
        public string MATERIAL_NO { get; set; }
        public string ORDER_TYPE { get; set; }
        public string SIZE_NO { get; set; }
        public int? Detail_Size_QTY { get; set; }
        public string ORG { get; set; }
        public string Detail_ART { get; set; }


        public DateTime? Detail_Updated_At { get; set; }

        public DateTime? ListSchedule_Created_At { get; set; }
        public DateTime? ListSchedule_Updated_At { get; set; }

        public int? Overview_Version { get; set; }
        public DateTime? Overview_Created_At { get; set; }

    }
}
