namespace backend.Models;

public enum TaskStatus
{
    Submitted,
    Started,
    Finished,
    Aborted
}

public class Task
{
    public Guid TaskId { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public TaskStatus Status { get; set; }
    public int Subdivisions { get; set; }
    public int M { get; set; }
    public int N { get; set; }
    
    public Mosaic? Mosaic { get; set; }
    
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public ICollection<ImageRef> Images { get; set; } = null!;

}