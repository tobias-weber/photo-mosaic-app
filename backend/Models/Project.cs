using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Project
{
    public Guid ProjectId { get; set; }
    
    [MaxLength(128)]
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
}