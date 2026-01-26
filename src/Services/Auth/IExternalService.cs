using authModule.Common;
using authModule.DTOs.Auth;
namespace authModule.Services.Auth;

public interface IExternalService
{
    Task<ServiceResponse<ClientReadDto>> GetExternalClientByIdAsync(string clientId);
    Task<ServiceResponse<string>> HandleExternalLoginAsync(string email, string password, string clientId, string callback);
    Task<ServiceResponse<object>> ExchangeCodeForTokenAsync(ExchangeCodeDto request);
}