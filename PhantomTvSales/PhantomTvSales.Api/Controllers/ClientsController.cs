using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;
using PhantomTvSales.Api.Models;

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
        [FromQuery] ContactStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 200);

        var q = _db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            q = q.Where(c =>
                (c.PhantomId ?? "").ToLower().Contains(search) ||
                (c.LastName ?? "").ToLower().Contains(search) ||
                (c.FirstName ?? "").ToLower().Contains(search) ||
                (c.Phone ?? "").ToLower().Contains(search) ||
                (c.Address ?? "").ToLower().Contains(search));
        }

        if (status.HasValue)
            q = q.Where(c => c.ContactStatus == status);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(c => c.PhantomId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.PhantomId,
                c.FirstName,
                c.LastName,
                c.Phone,
                c.Address,
                c.City,
                c.ContactStatus,
                c.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    public sealed record UpdateStatusRequest(ContactStatus status, string? notes);

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return NotFound();

        client.ContactStatus = req.status;
        if (req.notes is not null) client.Notes = req.notes;
        client.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:int}/sale")]
    [RequestSizeLimit(20_000_000)] // 20MB
    public async Task<IActionResult> CreateSale(
    int id,
    [FromForm] decimal amount,
    [FromForm] IFormFile receiptPdf,
    CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return NotFound();

        if (amount <= 0) return BadRequest("El monto debe ser mayor a 0.");

        if (receiptPdf is null || receiptPdf.Length == 0)
            return BadRequest("Falta el PDF.");

        // Validar PDF (content-type y extensión)
        var fileName = receiptPdf.FileName ?? "";
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("El comprobante debe ser PDF (.pdf).");

        if (receiptPdf.ContentType is not null &&
            !receiptPdf.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            // Algunos browsers mandan application/octet-stream, por eso no lo hago ultra estricto
        }

        // Guardar archivo
        var receiptsDir = Path.Combine(AppContext.BaseDirectory, "Storage", "Receipts");
        Directory.CreateDirectory(receiptsDir);

        var safeName = $"{client.PhantomId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        var fullPath = Path.Combine(receiptsDir, safeName);

        await using (var fs = System.IO.File.Create(fullPath))
        {
            await receiptPdf.CopyToAsync(fs, ct);
        }

        // Guardar venta
        var sale = new Models.Sale
        {
            ClientId = client.Id,
            Amount = amount,
            ReceiptPath = fullPath,
            SoldAt = DateTime.UtcNow
        };

        _db.Sales.Add(sale);

        // Opcional: marcar estado “SaleClosed”
        client.ContactStatus = ContactStatus.SaleClosed;
        client.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, saleId = sale.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id, CancellationToken ct)
    {
        var c = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();

        var sale = await _db.Sales.AsNoTracking()
            .Where(s => s.ClientId == id)
            .OrderByDescending(s => s.SoldAt)
            .Select(s => new
            {
                s.Id,
                s.Amount,
                s.SoldAt
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            c.Id,
            c.PhantomId,
            c.FirstName,
            c.LastName,
            c.Phone,
            c.Address,
            c.City,
            c.ContactStatus,
            c.Notes,
            c.UpdatedAt,
            Sale = sale
        });
    }

    [HttpGet("{id:int}/sale/receipt")]
    public async Task<IActionResult> DownloadReceipt(int id, CancellationToken ct)
    {
        var sale = await _db.Sales.AsNoTracking()
            .Where(s => s.ClientId == id)
            .OrderByDescending(s => s.SoldAt)
            .FirstOrDefaultAsync(ct);

        if (sale is null) return NotFound();
        if (!System.IO.File.Exists(sale.ReceiptPath)) return NotFound();

        var fileName = Path.GetFileName(sale.ReceiptPath);
        return PhysicalFile(sale.ReceiptPath, "application/pdf", fileName);
    }


}
