namespace Oguz_Nyzam.API.DTOs;

public class CreateGroupDto
{
    public int GroupNumber { get; set; }
    public int CourseId { get; set; }
}

public class GroupDto
{
    public int Id { get; set; }
    public int GroupNumber { get; set; }
    public string CourseNumber { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
}