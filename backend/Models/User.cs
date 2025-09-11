using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; init; }

    public ICollection<Project> Projects { get; init; } = null!;
}