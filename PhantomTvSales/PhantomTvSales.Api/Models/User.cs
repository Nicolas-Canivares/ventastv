using System.ComponentModel.DataAnnotations;

namespace PhantomTvSales.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Seller;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
