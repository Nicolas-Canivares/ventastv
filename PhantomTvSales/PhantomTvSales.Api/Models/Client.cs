using System.ComponentModel.DataAnnotations;

namespace PhantomTvSales.Api.Models;

public class Client
{
    public int Id { get; set; }

    [Required]
    public string PhantomId { get; set; } = "";   // Id Phantom (Id Cliente / IDA)

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string Phone { get; set; } = "";       // se completa luego (consulta avanzada)
    public string Address { get; set; } = "";
    public string City { get; set; } = "";

    public ContactStatus? ContactStatus { get; set; }
    public string Notes { get; set; } = "";

    public int? LastContactedByUserId { get; set; }
    public User? LastContactedByUser { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
