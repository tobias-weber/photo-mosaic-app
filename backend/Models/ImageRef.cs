using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ImageRef
{
    [Key]
    public int ImageId { get; set; }  // we use an integer key to minimize overhead
    
    public bool IsTarget { get; set; }
    [MaxLength(512)]
    public string FilePath { get; set; } = null!;
    [MaxLength(256)]
    public string Name { get; set; } = null!;
    
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; } = null!;
    
    public ICollection<Task> Tasks { get; set; } = null!;
}