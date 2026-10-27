namespace Oguz_Nyzam.API.DTOs;

public class CreateStudentDto
{
    public string FullName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public int GroupId { get; set; }
}

public class StudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public int GroupNumber { get; set; }
    public string CourseNumber { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
}