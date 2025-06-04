namespace Tutorial11.DTOs;

public class EmployeeDetailDto
{
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    
    public PersonDto Person { get; set; }
    
    public PositionDto Position { get; set; }
}