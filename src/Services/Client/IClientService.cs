using authModule.Common;
using authModule.Services;

namespace authModule.src.Services.Client;

public interface IClientService
{
    Task<ServiceResponse<List<ClientReadDto>>> GetAllClientsAsync(int limit, int page);
    Task<ServiceResponse<ClientReadDto>> GetClientByIdAsync(Guid clientId);
    Task<ServiceResponse<ClientReadDto>> AddClient(ClientDto c);
    Task<ServiceResponse<ClientReadDto>> UpdateClient(Guid clientId, ClientDto c);
    Task<ServiceResponse<bool>> DeleteClient(Guid clientId);
}