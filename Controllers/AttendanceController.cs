using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Entities;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;
using System.Security.Claims;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendancesController : ControllerBase
{
    private readonly AppDbContext _context;
    public AttendancesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "1,2,3")]
    public async Task<ActionResult<AttendanceRecordDto>> CreateAttendance([FromBody] CreateAttendanceRecordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid currentUserId))
        {
            return Unauthorized(new { message = "Ulanyjy tokenden tapylmady." });
        }

        var group = await _context.Groups
            .Include(g => g.Course)
                .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(g => g.Id == dto.GroupId);

        if (group == null)
        {
            return NotFound(new { message = "Görkezilen topar tapylmady." });
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole != "1")
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;

            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
            {
                return BadRequest(new { message = "Ulanyjynyň fakulteti tokenden tapylmady." });
            }

            if (group.Course == null || group.Course.FacultyId != userFacultyId)
            {
                return Forbid(); 
            }
        }

        var isAlreadySubmitted = await _context.AttendanceRecords
            .AnyAsync(ar => ar.GroupId == dto.GroupId && ar.Date == dto.Date && ar.IsSubmitted);
        
        if (isAlreadySubmitted)
            return BadRequest(new { 
                message = "Su topar ucin bugunki senede gatnasyk eyyam tassyklanypdyr!"
             });

        var attendanceRecord = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            Date = dto.Date,
            GroupId = dto.GroupId,
            UserId = currentUserId,
            IsSubmitted = true,
            SubmittedAt = DateTime.UtcNow,
            Details = dto.Details.Select(d => new AttendanceDetail
            {
                Id = Guid.NewGuid(),
                StudentId = d.StudentId,
                ViolationCategoryId = d.ViolationCategoryId,
                Note = d.Note
            }).ToList()
        };

        _context.AttendanceRecords.Add(attendanceRecord);
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Gatnaşyk üstünlikli tassyklandy we baza ýazyldy!", 
            id = attendanceRecord.Id 
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "1")] 
    public async Task<IActionResult> UpdateAttendance(Guid id, [FromBody] CreateAttendanceRecordDto dto)
    {
        var existingRecord = await _context.AttendanceRecords
            .Include(ar => ar.Details)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (existingRecord == null)
        {
            return NotFound(new { message = "Görkezilen gatnaşyk rekordy tapylmady." });
        }

        if (existingRecord.GroupId != dto.GroupId || existingRecord.Date != dto.Date)
        {
            var isDuplicate = await _context.AttendanceRecords
                .AnyAsync(ar => ar.GroupId == dto.GroupId 
                            && ar.Date == dto.Date 
                            && ar.IsSubmitted 
                            && ar.Id != id);

            if (isDuplicate)
            {
                return BadRequest(new { 
                    message = "Şu topar we sene üçin eýýäm tassyklanan başga gatnaşyk bar!" 
                });
            }
        }

        existingRecord.Date = dto.Date;
        existingRecord.GroupId = dto.GroupId;

        _context.AttendanceDetails.RemoveRange(existingRecord.Details);

        existingRecord.Details = dto.Details.Select(d => new AttendanceDetail
        {
            Id = Guid.NewGuid(),
            AttendanceRecordId = existingRecord.Id,
            StudentId = d.StudentId,
            ViolationCategoryId = d.ViolationCategoryId,
            Note = d.Note
        }).ToList();

        await _context.SaveChangesAsync();

        return Ok(new { message = "Gatnaşyk rekordy üstünlikli täzelendi!" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "1")] 
    public async Task<IActionResult> DeleteAttendance(Guid id)
    {
        var attendanceRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (attendanceRecord == null)
        {
            return NotFound(new { message = "Görkezilen gatnaşyk rekordy tapylmady." });
        }

        _context.AttendanceRecords.Remove(attendanceRecord);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Gatnaşyk rekordy we onuň jikme-jiklikleri üstünlikli pozuldy!" });
    }

//-----------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "1,2")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetAttendancesByDate([FromQuery] DateOnly date)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        int? userFacultyId = null;

        if (userRole == "2") 
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int facultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }
            userFacultyId = facultyId;
        }

        var query = _context.AttendanceDetails
            .Include(d => d.Student)
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .Include(d => d.ViolationCategory)
            .Where(d => d.AttendanceRecord.Date == date && d.AttendanceRecord.IsSubmitted);

        if (userFacultyId.HasValue)
        {
            query = query.Where(d => d.AttendanceRecord.Group.Course.FacultyId == userFacultyId.Value);
        }

        var details = await query.ToListAsync();

        var (items, statisticsByCategory) = MapToAttendanceReport(details);

        var response = new AttendanceRecordResponseDto
        {
            TotalCount = items.Count,
            Items = items,
            StatisticsByCategory = statisticsByCategory
        };

        return Ok(response);
    }

    [HttpGet("range")]
    [Authorize(Roles = "1,2")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetAttendancesByDateRange(
        [FromQuery] DateOnly startDate, 
        [FromQuery] DateOnly endDate)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        int? userFacultyId = null;

        if (userRole == "2") 
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int facultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }
            userFacultyId = facultyId;
        }

        var query = _context.AttendanceDetails
            .Include(d => d.Student)
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .Include(d => d.ViolationCategory)
            .Where(d => d.AttendanceRecord.Date >= startDate 
                    && d.AttendanceRecord.Date <= endDate 
                    && d.AttendanceRecord.IsSubmitted);

        if (userFacultyId.HasValue)
        {
            query = query.Where(d => d.AttendanceRecord.Group.Course.FacultyId == userFacultyId.Value);
        }

        var details = await query.ToListAsync();

        var (items, statisticsByCategory) = MapToAttendanceReport(details);

        var response = new AttendanceRecordResponseDto
        {
            TotalCount = items.Count,
            Items = items,
            StatisticsByCategory = statisticsByCategory
        };

        return Ok(response);
    }

    [HttpGet("group/{groupId}/summary")]
    [Authorize(Roles = "1,2")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetGroupAttendanceSummary(
        int groupId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var group = await _context.Groups
            .Include(g => g.Course)
                .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return NotFound(new { message = "Görkezilen topar tapylmady." });
        }   

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "2")
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }

            if (group.Course.FacultyId != userFacultyId)
            {
                return Forbid(); 
            }
        }

        var details = await _context.AttendanceDetails
            .Include(d => d.Student)
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .Include(d => d.ViolationCategory)
            .Where(d => d.AttendanceRecord.GroupId == groupId
                && d.AttendanceRecord.Date >= startDate
                && d.AttendanceRecord.Date <= endDate
                && d.AttendanceRecord.IsSubmitted)
            .ToListAsync();

        var (items, statisticsByCategory) = MapToAttendanceReport(details);

        var response = new AttendanceRecordResponseDto
        {
            TotalCount = items.Count,
            Items = items,
            StatisticsByCategory = statisticsByCategory
        };

        return Ok(response);
    }

    [HttpGet("student/{studentId}/stats")]
    [Authorize(Roles = "1,2")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetStudentAttendanceStats(
        Guid studentId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var student = await _context.Students
            .Include(s => s.Group)
                .ThenInclude(g => g.Course)
                    .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            return NotFound(new { message = "Görkezilen taly p tapylmady." });
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "2")
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }

            if (student.Group.Course.FacultyId != userFacultyId)
            {
                return Forbid(); 
            }
        }

        var details = await _context.AttendanceDetails
            .Include(d => d.Student)
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .Include(d => d.ViolationCategory)
            .Where(d => d.StudentId == studentId
                && d.AttendanceRecord.Date >= startDate
                && d.AttendanceRecord.Date <= endDate
                && d.AttendanceRecord.IsSubmitted)
            .ToListAsync();

        var (items, statisticsByCategory) = MapToAttendanceReport(details);

        var response = new AttendanceRecordResponseDto
        {
            TotalCount = items.Count,
            Items = items,
            StatisticsByCategory = statisticsByCategory
        };

        return Ok(response);
    }

    [HttpGet("violations")]
    [Authorize(Roles = "1,2,3")]
    public async Task<ActionResult<AttendanceRecordResponseDto>> GetViolationsByDate(
        [FromQuery] DateOnly date, 
        [FromQuery] int? categoryId)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        int? userFacultyId = null;

        if (userRole == "2" || userRole == "3") 
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int facultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }
            userFacultyId = facultyId;
        }

        var query = _context.AttendanceDetails
            .Include(d => d.Student)
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .Include(d => d.ViolationCategory)
            .Where(d => d.AttendanceRecord.Date == date 
                    && d.AttendanceRecord.IsSubmitted 
                    && d.ViolationCategoryId.HasValue); 

        if (categoryId.HasValue)
        {
            query = query.Where(d => d.ViolationCategoryId == categoryId.Value);
        }

        if (userFacultyId.HasValue)
        {
            query = query.Where(d => d.AttendanceRecord.Group.Course.FacultyId == userFacultyId.Value);
        }

        var details = await query.ToListAsync();

        var (items, statisticsByCategory) = MapToAttendanceReport(details);

        var response = new AttendanceRecordResponseDto
        {
            TotalCount = items.Count,
            Items = items,
            StatisticsByCategory = statisticsByCategory
        };

        return Ok(response);
    }

//-----------------------------------------

    private (List<AttendanceReportDto> items, Dictionary<string, int> statisticsByCategory) MapToAttendanceReport(List<AttendanceDetail> details)
    {
        var items = details.Select(d => new AttendanceReportDto
        {
            Id = d.Id,
            StudentId = d.Student.StudentId,
            FullName = d.Student.FullName,
            GroupId = d.AttendanceRecord.GroupId,
            GroupNumber = d.AttendanceRecord.Group.GroupNumber,
            CourseId = d.AttendanceRecord.Group.CourseId,
            CourseNumber = d.AttendanceRecord.Group.Course.CourseNumber,
            FacultyId = d.AttendanceRecord.Group.Course.FacultyId,
            FacultyName = d.AttendanceRecord.Group.Course.Faculty.Name,
            ViolationCategoryId = d.ViolationCategoryId,
            ViolationCategoryName = d.ViolationCategory?.Name,
            Note = d.Note,
            Date = d.AttendanceRecord.Date
        }).ToList();

        var statisticsByCategory = items
            .Where(i => !string.IsNullOrEmpty(i.ViolationCategoryName))
            .GroupBy(i => i.ViolationCategoryName!)
            .ToDictionary(g => g.Key, g => g.Count());

        return (items, statisticsByCategory);
    }

//--------------------------------------------

    [HttpPut("details/{detailId}")]
    [Authorize(Roles = "1,2,3")]
    public async Task<IActionResult> UpdateAttendanceDetail(Guid detailId, [FromBody] UpdateAttendanceDetailDto dto)
    {
        var detail = await _context.AttendanceDetails
            .Include(d => d.AttendanceRecord)
                .ThenInclude(ar => ar.Group)
                    .ThenInclude(g => g.Course)
                        .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(d => d.Id == detailId);

        if (detail == null)
        {
            return NotFound(new { message = "Görkezilen gatnaşyk detaly tapylmady." });
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "2" || userRole == "3") 
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
            {
                return BadRequest(new { message = "Dekanyň fakultet maglumaty tokenden tapylmady." });
            }

            if (detail.AttendanceRecord.Group.Course.FacultyId != userFacultyId)
            {
                return Forbid();
            }
        }

        if (dto.ViolationCategoryId.HasValue)
        {
            var categoryExists = await _context.ViolationCategories
                .AnyAsync(vc => vc.Id == dto.ViolationCategoryId.Value);

            if (!categoryExists)
            {
                return BadRequest(new { message = "Görkezilen düzgün bozma kategoriýasy tapylmady." });
            }
        }

        detail.ViolationCategoryId = dto.ViolationCategoryId;
        detail.Note = dto.Note;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Gatnaşyk detaly üstünlikli täzelendi." });
    }
}