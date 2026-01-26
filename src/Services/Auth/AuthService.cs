using authModule.Common;
using authModule.DataContext;
using authModule.DTOs.Auth;
using authModule.src.DTOs.Auth;
using authModule.Models;
using authModule.src.Helpers;
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
        private readonly Mail _mailHelper;
        private readonly OtpHelper _otpHelper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<User> userManager, ApplicationDbContext context, JwtHelper jwtHelper, Mail mailHelper, OtpHelper otpHelper, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _context = context;
            _jwtHelper = jwtHelper;
            _mailHelper = mailHelper;
            _otpHelper = otpHelper;
            _logger = logger;
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

        public async Task<ServiceResponse<object>> SendOtpAsync(SendOtpRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return ServiceResponse<object>.Fail("User not found. Please register first.");

            // Invalidate existing unused OTP codes
            var existingOtps = await _context.OtpCodes
                .Where(x => x.Email == request.Email && !x.IsUsed)
                .ToListAsync();
            _logger.LogInformation("Found {Count} existing OTPs for {Email}", existingOtps, request.Email);
            if (existingOtps.Any())
            {
                foreach (var otp in existingOtps)
                {
                    otp.IsUsed = true;
                }
                _context.OtpCodes.UpdateRange(existingOtps);
            }

            // Generate new OTP
            var otpCode = _otpHelper.GenerateOtpCode();
            var expiryTime = _otpHelper.GetOtpExpiryTime();
            var expiryMinutes = _otpHelper.GetExpirationMinutes();

            var newOtp = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Code = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiryTime,
                IsUsed = false
            };

            await _context.OtpCodes.AddAsync(newOtp);
            await _context.SaveChangesAsync();

            // Send email
            var emailBody = EmailTemplates.GetOtpEmailBody(otpCode, expiryMinutes);
            var (status, message) = _mailHelper.SendMail(request.Email, "Your Login Code", emailBody, true);

            if (!status)
            {
                _logger.LogError("Failed to send OTP email to {Email}: {Message}", request.Email, message);
                return ServiceResponse<object>.Fail("Failed to send email. Please try again later.");
            }

            _logger.LogInformation("OTP sent to {Email}", request.Email);
            return ServiceResponse<object>.Ok(new { message = "OTP code sent to your email" });
        }

        public async Task<ServiceResponse<object>> VerifyOtpLoginAsync(VerifyOtpLoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return ServiceResponse<object>.Fail("Invalid credentials.");

            var otpRecord = await _context.OtpCodes
                .Where(x => x.Email == request.Email && x.Code == request.OtpCode && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null) return ServiceResponse<object>.Fail("Invalid or expired OTP code.");

            if (otpRecord.IsExpired)
            {
                _logger.LogWarning("Expired OTP attempt for {Email}", request.Email);
                return ServiceResponse<object>.Fail("OTP code has expired. Please request a new one.");
            }

            // Mark OTP as used
            otpRecord.IsUsed = true;
            _context.OtpCodes.Update(otpRecord);
            await _context.SaveChangesAsync();

            // Generate JWT tokens (same as LoginAsync)
            var (token, jti) = _jwtHelper.GenerateToken(user.Id.ToString(), user.Email ?? "Unknown", user.Role.ToString());
            var refreshToken = _jwtHelper.CreateRefreshToken(jti, user.Id);

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP login successful for {Email}", request.Email);
            return ServiceResponse<object>.Ok(new
            {
                access_token = token,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        public async Task<ServiceResponse<object>> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return ServiceResponse<object>.Fail("If your email is registered, you will receive an OTP code.");

             var existingOtps = await _context.OtpCodes
                .Where(x => x.Email == email && !x.IsUsed)
                .ToListAsync();
            if (existingOtps.Any())
            {
                foreach (var otp in existingOtps) otp.IsUsed = true;
                _context.OtpCodes.UpdateRange(existingOtps);
            }

            var otpCode = _otpHelper.GenerateOtpCode();
            var expiryTime = _otpHelper.GetOtpExpiryTime();

            var newOtp = new OtpCode
            {
                Id = Guid.NewGuid(),
                Email = email,
                Code = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiryTime,
                IsUsed = false
            };

            await _context.OtpCodes.AddAsync(newOtp);
            await _context.SaveChangesAsync();

            // Send Email
            var emailBody = $"Your Password Reset Code is: <b>{otpCode}</b>. It expires in 5 minutes.";
            _mailHelper.SendMail(email, "Reset Password Request", emailBody, true);

            return ServiceResponse<object>.Ok("OTP sent.");
        }

        public async Task<ServiceResponse<object>> ResetPasswordAsync(ResetPasswordDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return ServiceResponse<object>.Fail("Invalid request.");

            var otpRecord = await _context.OtpCodes
                .Where(x => x.Email == request.Email && x.Code == request.OtpCode && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.IsExpired)
            {
                return ServiceResponse<object>.Fail("Invalid or expired OTP.");
            }

            // Mark OTP used
            otpRecord.IsUsed = true;
            _context.OtpCodes.Update(otpRecord);

            // Reset Password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                return ServiceResponse<object>.Fail("Failed to reset password. " + result.Errors.FirstOrDefault()?.Description);
            }

            await _context.SaveChangesAsync();
            return ServiceResponse<object>.Ok("Password reset successfully.");
        }

    }
}