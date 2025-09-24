namespace backend.DTOs;

public class EnqueueJobDto
{
    public required string Algorithm {get; set;}
    public int Subdivisions { get; set; }
    public int N {get; set; }
    public Guid Target { get; set; }
}