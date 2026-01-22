using System.ComponentModel.DataAnnotations;

namespace authModule.DTOs.Auth;

public class TokenRequestDto
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
