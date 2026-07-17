using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);

    /// <summary>Gửi OTP — chưa tạo User.</summary>
    Task<MessageResponseDto> RequestRegisterAsync(RegisterRequestDto dto);

    /// <summary>Verify OTP rồi mới INSERT User.</summary>
    Task<RegisterResponseDto> VerifyRegisterAsync(VerifyOtpRequestDto dto);

    Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto);
    Task<VerifyOtpResponseDto> VerifyForgotOtpAsync(VerifyOtpRequestDto dto);
    Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto);
}
