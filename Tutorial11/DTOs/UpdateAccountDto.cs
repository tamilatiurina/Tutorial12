using System.ComponentModel.DataAnnotations;

namespace Tutorial11.DTOs;

public class UpdateAccountDto
{
    [Required]
    [RegularExpression(@"^[^\d]")]
    public string Username { get; set; }
    
    [Required]
    [MinLength(12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$")]
    public string Password { get; set; }
}