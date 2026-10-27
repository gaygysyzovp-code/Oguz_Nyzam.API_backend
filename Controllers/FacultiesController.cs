using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;
using Oguz_Nyzam.API.Entities;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacultiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FacultiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<IEnumerable<FacultyDto>>> GetFaculties()
    {
        var faculties = await _context.Faculties
            .Select(f => new FacultyDto
            {
                Id = f.Id,
                Name = f.Name
            }).ToListAsync();
        
        return Ok(faculties);
    }

    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<FacultyDto>> CreateFaculty([FromBody] CreateFacultyDto dto)
    {
        if (await _context.Faculties.AnyAsync(f => f.Name == dto.Name))
            return BadRequest(new { message = "Bu facultet gosulan." });

        var faculty = new Faculty
        {
            Name = dto.Name
        };

        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();

        var result = new FacultyDto
        {
            Id = faculty.Id,
            Name = faculty.Name
        };

        return CreatedAtAction(nameof(GetFaculties), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateFaculty([FromBody] FacultyDto dto, [FromRoute] int id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty == null)
            return NotFound(new { message = "Faculty tapylmady." });

        faculty.Name = dto.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteFaculty([FromRoute] int id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty == null) 
            return NotFound(new { message = "Faculty tapylmady." });
        
        _context.Faculties.Remove(faculty);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Faculty ustunlikli pozuldy." });
    }
}