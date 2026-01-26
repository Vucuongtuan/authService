using authModule.DTOs.Auth;
using authModule.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace authModule.Controllers;

public class ExternalController : Controller
{
    private readonly ILogger<ExternalController> _logger;
    private readonly IExternalService _externalService;

    public ExternalController(IExternalService externalService, ILogger<ExternalController> logger)
    {
        _externalService = externalService;
        _logger = logger;
    }

    [HttpGet("external/status")]
    public IActionResult GetStatus()
    {
        return Ok("External Controller is working!");
    }

    /// <summary>
    ///  render View engine login page
    /// </summary>
    /// <param name="client_id"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    [HttpGet("/login")]
    public async Task<IActionResult> Login([FromQuery] string client_id, [FromQuery] string callback, [FromQuery] string locale = "en")
    {
        if (string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(callback))
        {
            return BadRequest("Missing client_id or callback parameters.");
        }
        var result = await _externalService.GetExternalClientByIdAsync(client_id);
        if (!result.Success || string.IsNullOrEmpty(callback) || callback != result.Data!.RedirectUris)
        {
            return BadRequest("Invalid client_id or callback URL.");
        }

        ViewData["logo"] = result.Data.ThumbnailUri;
        ViewData["name"] = result.Data.Name;
        ViewData["client_id"] = client_id;
        ViewData["callback"] = callback;
        ViewData["locale"] = locale;

        return View();
    }


    // api http
    [HttpPost("/login")]
    public async Task<IActionResult> Login([FromQuery] string client_id, [FromQuery] string callback, [FromForm] string email, [FromForm] string password)
    {
        ViewData["ClientId"] = client_id;
        ViewData["Callback"] = callback;

        var result = await _externalService.HandleExternalLoginAsync(email, password, client_id, callback);

        if (!result.Success)
        {
            ViewData["Error"] = result.Message;
            return View();
        }

        return Redirect(result.Data!);
    }



    [HttpPost("/connect/token")]
    public async Task<IActionResult> ExchangeToken([FromBody] ExchangeCodeDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var result = await _externalService.ExchangeCodeForTokenAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
