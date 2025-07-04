namespace KpiApplication.Models
{
    public class WeeklyPlanData_Model
    {
        public string ArticleName { get; set; }
        public string ModelName { get; set; }
        public string Stitching { get; set; }
        public string Assembling { get; set; }
        public string StockFitting { get; set; }
        public int? Week { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string BPFC { get; set; }
    }
}
