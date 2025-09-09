using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public class User : IdentityUser<Guid>
{

    public DateTime CreatedAt { get; set; }

    public ICollection<Project> Projects { get; set; } = null!;
}