using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;
using Oguz_Nyzam.API.Entities;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _context;
    public GroupsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1,2,3")]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetGroups([FromQuery] int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Faculty)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        
        if (course == null) 
            return NotFound(new { message = "Course tapylmady." });
        
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (userRole != "1")
        {
            var facultyIdClaim = User.FindFirst("FacultyId")?.Value;
            if (string.IsNullOrEmpty(facultyIdClaim) || !int.TryParse(facultyIdClaim, out int userFacultyId))
                return BadRequest(new { message = "User'in fakulteti tapylmady." });
            
            if  (course.FacultyId != userFacultyId)
                return Forbid();
        }

        var groups = await _context.Groups
            .Where(g => g.CourseId == courseId)
            .Select(g => new GroupDto
            {
                Id = g.Id,
                GroupNumber = g.GroupNumber,
                CourseNumber = course.CourseNumber,
                FacultyName = course.Faculty != null ? course.Faculty.Name : string.Empty
            }).ToListAsync();
        
        return Ok(groups);
    }

    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (await _context.Groups.AnyAsync(g => g.GroupNumber == dto.GroupNumber))
            return BadRequest(new { message = "Bu Group doredilen." });
        
        var group = new Group
        {
            GroupNumber = dto.GroupNumber,
            CourseId = dto.CourseId
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var course = await _context.Courses
            .Include(c => c.Faculty)
            .FirstOrDefaultAsync(c => c.Id == group.CourseId);

        var result = new GroupDto
        {
            Id = group.Id,
            GroupNumber = group.GroupNumber,
            CourseNumber = course != null ? course.CourseNumber : string.Empty,
            FacultyName = (course != null && course.Faculty != null) ? course.Faculty.Name : string.Empty
        };

        return StatusCode(201, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateGroup([FromRoute] int id, [FromBody] CreateGroupDto dto)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null) return NotFound(new { message = "Group tapylmady." });

        group.GroupNumber = dto.GroupNumber;
        group.CourseId = dto.CourseId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteGroup([FromRoute] int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null) return NotFound(new { message = "Group tapylmady." });

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Group ustunlikli pozuldy." });
    }
}