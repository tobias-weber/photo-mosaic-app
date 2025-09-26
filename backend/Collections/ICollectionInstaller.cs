namespace backend.Collections;

public interface ICollectionInstaller
{
    Task<bool> InstallAsync(TileCollectionConfig config, string dirPath);
}