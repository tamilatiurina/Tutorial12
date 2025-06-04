namespace Tutorial11.Models;

public class DeviceEmployee
{
    public int Id { get; set; }
    
    public DateTime IssueDate { get; set; }
    
    public DateTime? ReturnDate { get; set; }
    
    public int DeviceId { get; set; }
    public Device Device { get; set; }
    
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; }
}