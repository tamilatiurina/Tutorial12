namespace Tutorial11.DTOs;

public class EmployeeDetailDto2
{
    public PersonDto Person { get; set; }
    public decimal Salary { get; set; }
    
    public string Position { get; set; }
    public DateTime HireDate { get; set; }
    
}