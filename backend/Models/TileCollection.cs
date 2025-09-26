using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public enum CollectionStatus
{
    NotInstalled,
    Downloading,
    Ready
}

/// <summary>
/// Stores the dynamic state of a collection.
/// Static data can be found in the config.
/// </summary>
public class TileCollection
{
    [MaxLength(64)] public required string Id { get; init; }
    public CollectionStatus Status { get; set; }
    [MaxLength(512)] public string? ImagePath { get; set; }
    public DateTime? InstallDate { get; set; }
    public int TrueImageCount { get; set; } = -1;

    public ICollection<Project> Projects { get; init; } = null!;
}