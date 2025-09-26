using backend.DTOs;

namespace backend.Services;

public interface ITileCollectionService
{
    Task InitTileCollectionsAsync();

    Task<List<TileCollectionDto>> GetCollectionsAsync();

    Task StartInstallationAsync(string collectionId);
    
    Task UninstallAsync(string collectionId);
    Task SelectCollectionAsync(string userName, Guid projectId, string collectionId);
    Task<List<string>> GetSelectedCollectionIds(string userName, Guid projectId);
    Task DeselectCollectionAsync(string userName, Guid projectId, string collectionId);
}