namespace Tutorial11.DTOs;

public class CreateEmployeeDto
{
    public PersonDto2 Person { get; set; } = null!;
    public decimal Salary { get; set; }
    public int PositionId { get; set; }
}