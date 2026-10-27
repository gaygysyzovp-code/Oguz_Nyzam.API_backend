namespace Oguz_Nyzam.API.DTOs;

public class CreateViolationDto
{
    public string Name { get; set; } = string.Empty;
}

public class ViolationDto 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}