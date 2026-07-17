namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class LoginRequestDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class RegisterRequestDto
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Phone { get; set; }
}

public class RegisterResponseDto
{
    public int Id { get; set; }
    public string Message { get; set; } = null!;
}

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = null!;
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
}

public class VerifyOtpResponseDto
{
    public string Message { get; set; } = null!;
    public string? ResetToken { get; set; }
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = null!;
    public string ResetToken { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class MessageResponseDto
{
    public string Message { get; set; } = null!;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class AppPageDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Section { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class PermissionMatrixDto
{
    public List<RoleDto> Roles { get; set; } = new();
    public List<AppPageDto> Pages { get; set; } = new();
    /// <summary>roleId → pageKeys được phép</summary>
    public Dictionary<int, List<string>> Grants { get; set; } = new();
}

public class SetRolePermissionsDto
{
    public List<string> PageKeys { get; set; } = new();
}

public class MyPermissionsDto
{
    public List<string> PageKeys { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsLocked { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class CreateUserDto
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string? Password { get; set; }
}

public class UpdateRoleDto
{
    public string Role { get; set; } = null!;
}

public class SetLockedDto
{
    public bool IsLocked { get; set; }
}
