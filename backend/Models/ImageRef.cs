using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ImageRef
{
    [Key]
    public Guid
        ImageId
    {
        get;
        init;
    } // we use an integer key to minimize overhead. It should never be sent to the UI since it's sequential

    public bool IsTarget { get; set; }

    [MaxLength(512)] public string FilePath { get; set; } = null!;
    [MaxLength(256)] public string Name { get; init; } = null!;

    [MaxLength(256)] public string ContentType { get; set; } = null!;

    public Guid? ProjectId { get; init; }
    public Project? Project { get; init; } = null!;
}