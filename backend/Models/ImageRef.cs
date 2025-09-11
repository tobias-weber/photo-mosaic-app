using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ImageRef
{
    [Key]
    public int ImageId { get; init; }  // we use an integer key to minimize overhead
    
    public bool IsTarget { get; set; }
    [MaxLength(512)]
    public string FilePath { get; set; } = null!;
    [MaxLength(256)]
    public string Name { get; init; } = null!;
    
    public Guid? ProjectId { get; init; }
    public Project? Project { get; init; } = null!;
    
    public ICollection<Task> Tasks { get; init; } = null!;
}