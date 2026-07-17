using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        try { return Ok(await _auth.LoginAsync(dto)); }
        catch (UnauthorizedAccessException ex)
        {
            var code = ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;
            return StatusCode(code, new { message = ex.Message });
        }
    }

    /// <summary>Bước 1 đăng ký: gửi OTP, chưa tạo user.</summary>
    [HttpPost("register/request")]
    public async Task<ActionResult<MessageResponseDto>> RegisterRequest([FromBody] RegisterRequestDto dto)
    {
        try { return Ok(await _auth.RequestRegisterAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Bước 2: verify OTP → INSERT User.</summary>
    [HttpPost("register/verify")]
    public async Task<ActionResult<RegisterResponseDto>> RegisterVerify([FromBody] VerifyOtpRequestDto dto)
    {
        try { return Ok(await _auth.VerifyRegisterAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponseDto>> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        try { return Ok(await _auth.ForgotPasswordAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("forgot-password/verify")]
    public async Task<ActionResult<VerifyOtpResponseDto>> ForgotVerify([FromBody] VerifyOtpRequestDto dto)
    {
        try { return Ok(await _auth.VerifyForgotOtpAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<MessageResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        try { return Ok(await _auth.ResetPasswordAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
