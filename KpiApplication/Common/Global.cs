using KpiApplication.Models;

namespace KpiApplication.Common
{
    public static class Global
    {
        public static string Username { get; set; }
        public static string EmployeeName { get; set; }
        public static EmployeeInfo_Model CurrentEmployee { get; set; }
    }
}
