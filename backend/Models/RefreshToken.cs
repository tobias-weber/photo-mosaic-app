using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class RefreshToken
{
    public Guid Id { get; init; }
    [MaxLength(128)] public string Token { get; init; } = null!;
    public Guid UserId { get; init; }
    [MaxLength(512)] public string UserAgent { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime Expires { get; init; }
    public bool IsRevoked { get; set; }

    public User User { get; init; } = null!;
}