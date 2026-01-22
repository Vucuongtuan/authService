using authModule.Common;
using authModule.DataContext;
using authModule.DTOs.Auth;
using authModule.Models;
using authModule.src.Services.Auth;
using authModule.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace authModule.src.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(UserManager<User> userManager, ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _userManager = userManager;
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<ServiceResponse<User>> RegisterAsync(RegisterDto request)
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null) return ServiceResponse<User>.Fail("User already exists.");

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User // Default role
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResponse<User>.Fail($"User creation failed: {errors}");
            }

            return ServiceResponse<User>.Ok(user);
        }

        public async Task<ServiceResponse<object>> LoginAsync(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return ServiceResponse<object>.Fail("Invalid credentials.");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid) return ServiceResponse<object>.Fail("Invalid credentials.");

            var (token, jti) = _jwtHelper.GenerateToken(user.Id.ToString(), user.Email ?? "Unknown", user.Role.ToString());

            // Generate Refresh Token Object using Helper
            var refreshToken = _jwtHelper.CreateRefreshToken(jti, user.Id);

            // Save to DB
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return ServiceResponse<object>.Ok(new
            {
                access_token = token,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        public async Task<ServiceResponse<object>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            //  Validate Access Token format
            var validatedToken = _jwtHelper.GetPrincipalFromToken(request.Token, validateLifetime: false);
            if (validatedToken == null) return ServiceResponse<object>.Fail("Invalid Token");

            var jti = validatedToken.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (jti == null) return ServiceResponse<object>.Fail("Invalid Token JTI");

            // Validate Refresh Token in DB
            var storedRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken);
            if (storedRefreshToken == null) return ServiceResponse<object>.Fail("Token does not exist");

            // Validation Checks
            if (storedRefreshToken.ExpiryDate < DateTime.UtcNow) return ServiceResponse<object>.Fail("This refresh token has expired");
            if (storedRefreshToken.Invalidated) return ServiceResponse<object>.Fail("This refresh token has been invalidated");
            if (storedRefreshToken.Used) return ServiceResponse<object>.Fail("This refresh token has been used");
            if (storedRefreshToken.JwtId != jti) return ServiceResponse<object>.Fail("This refresh token does not match this JWT");

            // Update Used status
            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            // Generate New Tokens
            var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId.ToString());
            if (user == null) return ServiceResponse<object>.Fail("User not found");

            var (newToken, newJti) = _jwtHelper.GenerateToken(user.Id.ToString(), user.Email!, user.Role.ToString());

            // Generate New Refresh Token Object
            var newRefreshToken = _jwtHelper.CreateRefreshToken(newJti, user.Id);

            // Save to DB
            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            return ServiceResponse<object>.Ok(new
            {
                access_token = newToken,
                refresh_token = newRefreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }
    }
}