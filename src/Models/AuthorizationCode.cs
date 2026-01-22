using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace authModule.Models;


public class AuthorizationCode
{
    [Key]
    public Guid Id { get; set; }


    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [ForeignKey("ClientId")]
    public Client? Client { get; set; }

    [Required]
    public string RedirectUri { get; set; } = string.Empty;

    public DateTime Expiry { get; set; } // max 5 minutes
    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
