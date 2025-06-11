using System.ComponentModel.DataAnnotations;

namespace Tutorial11.DTOs;

public class CreateAccountDto
{
    [Required]
    [RegularExpression(@"^[^\d].*")]
    public string Username { get; set; }
    
    [Required]
    [MinLength(12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$")]
    public string Password { get; set; }
    
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public int RoleId { get; set; }
    
    
}