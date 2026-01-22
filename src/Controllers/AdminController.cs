using authModule.DTOs.Auth;
using authModule.Models;
using authModule.Services;
using authModule.src.Services.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace authModule.Controllers
{
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IClientService _clientService;

        public AdminController(UserManager<User> userManager, SignInManager<User> signInManager, IClientService clientService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _clientService = clientService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (user.Role != UserRole.Admin)
            {
                ModelState.AddModelError(string.Empty, "Bạn không có quyền truy cập trang quản trị.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "AdminCookie");
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync("AdminCookie", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var result = await _clientService.GetAllClientsAsync(100, 1);
            return View(result.Data);
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpGet("clients/create")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpPost("clients/create")]
        public async Task<IActionResult> Create(ClientDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _clientService.AddClient(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpGet("clients/edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _clientService.GetClientByIdAsync(id);
            if (!result.Success) return NotFound();

            var clientDto = new ClientDto
            {
                Name = result.Data!.Name,
                Description = result.Data.Description,
                Domain = result.Data.Domain,
                RedirectUris = result.Data.RedirectUris,
                ThumbnailUri = result.Data.ThumbnailUri
            };

            ViewData["ClientId"] = id;
            return View(clientDto);
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpPost("clients/edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, ClientDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _clientService.UpdateClient(id, model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        [HttpPost("clients/delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _clientService.DeleteClient(id);
            return RedirectToAction("Index");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction("Login");
        }
    }
}
