using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Entities;
using Oguz_Nyzam.API.DTOs;
using Oguz_Nyzam.API.Data;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CoursesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1,2,3")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses([FromQuery] int? facultyId)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        int targetFacultyId;

        if (userRole == "1") 
        {
            if (!facultyId.HasValue)
                return BadRequest(new { message = "Admin üçin fakultet ID hökmany." });
            
            targetFacultyId = facultyId.Value;
        }
        else
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
            {
                return BadRequest(new { message = "Ulanyjynyň fakulteti tapylmady." });
            }
            targetFacultyId = userFacultyId;
        }

        var courses = await _context.Courses
            .Include(c => c.Faculty)
            .Where(c => c.FacultyId == targetFacultyId)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                CourseNumber = c.CourseNumber,
                FacultyName = c.Faculty != null ? c.Faculty.Name : string.Empty
            }).ToListAsync();

        return Ok(courses);
    }

    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var course = new Course
        {
            CourseNumber = dto.CourseNumber,
            FacultyId = dto.FacultyId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var result = new CourseDto
        {
            Id = course.Id,
            CourseNumber = course.CourseNumber,
            FacultyName = await _context.Faculties
                .Where(f => f.Id == course.FacultyId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync() ?? string.Empty
        };

        return StatusCode(201, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateCourse([FromBody] CreateCourseDto dto, [FromRoute] int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound(new { message = "Course tapylmady." });

        course.CourseNumber = dto.CourseNumber;
        course.FacultyId = dto.FacultyId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteCourse([FromRoute] int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound(new { message = "Course tapylmady." });

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Course ustunlikli pozuldy." });
    }
}