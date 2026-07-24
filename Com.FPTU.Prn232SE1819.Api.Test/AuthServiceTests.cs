using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class AuthServiceTests
{
    private static IConfiguration BuildJwtConfig(bool includeKey = true)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "IS",
            ["Jwt:Audience"] = "C",
            ["Jwt:ExpireMinutes"] = "60",
        };
        if (includeKey)
            dict["Jwt:Key"] = "test-key-at-least-32-chars-long!!";
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static (
        AuthService Svc,
        Mock<IAuditService> Audit,
        Mock<IEmailSender> Email,
        Mock<IPermissionService> Permissions)
        CreateAuthService(IUnitOfWork uow, bool includeJwtKey = true)
    {
        var audit = new Mock<IAuditService>();
        var email = new Mock<IEmailSender>();
        var permissions = new Mock<IPermissionService>();
        permissions
            .Setup(p => p.GetPageKeysForRoleAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<string> { "/admin" });

        var svc = new AuthService(
            uow,
            BuildJwtConfig(includeJwtKey),
            email.Object,
            permissions.Object,
            audit.Object);
        return (svc, audit, email, permissions);
    }

    private static void VerifyLoginFailed(Mock<IAuditService> audit)
        => audit.Verify(a => a.LogAsync(
            "LOGIN_FAILED",
            "Auth",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>()), Times.Once);

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_success_returns_token_and_permissions()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, _, permissions) = CreateAuthService(uow);

            var result = await svc.LoginAsync(new LoginRequestDto
            {
                Email = "kh@test.com",
                Password = TestDb.DefaultPassword,
            });

            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.Equal(1, result.Id);
            Assert.Equal("kh@test.com", result.Email);
            Assert.Equal("Customer", result.Role);
            Assert.Contains("/admin", result.Permissions);
            permissions.Verify(p => p.GetPageKeysForRoleAsync(1), Times.Once);
            audit.Verify(a => a.LogAsync(
                "LOGIN",
                "Auth",
                "1",
                It.IsAny<string?>(),
                1), Times.Once);
        }
    }

    [Fact]
    public async Task Login_wrong_password_throws_Unauthorized_and_audits()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "kh@test.com",
                    Password = "WrongPass!",
                }));

            Assert.Equal("Invalid email or password.", ex.Message);
            VerifyLoginFailed(audit);
        }
    }

    [Fact]
    public async Task Login_unknown_email_throws_Unauthorized_and_audits()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "nobody@test.com",
                    Password = TestDb.DefaultPassword,
                }));

            Assert.Equal("Invalid email or password.", ex.Message);
            VerifyLoginFailed(audit);
        }
    }

    [Fact]
    public async Task Login_locked_user_throws_Unauthorized_and_audits()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var user = await db.Users.FirstAsync(u => u.Id == 1);
            user.IsLocked = true;
            await db.SaveChangesAsync();

            var (svc, audit, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "kh@test.com",
                    Password = TestDb.DefaultPassword,
                }));

            Assert.Equal("Account is locked.", ex.Message);
            VerifyLoginFailed(audit);
        }
    }

    [Fact]
    public async Task Login_inactive_user_throws_Unauthorized_and_audits()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var user = await db.Users.FirstAsync(u => u.Id == 1);
            user.IsActive = false;
            await db.SaveChangesAsync();

            var (svc, audit, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "kh@test.com",
                    Password = TestDb.DefaultPassword,
                }));

            Assert.Equal("Account is inactive.", ex.Message);
            VerifyLoginFailed(audit);
        }
    }

    [Fact]
    public async Task Login_soft_deleted_user_treated_as_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var user = await db.Users.FirstAsync(u => u.Id == 1);
            user.IsDeleted = true;
            await db.SaveChangesAsync();

            var (svc, audit, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "kh@test.com",
                    Password = TestDb.DefaultPassword,
                }));

            Assert.Equal("Invalid email or password.", ex.Message);
            VerifyLoginFailed(audit);
        }
    }

    [Fact]
    public async Task Login_missing_Jwt_Key_throws_InvalidOperationException()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow, includeJwtKey: false);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.LoginAsync(new LoginRequestDto
                {
                    Email = "kh@test.com",
                    Password = TestDb.DefaultPassword,
                }));

            Assert.Equal("Jwt:Key missing.", ex.Message);
        }
    }

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RequestRegister_name_too_short_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.RequestRegisterAsync(new RegisterRequestDto
                {
                    Name = "Ab",
                    Email = "new@test.com",
                    Password = "Pass@123",
                }));

            Assert.Contains("5", ex.Message);
        }
    }

    [Fact]
    public async Task RequestRegister_password_too_short_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.RequestRegisterAsync(new RegisterRequestDto
                {
                    Name = "Nguyen Van A",
                    Email = "new@test.com",
                    Password = "12345",
                }));

            Assert.Equal("Password must be at least 6 characters.", ex.Message);
        }
    }

    [Fact]
    public async Task RequestRegister_existing_email_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.RequestRegisterAsync(new RegisterRequestDto
                {
                    Name = "Nguyen Van A",
                    Email = "kh@test.com",
                    Password = "Pass@123",
                }));

            Assert.Equal("Email already exists.", ex.Message);
        }
    }

    [Fact]
    public async Task Register_success_stores_otp_and_verify_creates_user()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, email, _) = CreateAuthService(uow);

            var msg = await svc.RequestRegisterAsync(new RegisterRequestDto
            {
                Name = "Nguyen Van A",
                Email = "newbie@test.com",
                Password = "Pass@123",
                Phone = "0901111222",
            });

            Assert.Contains("OTP", msg.Message);
            email.Verify(e => e.SendAsync(
                "newbie@test.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()), Times.Once);

            var otpRow = await db.EmailOtps
                .Where(x => x.Email == "newbie@test.com" && x.Purpose == AuthService.PurposeRegister)
                .OrderByDescending(x => x.Id)
                .FirstAsync();
            Assert.False(string.IsNullOrWhiteSpace(otpRow.Otp));
            Assert.NotNull(otpRow.Payload);

            var registered = await svc.VerifyRegisterAsync(new VerifyOtpRequestDto
            {
                Email = "newbie@test.com",
                Otp = otpRow.Otp,
            });

            Assert.True(registered.Id > 0);
            Assert.Equal("Registered successfully.", registered.Message);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "newbie@test.com");
            Assert.NotNull(user);
            Assert.Equal("Nguyen Van A", user!.FullName);
            Assert.Equal(1, user.RoleId);
            Assert.True(user.IsActive);
            Assert.False(user.IsDeleted);

            audit.Verify(a => a.LogAsync(
                "REGISTER",
                "Auth",
                user.Id.ToString(),
                It.IsAny<string?>(),
                user.Id), Times.Once);
        }
    }

    [Fact]
    public async Task VerifyRegister_bad_otp_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            await svc.RequestRegisterAsync(new RegisterRequestDto
            {
                Name = "Nguyen Van A",
                Email = "otpbad@test.com",
                Password = "Pass@123",
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.VerifyRegisterAsync(new VerifyOtpRequestDto
                {
                    Email = "otpbad@test.com",
                    Otp = "000000",
                }));

            Assert.Contains("OTP", ex.Message);
        }
    }

    [Fact]
    public async Task VerifyRegister_empty_otp_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            await svc.RequestRegisterAsync(new RegisterRequestDto
            {
                Name = "Nguyen Van A",
                Email = "otpempty@test.com",
                Password = "Pass@123",
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.VerifyRegisterAsync(new VerifyOtpRequestDto
                {
                    Email = "otpempty@test.com",
                    Otp = "   ",
                }));

            Assert.Equal("OTP is required.", ex.Message);
        }
    }

    // ── Forgot / Reset password ────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_unknown_email_returns_same_message_without_otp()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, email, _) = CreateAuthService(uow);

            var result = await svc.ForgotPasswordAsync(new ForgotPasswordRequestDto
            {
                Email = "ghost@test.com",
            });

            Assert.Equal("Nếu email tồn tại, OTP đã được gửi.", result.Message);
            Assert.Empty(await db.EmailOtps.Where(x => x.Email == "ghost@test.com").ToListAsync());
            email.Verify(e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()), Times.Never);
        }
    }

    [Fact]
    public async Task ForgotPassword_known_email_creates_otp()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, email, _) = CreateAuthService(uow);

            var result = await svc.ForgotPasswordAsync(new ForgotPasswordRequestDto
            {
                Email = "kh@test.com",
            });

            Assert.Equal("Nếu email tồn tại, OTP đã được gửi.", result.Message);

            var otpRow = await db.EmailOtps
                .Where(x => x.Email == "kh@test.com" && x.Purpose == AuthService.PurposeReset)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(otpRow);
            Assert.False(string.IsNullOrWhiteSpace(otpRow!.Otp));

            email.Verify(e => e.SendAsync(
                "kh@test.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()), Times.Once);
            audit.Verify(a => a.LogAsync(
                "FORGOT_PASSWORD",
                "Auth",
                "1",
                It.IsAny<string?>(),
                1), Times.Once);
        }
    }

    [Fact]
    public async Task VerifyForgotOtp_then_ResetPassword_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit, _, _) = CreateAuthService(uow);

            await svc.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "kh@test.com" });
            var otpRow = await db.EmailOtps
                .Where(x => x.Email == "kh@test.com" && x.Purpose == AuthService.PurposeReset)
                .OrderByDescending(x => x.Id)
                .FirstAsync();

            var verify = await svc.VerifyForgotOtpAsync(new VerifyOtpRequestDto
            {
                Email = "kh@test.com",
                Otp = otpRow.Otp,
            });

            Assert.False(string.IsNullOrWhiteSpace(verify.ResetToken));

            var reset = await svc.ResetPasswordAsync(new ResetPasswordRequestDto
            {
                Email = "kh@test.com",
                ResetToken = verify.ResetToken!,
                NewPassword = "NewPass@1",
                ConfirmPassword = "NewPass@1",
            });

            Assert.Contains("Đổi mật khẩu thành công", reset.Message);
            audit.Verify(a => a.LogAsync(
                "RESET_PASSWORD",
                "Auth",
                "1",
                It.IsAny<string?>(),
                1), Times.Once);

            // Can login with new password
            var login = await svc.LoginAsync(new LoginRequestDto
            {
                Email = "kh@test.com",
                Password = "NewPass@1",
            });
            Assert.Equal(1, login.Id);
        }
    }

    [Fact]
    public async Task ResetPassword_too_short_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResetPasswordAsync(new ResetPasswordRequestDto
                {
                    Email = "kh@test.com",
                    ResetToken = "any",
                    NewPassword = "12345",
                    ConfirmPassword = "12345",
                }));

            Assert.Equal("Password must be at least 6 characters.", ex.Message);
        }
    }

    [Fact]
    public async Task ResetPassword_confirm_mismatch_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResetPasswordAsync(new ResetPasswordRequestDto
                {
                    Email = "kh@test.com",
                    ResetToken = "any",
                    NewPassword = "Pass@123",
                    ConfirmPassword = "Pass@999",
                }));

            Assert.Equal("Confirm password does not match.", ex.Message);
        }
    }

    [Fact]
    public async Task ResetPassword_bad_token_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _, _, _) = CreateAuthService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResetPasswordAsync(new ResetPasswordRequestDto
                {
                    Email = "kh@test.com",
                    ResetToken = "not-a-real-token",
                    NewPassword = "Pass@123",
                    ConfirmPassword = "Pass@123",
                }));

            Assert.Equal("Reset token invalid or expired.", ex.Message);
        }
    }
}
