using backend.Collections;
using backend.Models;

namespace backend.DTOs;

public class TileCollectionDto(TileCollectionConfig config, TileCollection? collection)
{
    public string Id { get; init; } = config.Id;
    public string Name { get; init; } = config.Name;
    public int ImageCount { get; init; } = collection?.TrueImageCount > 0 ? collection.TrueImageCount : config.ImageCount;
    public string Size { get; init; } = config.Size;
    public string Description { get; init; } = config.Description;
    public CollectionStatus Status { get; set; } = collection?.Status ?? CollectionStatus.NotInstalled;
    public DateTime? InstallDate { get; set; } = collection?.InstallDate;
}