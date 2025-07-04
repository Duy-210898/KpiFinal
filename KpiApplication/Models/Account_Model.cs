using DevExpress.XtraEditors.Filtering.Templates;
using System;

public class Account_Model
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public string Password { get; set; } 
    public string EmployeeName { get; set; }
    public string EnglishName { get; set; }
    public string Department { get; set; }
    public string EmployeeID { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
