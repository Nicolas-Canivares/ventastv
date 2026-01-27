using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;
using PhantomTvSales.Api.Services;
using System.Text.Json;

namespace PhantomTvSales.Api.Controllers;

[ApiController]
[Route("api/phantom")]
public class PhantomController : ControllerBase
{
    private readonly PhantomClient _phantom;
    private readonly AppDbContext _db;

    public PhantomController(PhantomClient phantom, AppDbContext db)
    {
        _phantom = phantom;
        _db = db;
    }

    [HttpPost("sync/abonados-olt")]
    public async Task<IActionResult> SyncAbonadosOlt(CancellationToken ct)
    {
        var root = await _phantom.GetAbonadosPorOltAllAsync(ct);

        if (root.ValueKind != JsonValueKind.Array)
            return BadRequest(new { error = "Respuesta inesperada de Phantom (no es array)." });

        int inserted = 0, updated = 0;

        foreach (var row in root.EnumerateArray())
        {
            // En el manual dice “array bidireccional” (suele venir como array de arrays) :contentReference[oaicite:4]{index=4}
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 6)
                continue;

            string phantomId = row[0].ToString();  // Id Cliente
            string lastName  = row[2].ToString();  // Apellido
            string firstName = row[3].ToString();  // Nombre
            string address   = row[4].ToString();  // Dirección
            string city      = row[5].ToString();  // Ciudad

            if (string.IsNullOrWhiteSpace(phantomId))
                continue;

            var existing = await _db.Clients.SingleOrDefaultAsync(x => x.PhantomId == phantomId, ct);

            if (existing is null)
            {
                _db.Clients.Add(new Client
                {
                    PhantomId = phantomId,
                    FirstName = firstName,
                    LastName = lastName,
                    Address = address,
                    City = city,
                    UpdatedAt = DateTime.UtcNow
                });
                inserted++;
            }
            else
            {
                existing.FirstName = firstName;
                existing.LastName = lastName;
                existing.Address = address;
                existing.City = city;
                existing.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { inserted, updated, total = inserted + updated });
    }
}
