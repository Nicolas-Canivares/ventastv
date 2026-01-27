using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;

namespace PhantomTvSales.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 200);

        var q = _db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(c =>
                c.PhantomId.Contains(search) ||
                c.LastName.Contains(search) ||
                c.FirstName.Contains(search));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new {
                c.PhantomId, c.FirstName, c.LastName, c.Address, c.City, c.Phone, c.ContactStatus, c.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }
}
