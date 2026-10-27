using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;
using Oguz_Nyzam.API.Entities;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViolationController : ControllerBase
{
    private readonly AppDbContext _context;
    public ViolationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<IEnumerable<ViolationDto>>> GetViolation()
    {
        var violations = await _context.ViolationCategories
            .Select(v => new ViolationDto
            {
                Id = v.Id,
                Name = v.Name
            }).ToListAsync();
        
        return Ok(violations);
    }

    [HttpPost]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<ViolationDto>> CreateViolation([FromBody] CreateViolationDto dto)
    {
        if (await _context.ViolationCategories.AnyAsync(v => v.Name == dto.Name))
            return BadRequest(new { message = "Bu Violation gosulan." });
        
        var violation = new ViolationCategory
        {
            Name = dto.Name
        };

        _context.ViolationCategories.Add(violation);
        await _context.SaveChangesAsync();

        var result = new ViolationDto
        {
            Id = violation.Id,
            Name = violation.Name
        };

        return StatusCode(201, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> UpdateViolation([FromRoute] int id, [FromBody] ViolationDto dto)
    {
        var violation = await _context.ViolationCategories.FindAsync(id);
        if (violation == null) 
            return NotFound(new { message = "Violation tapylmady." });
        
        violation.Name = dto.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteViolation([FromRoute] int id)
    {
        var violation = await _context.ViolationCategories.FindAsync(id);
        if (violation == null) 
            return NotFound(new { message = "Violation tapylmady." });
        
        _context.ViolationCategories.Remove(violation);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Violation ustunlikli delete edildi." });
    }
}