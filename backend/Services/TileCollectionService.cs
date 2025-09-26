using backend.Collections;
using backend.Collections.Installers;
using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class TileCollectionService : ITileCollectionService
{
    private readonly AppDbContext _db;
    private readonly List<TileCollectionConfig> _configs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IImageStorageService _storage;

    public TileCollectionService(AppDbContext db, IOptions<List<TileCollectionConfig>> options,
        IServiceScopeFactory scopeFactory, IImageStorageService storage)
    {
        _db = db;
        _configs = options.Value;
        _scopeFactory = scopeFactory;
        _storage = storage;
    }

    public async Task InitTileCollectionsAsync()
    {
        Console.WriteLine($"Initializing tile collections {_configs.Count}");
        foreach (var config in _configs)
        {
            Console.WriteLine($"Config item: {config.Id}, {config.Name}");
            var existing = await _db.TileCollections
                .FindAsync(config.Id);

            if (existing == null)
            {
                _db.TileCollections.Add(new TileCollection
                {
                    Id = config.Id,
                    Status = CollectionStatus.NotInstalled
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<TileCollectionDto>> GetCollectionsAsync()
    {
        var dbCollections = await _db.TileCollections.ToListAsync();
        return _configs.Select(config =>
        {
            var collection = dbCollections.Find(d => d.Id == config.Id);
            return new TileCollectionDto(config, collection);
        }).ToList();
    }

    public async Task StartInstallationAsync(string collectionId)
    {
        var config = _configs.Find(c => c.Id == collectionId);
        if (config is null)
            throw new InvalidOperationException($"Collection {collectionId} not found.");

        var dbCollection = await _db.TileCollections.FindAsync(config.Id);
        if (dbCollection is null)
            throw new InvalidOperationException($"Collection {config.Id} not found");
        if (dbCollection.Status != CollectionStatus.NotInstalled)
            throw new InvalidOperationException($"Collection {config.Id} is already installed or being installed");

        dbCollection.Status = CollectionStatus.Downloading;
        await _db.SaveChangesAsync();

        // Run installation in background
        Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope(); // required to ensure dependencies stay alive
                var storage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var installer = ResolveInstaller(config, scope);
                var dirPath = storage.CreateAndGetCollectionPath(collectionId);
                var isSuccess = await installer.InstallAsync(config, dirPath );

                dbCollection = (await db.TileCollections.FindAsync(config.Id))!;
                if (isSuccess)
                {
                    dbCollection.TrueImageCount = Directory.GetFiles(dirPath).Length;
                    await GenerateSmallImages(dirPath);
                    dbCollection.Status = CollectionStatus.Ready;
                    dbCollection.InstallDate = DateTime.UtcNow;
                }
                else
                {
                    dbCollection.Status = CollectionStatus.NotInstalled;
                }

                Console.WriteLine("Before saving changes");
                await db.SaveChangesAsync();
                Console.WriteLine($"Installation completed, status is {dbCollection.Status}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public async Task UninstallAsync(string collectionId)
    {
        var config = _configs.Find(c => c.Id == collectionId);
        if (config is null)
            throw new InvalidOperationException($"Collection {collectionId} not found.");

        var dbCollection = await _db.TileCollections.FindAsync(config.Id);
        if (dbCollection is null)
            throw new InvalidOperationException($"Collection {config.Id} not found");

        dbCollection.Status = CollectionStatus.NotInstalled;
        dbCollection.InstallDate = null;
        dbCollection.TrueImageCount = -1;
        await _db.SaveChangesAsync();

        var dirPath = _storage.CreateAndGetCollectionPath(collectionId);
        Directory.Delete(dirPath, true);
    }

    public async Task SelectCollectionAsync(string userName, Guid projectId, string collectionId)
    {
        var project = await _db.Projects.Include(p => p.TileCollections)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (project is null)
            throw new InvalidOperationException($"Project {projectId} not found.");
        var dbCollection = await _db.TileCollections.FindAsync(collectionId);
        if (dbCollection is null)
            throw new InvalidOperationException($"Collection {collectionId} not found");
        project.TileCollections.Add(dbCollection);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetSelectedCollectionIds(string userName, Guid projectId)
    {
        var project = await _db.Projects
            .Include(project => project.TileCollections)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (project is null)
            throw new InvalidOperationException($"Project {projectId} not found.");
        return project.TileCollections.Select(t => t.Id).ToList();
    }

    public async Task DeselectCollectionAsync(string userName, Guid projectId, string collectionId)
    {
        var project = await _db.Projects.Include(project => project.TileCollections)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (project is null)
            throw new InvalidOperationException($"Project {projectId} not found.");
        var dbCollection = await _db.TileCollections.FindAsync(collectionId);
        if (dbCollection is null)
            throw new InvalidOperationException($"Collection {collectionId} not found");
        project.TileCollections.Remove(dbCollection);
        await _db.SaveChangesAsync();
    }

    private ICollectionInstaller ResolveInstaller(TileCollectionConfig config, IServiceScope scope)
    {
        return config.InstallerType.ToLower() switch
        {
            "zip" => scope.ServiceProvider.GetRequiredService<ZipCollectionInstaller>(),
            _ => throw new NotSupportedException($"Installer for type '{config.InstallerType}' not found.")
        };
    }

    private static async Task GenerateSmallImages(string dirPath)
    {
        var filePaths = Directory.GetFiles(dirPath);
        foreach (var filePath in filePaths)
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                await ImageStorageService.SaveDownscaledRgbImageAsync(stream, filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An error occurred processing file {filePath}: {ex.Message}");
            }
        }
    }
}