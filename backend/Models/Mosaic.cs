namespace backend.Models;

public class Mosaic
{
    public Guid MosaicId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public int[] Assignment { get; set; } = null!;
    
    public Guid TaskId { get; set; }
    public Task Task { get; set; } = null!;
}