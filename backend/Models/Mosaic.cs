namespace backend.Models;

public class Mosaic
{
    public Guid MosaicId { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public int[] Assignment { get; init; } = null!;
    
    public Guid JobId { get; init; }
    public Job Job { get; init; } = null!;
}