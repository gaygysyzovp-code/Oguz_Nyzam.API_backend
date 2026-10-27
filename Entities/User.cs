namespace Oguz_Nyzam.API.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int? RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int FacultyId { get; set; }
    public Faculty Faculty { get; set; } = null!;
}