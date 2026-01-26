using System.ComponentModel.DataAnnotations;

namespace authModule.src.DTOs.Auth;

/// <summary>
/// DTO để yêu cầu gửi OTP đến email
/// </summary>
public class SendOtpRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO để xác thực OTP và đăng nhập
/// </summary>
public class VerifyOtpLoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain only numbers")]
    public string OtpCode { get; set; } = string.Empty;
}
