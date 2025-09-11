using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Project
{
    public Guid ProjectId { get; init; }
    
    [MaxLength(128)]
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; init; }
    
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    
}