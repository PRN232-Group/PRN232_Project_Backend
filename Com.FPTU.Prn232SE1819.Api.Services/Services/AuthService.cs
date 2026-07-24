using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class AuthService : IAuthService
{
    public const string PurposeRegister = "Register";
    public const string PurposeReset = "ResetPassword";

    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly IEmailSender _email;
    private readonly IPermissionService _permissions;
    private readonly IAuditService _audit;

    public AuthService(
        IUnitOfWork uow,
        IConfiguration config,
        IEmailSender email,
        IPermissionService permissions,
        IAuditService audit)
    {
        _uow = uow;
        _config = config;
        _email = email;
        _permissions = permissions;
        _audit = audit;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await _uow.Repository<User>().Entities
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            await _audit.LogAsync(
                action: "LOGIN_FAILED",
                entity: "Auth",
                entityId: null,
                detail: $"Failed login for {email}.",
                actorUserId: null);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if (user.IsLocked)
        {
            await _audit.LogAsync(
                action: "LOGIN_FAILED",
                entity: "Auth",
                entityId: user.Id.ToString(),
                detail: $"Locked account login attempt: {email}.",
                actorUserId: user.Id);
            throw new UnauthorizedAccessException("Account is locked.");
        }
        if (!user.IsActive)
        {
            await _audit.LogAsync(
                action: "LOGIN_FAILED",
                entity: "Auth",
                entityId: user.Id.ToString(),
                detail: $"Inactive account login attempt: {email}.",
                actorUserId: user.Id);
            throw new UnauthorizedAccessException("Account is inactive.");
        }

        var pageKeys = await _permissions.GetPageKeysForRoleAsync(user.RoleId);

        await _audit.LogAsync(
            action: "LOGIN",
            entity: "Auth",
            entityId: user.Id.ToString(),
            detail: $"Login success: {email} ({user.Role.Name}).",
            actorUserId: user.Id);

        return new LoginResponseDto
        {
            AccessToken = CreateToken(user),
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Permissions = pageKeys.ToList(),
        };
    }

    public async Task<MessageResponseDto> RequestRegisterAsync(RegisterRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 5)
            throw new InvalidOperationException("Họ tên phải có ít nhất 5 ký tự.");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");
        if (await _uow.Repository<User>().Entities.AnyAsync(u => u.Email == email && !u.IsDeleted))
            throw new InvalidOperationException("Email already exists.");

        var otp = GenerateOtp();
        var payload = JsonSerializer.Serialize(new
        {
            name = dto.Name.Trim(),
            passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            phone = dto.Phone,
        });

        await InvalidateActiveOtps(email, PurposeRegister);
        await SaveOtp(email, PurposeRegister, otp, payload, minutes: 10);
        var mail = EmailTemplates.OtpRegister(otp, minutes: 10);
        await _email.SendAsync(
            email,
            "Interior Studio — Mã OTP đăng ký",
            mail.Html,
            mail.Plain);

        await _audit.LogAsync(
            action: "REGISTER_REQUEST",
            entity: "Auth",
            entityId: null,
            detail: $"Register OTP requested for {email}.",
            actorUserId: null);

        return new MessageResponseDto { Message = "OTP đã gửi tới email. Vui lòng xác minh để hoàn tất đăng ký." };
    }

    public async Task<RegisterResponseDto> VerifyRegisterAsync(VerifyOtpRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var row = await GetValidOtp(email, PurposeRegister, dto.Otp);
        var payload = JsonSerializer.Deserialize<RegisterPayload>(row.Payload!)
            ?? throw new InvalidOperationException("Invalid registration payload.");

        if (await _uow.Repository<User>().Entities.AnyAsync(u => u.Email == email && !u.IsDeleted))
            throw new InvalidOperationException("Email already exists.");

        var customerRole = await _uow.Repository<Role>().Entities
            .FirstOrDefaultAsync(r => r.Name == "Customer")
            ?? throw new InvalidOperationException("Customer role missing. Seed Roles first.");

        var user = new User
        {
            Email = email,
            FullName = payload.name,
            Phone = payload.phone,
            RoleId = customerRole.Id,
            PasswordHash = payload.passwordHash,
            IsActive = true,
            IsLocked = false,
            IsDeleted = false,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().InsertAsync(user, saveChanges: false);
            row.UsedAt = VnDateTime.Now;
            await _uow.Repository<EmailOtp>().UpdateAsync(row, saveChanges: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        await _audit.LogAsync(
            action: "REGISTER",
            entity: "Auth",
            entityId: user.Id.ToString(),
            detail: $"Registered customer {email}.",
            actorUserId: user.Id);

        return new RegisterResponseDto { Id = user.Id, Message = "Registered successfully." };
    }

    public async Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var user = await _uow.Repository<User>().Entities
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        // Không lộ email tồn tại — vẫn trả message giống nhau
        if (user == null)
            return new MessageResponseDto { Message = "Nếu email tồn tại, OTP đã được gửi." };

        var otp = GenerateOtp();
        await InvalidateActiveOtps(email, PurposeReset);
        await SaveOtp(email, PurposeReset, otp, payload: null, minutes: 10);
        var mail = EmailTemplates.OtpForgotPassword(otp, minutes: 10);
        await _email.SendAsync(
            email,
            "Interior Studio — OTP quên mật khẩu",
            mail.Html,
            mail.Plain);

        await _audit.LogAsync(
            action: "FORGOT_PASSWORD",
            entity: "Auth",
            entityId: user.Id.ToString(),
            detail: $"Forgot-password OTP sent to {email}.",
            actorUserId: user.Id);

        return new MessageResponseDto { Message = "Nếu email tồn tại, OTP đã được gửi." };
    }

    public async Task<VerifyOtpResponseDto> VerifyForgotOtpAsync(VerifyOtpRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        var row = await GetValidOtp(email, PurposeReset, dto.Otp);
        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        row.ResetToken = resetToken;
        row.UsedAt = VnDateTime.Now; // OTP used; resetToken still valid until ExpiresAt
        // Extend expiry for password change step
        row.ExpiresAt = VnDateTime.Now.AddMinutes(15);

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<EmailOtp>().UpdateAsync(row);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return new VerifyOtpResponseDto
        {
            Message = "OTP hợp lệ. Hãy đặt mật khẩu mới.",
            ResetToken = resetToken,
        };
    }

    public async Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new InvalidOperationException("Confirm password does not match.");

        var row = await _uow.Repository<EmailOtp>().Entities
            .Where(x => x.Email == email
                && x.Purpose == PurposeReset
                && x.ResetToken == dto.ResetToken
                && x.ExpiresAt > VnDateTime.Now)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Reset token invalid or expired.");

        var user = await _uow.Repository<User>().Entities
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted)
            ?? throw new InvalidOperationException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = VnDateTime.Now;
        row.ResetToken = null;
        row.ExpiresAt = VnDateTime.Now; // invalidate

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().UpdateAsync(user, saveChanges: false);
            await _uow.Repository<EmailOtp>().UpdateAsync(row, saveChanges: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        await _audit.LogAsync(
            action: "RESET_PASSWORD",
            entity: "Auth",
            entityId: user.Id.ToString(),
            detail: $"Password reset for {email}.",
            actorUserId: user.Id);

        return new MessageResponseDto { Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập." };
    }

    private async Task InvalidateActiveOtps(string email, string purpose)
    {
        var actives = await _uow.Repository<EmailOtp>().Entities
            .Where(x => x.Email == email && x.Purpose == purpose && x.UsedAt == null && x.ExpiresAt > VnDateTime.Now)
            .ToListAsync();
        if (actives.Count == 0) return;

        await _uow.BeginTransactionAsync();
        try
        {
            foreach (var a in actives)
            {
                a.UsedAt = VnDateTime.Now;
                await _uow.Repository<EmailOtp>().UpdateAsync(a, saveChanges: false);
            }
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task SaveOtp(string email, string purpose, string otp, string? payload, int minutes)
    {
        var row = new EmailOtp
        {
            Email = email,
            Purpose = purpose,
            Otp = otp,
            Payload = payload,
            ExpiresAt = VnDateTime.Now.AddMinutes(minutes),
            CreatedAt = VnDateTime.Now,
        };
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<EmailOtp>().InsertAsync(row);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<EmailOtp> GetValidOtp(string email, string purpose, string otp)
    {
        if (string.IsNullOrWhiteSpace(otp))
            throw new InvalidOperationException("OTP is required.");

        var row = await _uow.Repository<EmailOtp>().Entities
            .Where(x => x.Email == email
                && x.Purpose == purpose
                && x.Otp == otp.Trim()
                && x.UsedAt == null
                && x.ExpiresAt > VnDateTime.Now)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (row == null)
            throw new InvalidOperationException("OTP không hợp lệ hoặc đã hết hạn.");
        return row;
    }

    private static string NormalizeEmail(string email)
        => (email ?? "").Trim().ToLowerInvariant();

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private string CreateToken(User user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
        var issuer = _config["Jwt:Issuer"] ?? "InteriorStudio";
        var audience = _config["Jwt:Audience"] ?? "InteriorStudio.Client";
        var minutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 480;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("roleId", user.RoleId.ToString()),
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class RegisterPayload
    {
        public string name { get; set; } = null!;
        public string passwordHash { get; set; } = null!;
        public string? phone { get; set; }
    }
}
