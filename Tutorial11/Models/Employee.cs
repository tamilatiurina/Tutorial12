namespace Tutorial11.Models;

public class Employee
{
    public int Id { get; set; }
    
    public decimal Salary { get; set; }
    
    public DateTime HireDate { get; set; }
    
    public int PersonId { get; set; }
    public Person Person { get; set; }
    
    public int PositionId { get; set; }
    public Position Position { get; set; }
    
    public Account Account { get; set; } 
}