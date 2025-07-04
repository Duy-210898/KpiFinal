using System;

namespace KpiApplication.Models
{
    public class EmployeeInfo_Model
    {
        public int UserID { get; set; }                  
        public string Username { get; set; }          
        public string EmployeeName { get; set; }        
        public string EnglishName { get; set; }        
        public string Department { get; set; }         
        public string EmployeeID { get; set; }           

        public override string ToString()
        {
            return $"{EmployeeName} ({Username}) - {Department}";
        }
    }
}
