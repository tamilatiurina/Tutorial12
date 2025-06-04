using System.ComponentModel.DataAnnotations;

namespace Tutorial11.Models;

public class Person
{
    public int Id { get; set; }
    
    public string PassportNumber { get; set; }
    
    public string FirstName { get; set; }
    
    public string? MiddleName { get; set; }
    
    public string LastName { get; set; }
    
    public string PhoneNumber { get; set; }
    
    public string Email { get; set; }
}