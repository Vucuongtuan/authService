using authModule.Common;
using authModule.DTOs.Auth;
using authModule.Models;

namespace authModule.src.Services.Auth;

public interface IAuthService
{
    Task<ServiceResponse<User>> RegisterAsync(RegisterDto request);
    Task<ServiceResponse<object>> LoginAsync(LoginDto request);
    Task<ServiceResponse<object>> RefreshTokenAsync(RefreshTokenRequestDto request);
}
