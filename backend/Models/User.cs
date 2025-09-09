using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Project> Projects { get; set; } = null!;
}