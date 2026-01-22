using System.ComponentModel.DataAnnotations;

namespace authModule.Services;

// DTO use when creating a new client
// validate fields with data annotations
public class ClientDto 
{
    [Required(ErrorMessage = "Tên client không được để trống")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Domain là bắt buộc")]
    [Url(ErrorMessage = "Domain phải là một URL hợp lệ")]
    public string Domain { get; set; } = string.Empty;

    [Required(ErrorMessage = "Redirect URIs là bắt buộc")]
    public string RedirectUris { get; set; } = string.Empty;

    public string ThumbnailUri { get; set; } = string.Empty;
}

// DTO use when reading client data
public class ClientReadDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string RedirectUris { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ThumbnailUri { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


