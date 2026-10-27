namespace Oguz_Nyzam.API.DTOs;

public class CreateAttendanceDetailDto
{
    public Guid StudentId { get; set; }
    public int? ViolationCategoryId { get; set; }
    public string? Note { get; set; }
}

public class CreateAttendanceRecordDto
{
    public DateOnly Date { get; set; }
    public int GroupId { get; set; }
    public Guid UserId { get; set; }
    public List<CreateAttendanceDetailDto> Details { get; set; } = new();
}

public class AttendanceDetailDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int? ViolationCategoryId { get; set; }
    public string? ViolationCategoryName { get; set; }
    public string? Note { get; set; }
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int GroupId { get; set; }
    public int GroupNumber { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<AttendanceDetailDto> Details { get; set; } = new();
}

public class AttendanceReportDto
{
    public Guid Id { get; set; }
    public int StudentId { get; set; } // Backendin beryan ID dalde talybyn sahsy ID (example: 233823)
    public string FullName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int GroupNumber { get; set; }
    public int CourseId { get; set; }
    public string CourseNumber { get; set; } = string.Empty;
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;

    public int? ViolationCategoryId { get; set; }
    public string? ViolationCategoryName { get; set; }
    public string? Note { get; set; }
    public DateOnly Date { get; set; }
}

public class AttendanceRecordResponseDto
{
    public int TotalCount { get; set; }
    public List<AttendanceReportDto> Items { get; set; } = new();

    public Dictionary<string, int>? StatisticsByCategory { get; set; }
}

public class UpdateAttendanceDetailDto
{
    public int? ViolationCategoryId { get; set; }
    public string? Note { get; set; }
}