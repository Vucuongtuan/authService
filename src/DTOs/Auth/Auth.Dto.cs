
using System.ComponentModel.DataAnnotations;

namespace authModule.Services.Auth;



public class CreateAccountDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(4,ErrorMessage = "PassWord min length 4 ")]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
    public string ComfirmPassword { get; set; } = string.Empty;


}

public class ReadUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

}

public class LoginDto { 
     
}

public class LoginReadDto { 
}