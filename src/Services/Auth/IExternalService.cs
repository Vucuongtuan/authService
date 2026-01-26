using authModule.Common;
using authModule.DTOs.Auth;
namespace authModule.Services.Auth;

public interface IExternalService
{
    Task<ServiceResponse<string>> HandleExternalLoginAsync(string email, string password, string clientId, string callback);
    Task<ServiceResponse<string>> HandleExternalOtpLoginAsync(string email, string otpCode, string clientId, string callback);
    Task<ServiceResponse<object>> ExchangeCodeForTokenAsync(ExchangeCodeDto request);
    Task<ServiceResponse<ClientReadDto>> GetExternalClientByIdAsync(string clientId);
}