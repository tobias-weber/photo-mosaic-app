namespace backend.Models;

public class Mosaic
{
    public Guid MosaicId { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public int[] Assignment { get; init; } = null!;
    
    public Guid TaskId { get; init; }
    public Task Task { get; init; } = null!;
}