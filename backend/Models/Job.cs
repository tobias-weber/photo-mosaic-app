using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace backend.Models;

public enum JobStatus
{
    Created,
    Submitted,
    Started,
    Finished,
    Aborted
}

public class Job
{
    public Guid JobId { get; init; }
    public Guid Token {get; init;} // required to access the /jobs endpoint (only used by processing)
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; set; }
    public JobStatus Status { get; set; }
    
    public int N { get; init; }
    [MaxLength(64)]
    public string Algorithm {get; init;} = null!;
    public int Subdivisions { get; init; }
    public Mosaic? Mosaic { get; set; }
    
    public Guid ProjectId { get; init; }
    public Project Project { get; init; } = null!;
    
    public Guid TargetImageId { get; init; }
    public ImageRef TargetImage { get; init; } = null!;
    
    public ICollection<ImageRef> Images { get; init; } = null!;

}