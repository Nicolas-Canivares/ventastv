using System.ComponentModel.DataAnnotations;

namespace PhantomTvSales.Api.Models;

public class UserSession
{
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = "";

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
}
