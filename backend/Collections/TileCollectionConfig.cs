namespace backend.Collections;

public class TileCollectionConfig
{
    public required string Id { get; init; }
    public string Name { get; set; } = "";
    public int ImageCount { get; set; }
    public string Size { get; set; } = "";
    public string Description { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? SubDirectory { get; set; }
    public string InstallerType { get; set; } = "";
}