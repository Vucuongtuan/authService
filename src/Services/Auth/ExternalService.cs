using authModule.Common;
using authModule.DataContext;
using authModule.DTOs.Auth;
using authModule.Models;
using authModule.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace authModule.Services.Auth;

public class ExternalService : IExternalService
{
    private readonly UserManager<User> _userManager;
    private readonly JwtHelper _jwtHelper;
    private readonly ApplicationDbContext _context;

    public ExternalService(UserManager<User> userManager, JwtHelper jwtHelper, ApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtHelper = jwtHelper;
        _context = context;
    }

    public async Task<ServiceResponse<string>> HandleExternalLoginAsync(string email, string password, string clientId, string callback)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return ServiceResponse<string>.Fail("Email and Password are required.");
        }

        // Validate user credentials
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return ServiceResponse<string>.Fail("Invalid email or password.");
        }

        return await GenerateAuthorizationCodeAsync(user, clientId, callback);
    }

    public async Task<ServiceResponse<string>> HandleExternalOtpLoginAsync(string email, string otpCode, string clientId, string callback)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otpCode))
        {
            return ServiceResponse<string>.Fail("Email and OTP code are required.");
        }

        // Validate user
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return ServiceResponse<string>.Fail("User not found.");
        }

        // Validate OTP
        var otpRecord = await _context.OtpCodes
            .Where(x => x.Email == email && x.Code == otpCode && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null) return ServiceResponse<string>.Fail("Invalid or expired OTP code.");

        if (otpRecord.IsExpired)
        {
            return ServiceResponse<string>.Fail("OTP code has expired. Please request a new one.");
        }

        // Mark OTP as used
        otpRecord.IsUsed = true;
        _context.OtpCodes.Update(otpRecord);
        
        return await GenerateAuthorizationCodeAsync(user, clientId, callback);
    }

    private async Task<ServiceResponse<string>> GenerateAuthorizationCodeAsync(User user, string clientId, string callback)
    {
        // Validate clientId
        if (!Guid.TryParse(clientId, out var clientGuid))
        {
            return ServiceResponse<string>.Fail("Invalid client ID.");
        }

        var client = await _context.Clients.FindAsync(clientGuid);
        if (client == null)
        {
            return ServiceResponse<string>.Fail("Client not found.");
        }

        // Generate temporary authorization code (token fake ,max 5 minutes)
        var authCode = new AuthorizationCode
        {
            Id = Guid.NewGuid(),
            Code = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), // Random code
            UserId = user.Id,
            ClientId = clientGuid,
            RedirectUri = callback,
            Expiry = DateTime.UtcNow.AddMinutes(5), // 5 phút
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Lưu authorization code vào database
        await _context.AuthorizationCodes.AddAsync(authCode);
        await _context.SaveChangesAsync();

        // Redirect về client với code
        var separator = callback.Contains("?") ? "&" : "?";
        var redirectUrl = $"{callback}{separator}code={authCode.Code}";

        return ServiceResponse<string>.Ok(redirectUrl);
    }

    /// <summary>
    /// Exchange authorization code để lấy access token thật
    /// Web client sẽ gọi API này với code nhận được từ redirect
    /// </summary>
    public async Task<ServiceResponse<object>> ExchangeCodeForTokenAsync(ExchangeCodeDto request)
    {
        // Tìm authorization code trong database
        var authCode = await _context.AuthorizationCodes
            .Include(ac => ac.User)
            .Include(ac => ac.Client)
            .FirstOrDefaultAsync(ac => ac.Code == request.Code);

        if (authCode == null)
        {
            return ServiceResponse<object>.Fail("Invalid authorization code.");
        }

        // Kiểm tra code đã hết hạn chưa (5 phút)
        if (authCode.Expiry < DateTime.UtcNow)
        {
            return ServiceResponse<object>.Fail("Authorization code has expired.");
        }

        // Kiểm tra code đã được sử dụng chưa (chỉ dùng 1 lần)
        if (authCode.IsUsed)
        {
            return ServiceResponse<object>.Fail("Authorization code has already been used.");
        }

        // Validate client credentials
        if (authCode.ClientId != request.ClientId)
        {
            return ServiceResponse<object>.Fail("Client ID mismatch.");
        }

        if (authCode.Client == null || authCode.Client.ClientSecret != request.ClientSecret)
        {
            return ServiceResponse<object>.Fail("Invalid client credentials.");
        }

        // Đánh dấu code đã được sử dụng
        authCode.IsUsed = true;
        _context.AuthorizationCodes.Update(authCode);
        await _context.SaveChangesAsync();

        // Generate access token thật (sống lâu hơn)
        var user = authCode.User!;
        var (accessToken, jti) = _jwtHelper.GenerateToken(
            user.Id.ToString(),
            user.UserName ?? user.Email!,
            user.Role.ToString(),
            isSecretToken: true // Token thật, sống 1 tháng
        );

        // Generate refresh token
        var refreshToken = _jwtHelper.CreateRefreshToken(jti, user.Id);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return ServiceResponse<object>.Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken.Token,
            token_type = "Bearer",
            expires_in = 2592000 // 30 days in seconds
        });
    }


    public async Task<ServiceResponse<ClientReadDto>> GetExternalClientByIdAsync(string clientId)
    {
        if (!Guid.TryParse(clientId, out var clientGuid))
        {
            return ServiceResponse<ClientReadDto>.Fail("Invalid client ID.");
        }

        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientGuid);

        if (client == null)
        {
            return ServiceResponse<ClientReadDto>.Fail("Client not found.");
        }

        return ServiceResponse<ClientReadDto>.Ok(new ClientReadDto
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            Domain = client.Domain,
            RedirectUris = client.RedirectUris,
            ClientSecret = client.ClientSecret,
            ThumbnailUri = client.ThumbnailUri,
            CreatedAt = client.CreatedAt
        });
    }
}
