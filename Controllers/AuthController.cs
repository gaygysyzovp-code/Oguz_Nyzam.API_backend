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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.FullName == dto.FullName))
            return BadRequest(new { message = "Bu FullName eyyam registrasiya edilen" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            PasswordHash = dto.Password,
            RoleId = null,
            FacultyId = dto.FacultyId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            RoleId = user.RoleId,
            FacultyId = user.FacultyId
        };

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.FullName == dto.FullName && u.PasswordHash == dto.Password);

        if (user == null)
            return BadRequest(new { message = "FullName ýa-da Password ýalňyş." });

        if (user.RoleId == null)
            return BadRequest(new { message = "Admin tarapyndan tassyklanmadyk." });
        
        var token = _jwtService.GenerateToken(user);

        var response = new AuthResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                RoleId = user.RoleId,
                FacultyId = user.FacultyId
            },
            Token = token
        };

        return Ok(response);
    }
}