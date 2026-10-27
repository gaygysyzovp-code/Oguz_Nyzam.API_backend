using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Entities;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public StudentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1,2,3")]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents([FromQuery] int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Course)
                .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        
        if (group == null)
            return NotFound(new { message = "Gorkezilen Group tapylmady." });

        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (userRole != "1")
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
                {
                    return BadRequest(new { message = "User'in fakulteti tapylmady." });
                }
            
            if (group.Course == null || group.Course.FacultyId != userFacultyId)
            {
                return Forbid();
            }
        }

        var students = await _context.Students
            .Where(s => s.GroupId == groupId)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                FullName = s.FullName,
                StudentId = s.StudentId,
                GroupNumber = group.GroupNumber,
                CourseNumber = group.Course.CourseNumber,
                FacultyName = group.Course.Faculty.Name
            }).ToListAsync();
        
        return Ok(students);
    }

    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto dto)
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            StudentId = dto.StudentId,
            GroupId = dto.GroupId
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var group = await _context.Groups
            .Include(g => g.Course)
                .ThenInclude(c => c.Faculty)
            .FirstOrDefaultAsync(g => g.Id == student.GroupId);

        var result = new StudentDto
        {
            Id = student.Id,
            FullName = student.FullName,
            StudentId = student.StudentId,
            GroupNumber = group != null ? group.GroupNumber : 0,
            CourseNumber = (group != null && group.Course != null) ? group.Course.CourseNumber : string.Empty,
            FacultyName = (group != null && group.Course != null && group.Course.Faculty != null) ? group.Course.Faculty.Name : string.Empty
        };

        return StatusCode(201, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateStudent([FromRoute] Guid id, [FromBody] CreateStudentDto dto)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
            return NotFound(new { message = "Student tapylmady." });
        
        student.FullName = dto.FullName;
        student.StudentId = dto.StudentId;
        student.GroupId = dto.GroupId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteStudent([FromRoute] Guid id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
            return NotFound(new { message = "Student tapylmady." });
        
        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Student ustunlikli pozuldy." });
    }
}