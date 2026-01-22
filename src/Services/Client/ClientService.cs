using authModule.DataContext;
using authModule.Common;
using Microsoft.EntityFrameworkCore;

using ClientModel = authModule.Models.Client;
using authModule.Services;
using authModule.Services.Client;
using authModule.src.Services.Client;

namespace authModule.Services.Client;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<ClientService> _logger;
    private readonly IConfiguration _config;

    public ClientService(ApplicationDbContext context, ILogger<ClientService> logger, IConfiguration config)
    {
        _ctx = context;
        _logger = logger;
        _config = config;
    }

    // Mapping function
    private static ClientReadDto MapToDto(ClientModel client)
    {
        return new ClientReadDto
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            Domain = client.Domain,
            RedirectUris = client.RedirectUris,
            ClientSecret = client.ClientSecret,
            ThumbnailUri = client.ThumbnailUri,
            CreatedAt = client.CreatedAt
        };
    }

    private static void ApplyDtoToClient(ClientDto dto, ClientModel client)
    {
        client.Name = dto.Name;
        client.Description = dto.Description;
        client.Domain = dto.Domain;
        client.RedirectUris = dto.RedirectUris;
        // ClientSecret is not updated here
        client.ThumbnailUri = dto.ThumbnailUri;
    }

    public async Task<ServiceResponse<List<ClientReadDto>>> GetAllClientsAsync(int limit, int page)
    {
        try
        {
            var validPagination = PaginationUtils.GetValidPagination(page, limit, _config);
            page = validPagination.Page;
            limit = validPagination.Limit;

            var totalCount = await _ctx.Clients.CountAsync();

            var clients = await _ctx.Clients
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var clientDtos = clients.Select(MapToDto).ToList();

            var pagination = new Paginations
            {
                Page = page,
                Limit = limit,
                TotalCount = totalCount
            };

            return ServiceResponse<List<ClientReadDto>>.Ok(clientDtos, pagination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetAllClientsAsync");
            return ServiceResponse<List<ClientReadDto>>.Fail("Internal server error while retrieving clients.");
        }
    }

    public async Task<ServiceResponse<ClientReadDto>> GetClientByIdAsync(Guid id)
    {
        try
        {
            var client = await _ctx.Clients.FindAsync(id);
            if (client == null)
            {
                return ServiceResponse<ClientReadDto>.Fail("Client not found");
            }

            return ServiceResponse<ClientReadDto>.Ok(MapToDto(client));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetClientByIdAsync for Id: {Id}", id);
            return ServiceResponse<ClientReadDto>.Fail("Internal server error while retrieving client.");
        }
    }

    public async Task<ServiceResponse<ClientReadDto>> AddClient(ClientDto createDto)
    {
        try
        {
            // Generate secret
            var secret = Convert.ToHexString(Guid.NewGuid().ToByteArray()) + Convert.ToHexString(Guid.NewGuid().ToByteArray());

            var newClient = new ClientModel
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Domain = createDto.Domain,
                RedirectUris = createDto.RedirectUris,
                ClientSecret = secret.ToLower(),
                ThumbnailUri = createDto.ThumbnailUri,
                CreatedAt = DateTime.UtcNow
            };

            await _ctx.Clients.AddAsync(newClient);
            await _ctx.SaveChangesAsync();

            return ServiceResponse<ClientReadDto>.Ok(MapToDto(newClient), "Client created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddClient");
            return ServiceResponse<ClientReadDto>.Fail("Error creating client: " + ex.Message);
        }
    }

    public async Task<ServiceResponse<ClientReadDto>> UpdateClient(Guid id, ClientDto c)
    {
        try
        {
            var client = await _ctx.Clients.FirstOrDefaultAsync(cl => cl.Id == id);

            if (client == null)
            {
                _logger.LogWarning("Client with Id: {Id} not found for update", id);
                return ServiceResponse<ClientReadDto>.Fail("Client not found");
            }

            ApplyDtoToClient(c, client);

            await _ctx.SaveChangesAsync();

            return ServiceResponse<ClientReadDto>.Ok(MapToDto(client), "Client updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateClient for Id: {Id}", id);
            return ServiceResponse<ClientReadDto>.Fail("Internal server error while updating client.");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteClient(Guid id)
    {
        try
        {
            var client = await _ctx.Clients.FindAsync(id);
            if (client == null)
            {
                return ServiceResponse<bool>.Fail("Client not found");
            }
            _ctx.Clients.Remove(client);
            await _ctx.SaveChangesAsync();
            return ServiceResponse<bool>.Ok(true, "Client deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in DeleteClient for Id: {Id}", id);
            return ServiceResponse<bool>.Fail("Internal server error while deleting client.");
        }
    }
}