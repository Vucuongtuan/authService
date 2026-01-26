using authModule.Common;
using authModule.DTOs.Auth;
using authModule.src.DTOs.Auth;
using authModule.Models;

namespace authModule.src.Services.Auth;

public interface IAuthService
{
    Task<ServiceResponse<User>> RegisterAsync(RegisterDto request);
    Task<ServiceResponse<object>> LoginAsync(LoginDto request);
    Task<ServiceResponse<object>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ServiceResponse<object>> SendOtpAsync(SendOtpRequestDto request);
    Task<ServiceResponse<object>> VerifyOtpLoginAsync(VerifyOtpLoginDto request);
    Task<ServiceResponse<object>> ForgotPasswordAsync(string email);
    Task<ServiceResponse<object>> ResetPasswordAsync(ResetPasswordDto request);
}
