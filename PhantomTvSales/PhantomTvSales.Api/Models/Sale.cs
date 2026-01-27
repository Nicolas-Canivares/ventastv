using System.ComponentModel.DataAnnotations;

namespace PhantomTvSales.Api.Models;

public class Sale
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    // Guardamos referencia al PDF
    [Required]
    public string ReceiptPath { get; set; } = "";

    public DateTime SoldAt { get; set; } = DateTime.UtcNow;
}
