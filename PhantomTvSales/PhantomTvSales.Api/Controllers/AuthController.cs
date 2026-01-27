using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;

namespace PhantomTvSales.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public sealed record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var username = request.Username?.Trim();
        var password = request.Password ?? "";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Usuario y contraseña son obligatorios.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user is null)
            return Unauthorized("Credenciales inválidas.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Credenciales inválidas.");

        var token = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Token = token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Username, role = user.Role.ToString() }
        });
    }
}
