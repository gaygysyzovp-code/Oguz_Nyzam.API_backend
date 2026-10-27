namespace Oguz_Nyzam.API.DTOs;

public class CreateCourseDto
{
    public string CourseNumber { get; set; } = string.Empty;
    public int FacultyId { get; set; }
}

public class CourseDto
{
    public int Id { get; set; }
    public string CourseNumber { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
}