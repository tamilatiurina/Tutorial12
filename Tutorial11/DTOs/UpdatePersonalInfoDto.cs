using System.ComponentModel.DataAnnotations;

namespace Tutorial11.DTOs;

public class UpdatePersonalInfoDto
{
    [Required]
    [RegularExpression(@"^[^\d].*")]
    public string Username { get; set; }
    
    [Required]
    [MinLength(12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$")]
    public string Password { get; set; }
    
    public string PassportNumber { get; set; }
    
    public string FirstName { get; set; }
    
    public string? MiddleName { get; set; }
    
    public string LastName { get; set; }
    
    public string PhoneNumber { get; set; }
    
    public string Email { get; set; }
}