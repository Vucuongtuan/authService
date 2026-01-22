using authModule.Services;
using authModule.src.Services.Client;
using Microsoft.AspNetCore.Mvc;

namespace authModule.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientSV; // Inject Interface

    public ClientController(IClientService sv)
    {
        _clientSV = sv;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int limit = 10, [FromQuery] int page = 1)
    {
        var result = await _clientSV.GetAllClientsAsync(limit, page);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Show(Guid id)
    {
        var result = await _clientSV.GetClientByIdAsync(id);
        if (!result.Success)
        {
            if (result.Message == "Client not found") return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientDto request)
    {
        if (!ModelState.IsValid)
        {

            return BadRequest(ModelState);
        }

        var result = await _clientSV.AddClient(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(Show), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _clientSV.UpdateClient(id, request);
        if (!result.Success)
        {
            if (result.Message == "Client not found") return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _clientSV.DeleteClient(id);
        if (!result.Success)
        {
            if (result.Message == "Client not found") return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }
}
