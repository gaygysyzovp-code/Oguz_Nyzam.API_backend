namespace Oguz_Nyzam.API.DTOs;

public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public int FacultyId { get; set; } 
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int FacultyId { get; set; }
    public int? RoleId { get; set; }
}

public class AuthResponseDto
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
}

public class UpdateUserRole
{
    public int RoleId { get; set; }
}