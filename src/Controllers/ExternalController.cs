

using authModule.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace authModule.Controllers;

[Route("/[controller]")]
public class ExternalController : Controller
{
    private readonly ILogger<ExternalController> _logger;
    private readonly IExternalService _externalService;

    public ExternalController(IExternalService externalService, ILogger<ExternalController> logger)
    {
        _externalService = externalService;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok("External Controller is working!");
    }

    // Route: /login
    [HttpGet("/login")]
    public IActionResult Login([FromQuery] string client_id, [FromQuery] string callback)
    {
        if (string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(callback))
        {
            return BadRequest("Missing client_id or callback parameters.");
        }

        ViewData["ClientId"] = client_id;
        ViewData["Callback"] = callback;

        return View();
    }

    //Route admin
    [HttpGet("/admin/login")]
    public IActionResult AdminLogin()
    {
        return View();
    }
}