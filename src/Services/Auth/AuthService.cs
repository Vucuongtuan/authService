using authModule.Common;
using authModule.DataContext;
using authModule.DTOs.Auth;
using authModule.Models;
using authModule.src.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace authModule.src.Services.Auth
{
    public class AuthService : IAuthService
    {
        // Inject UserManager form  ASP.NET Core Identity
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(UserManager<User> userManager, IConfiguration configuration, ApplicationDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        public async Task<ServiceResponse<User>> RegisterAsync(RegisterDto request)
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null) return ServiceResponse<User>.Fail("User already exists.");

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
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

            var token = GenerateJwtToken(user.Id.ToString(), user.Email ?? "Unknown", "User");
            var refreshToken = await GenerateRefreshTokenAsync(token.Id, user.Id);

            return ServiceResponse<object>.Ok(new
            {
                access_token = token.Token,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        public async Task<ServiceResponse<object>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            // 1. Validate Access Token format
            var validatedToken = GetPrincipalFromToken(request.Token);
            if (validatedToken == null) return ServiceResponse<object>.Fail("Invalid Token");
            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            // 2. Validate Refresh Token in DB
            var storedRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken);
            if (storedRefreshToken == null) return ServiceResponse<object>.Fail("Token does not exist");

            // 3. Validation Checks
            if (storedRefreshToken.ExpiryDate < DateTime.UtcNow) return ServiceResponse<object>.Fail("This refresh token has expired");
            if (storedRefreshToken.Invalidated) return ServiceResponse<object>.Fail("This refresh token has been invalidated");
            if (storedRefreshToken.Used) return ServiceResponse<object>.Fail("This refresh token has been used");
            if (storedRefreshToken.JwtId != jti) return ServiceResponse<object>.Fail("This refresh token does not match this JWT");

            // 4. Update Used status
            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            // 5. Generate New Tokens
            var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId.ToString());
            if (user == null) return ServiceResponse<object>.Fail("User not found");

            var newToken = GenerateJwtToken(user.Id.ToString(), user.Email!, "User");
            var newRefreshToken = await GenerateRefreshTokenAsync(newToken.Id, user.Id);

            return ServiceResponse<object>.Ok(new
            {
                access_token = newToken.Token,
                refresh_token = newRefreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        // --- Helper Methods ---

        private async Task<RefreshToken> GenerateRefreshTokenAsync(string jwtId, Guid userId)
        {
            var refreshToken = new RefreshToken
            {
                JwtId = jwtId,
                Used = false,
                UserId = userId,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(1),
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        private (string Token, string Id) GenerateJwtToken(string id, string name, string role)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), jti);
        }

        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token");

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
