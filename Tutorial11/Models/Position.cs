using System.ComponentModel.DataAnnotations;

namespace Tutorial11.Models;

public class Position
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public int MinExpYears { get; set; }
}