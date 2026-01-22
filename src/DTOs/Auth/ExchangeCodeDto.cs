using System.ComponentModel.DataAnnotations;

namespace authModule.DTOs.Auth;

public class ExchangeCodeDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
