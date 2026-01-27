using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;
using PhantomTvSales.Api.Services;

namespace PhantomTvSales.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public UsersController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!HttpContext.IsAdmin()) return Unauthorized("Solo admin.");

        var users = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, Role = u.Role.ToString(), u.CreatedAt })
            .ToListAsync(ct);

        return Ok(users);
    }

    public sealed record CreateUserRequest(string Username, string Password, UserRole Role);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (!HttpContext.IsAdmin()) return Unauthorized("Solo admin.");

        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Usuario y contraseña son obligatorios.");

        var exists = await _db.Users.AnyAsync(u => u.Username == username, ct);
        if (exists) return BadRequest("Usuario ya existe.");

        var user = new User
        {
            Username = username,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new { user.Id, user.Username, role = user.Role.ToString() });
    }

    public sealed record UpdateUserRequest(string? Password, UserRole? Role);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (!HttpContext.IsAdmin()) return Unauthorized("Solo admin.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { user.Id, user.Username, role = user.Role.ToString() });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!HttpContext.IsAdmin()) return Unauthorized("Solo admin.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }
}
