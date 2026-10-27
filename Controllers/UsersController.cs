using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oguz_Nyzam.API.Data;
using Oguz_Nyzam.API.DTOs;
using Oguz_Nyzam.API.Entities;
using Oguz_Nyzam.API.Services;

namespace Oguz_Nyzam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                FacultyId = u.FacultyId,
                RoleId = u.RoleId
            }).ToListAsync();

        return Ok(users);
    }

    [HttpGet("faculty/{facultyId:int}")]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByFaculty(int facultyId)
    {
        var users = await _context.Users
            .Where(u => u.FacultyId == facultyId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                FacultyId = u.FacultyId,
                RoleId = u.RoleId
            }).ToListAsync();

        return Ok(users);            
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("Ulanyjy tapylmady.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok("Ulanyjy ustunlikli pozuldy.");
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> AssignRole([FromRoute] Guid id, [FromBody] UpdateUserRole dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User tapylmady." });

        user.RoleId = dto.RoleId;

        await _context.SaveChangesAsync();

        return Ok(new { message = "User'a ustunlikli role brildi." });
    }
}