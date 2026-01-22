using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;


namespace authModule.Models;


public class User : IdentityUser<Guid>
{

    // Basic fields are inherited from IdentityUser<Guid>
    //[Key]
    //public Guid Id { get; set; }

    //[Required]
    //[MaxLength(100)]
    //public string Name { get; set; } = string.Empty;

    //[Required]
    //[EmailAddress]
    //public string Email { get; set; } = string.Empty;

    //[Required]
    //[MaxLength(100)]
    //public string Salt { get; set; } = string.Empty;

    //[Required]
    //public string Hashed { get; set; } = string.Empty;

    //public int LoginAttempts { get; set; } = 0;
    //public string ResetPasswordTokens { get; set; } = string.Empty;

    //public DateTime ResetTokenExpiresAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    [Required]
    public AuthProvider Provider { get; set; } = AuthProvider.Local;

    // public ICollection<AuthClient> AuthClients { get; set; } = new List<Client>();
}

public enum UserRole
{
    User,
    Editor,
    Admin
}



// optional enum for authentication providers
// default is Local
public enum AuthProvider
{
    Local,
    Google,
    FaceBook,

}