using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace backend.Services;

public class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif"
    ];

    private const int MaxFileSize = 10 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly string _uploadPath;

    public ImageStorageService(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        var path = config.GetValue<string>("UploadPath")
                   ?? throw new InvalidOperationException("UploadPath missing in config");
        _uploadPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(env.ContentRootPath, path);
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<ImageRef> StoreImageAsync(string userName, Guid projectId, bool isTarget, IFormFile? file)
    {
        await CheckIfProjectValid(userName, projectId);
        
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("File is empty.", nameof(file));
        }

        if (!AllowedTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException($"Unsupported file type: {file.ContentType}");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"File exceeds the {MaxFileSize / (1024 * 1024)} MB limit.");
        }

        var imageId = Guid.NewGuid();
        var imageRef = new ImageRef
        {
            ImageId = imageId,
            IsTarget = isTarget,
            ProjectId = projectId,
            Name = file.FileName,
            ContentType = file.ContentType,
            FilePath = Path.Combine(
                _uploadPath,
                "users", userName,
                "projects", projectId.ToString(),
                $"{imageId}{Path.GetExtension(file.FileName)}"
            )
        };

        Directory.CreateDirectory(Path.GetDirectoryName(imageRef.FilePath)!);

        await using (var stream = new FileStream(imageRef.FilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _db.Images.Add(imageRef);
        await _db.SaveChangesAsync();

        return imageRef;
    }

    public async Task<ImageRef> GetImageRefAsync(string userName, Guid projectId, Guid imageId)
    {
        await CheckIfProjectValid(userName, projectId);
        var imageRef = await _db.Images.FindAsync(imageId);

        if (imageRef is null)
        {
            throw new InvalidOperationException($"Image {imageId} does not exist.");
        }

        if (!File.Exists(imageRef.FilePath))
        {
            throw new FileNotFoundException($"Image {imageId} does not exist on disk.");
        }
        
        return imageRef;
    }

    public async Task DeleteImageAsync(string userName, Guid projectId, Guid imageId)
    {
        await CheckIfProjectValid(userName, projectId);
        var imageRef = await _db.Images.FindAsync(imageId);
        if (imageRef == null)
        {
            throw new InvalidOperationException($"Image {imageId} does not exist.");
        }
        // TODO: check if task uses image
        if (File.Exists(imageRef.FilePath))
        {
            File.Delete(imageRef.FilePath);
        }
        _db.Images.Remove(imageRef);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteImagesAsync(string userName, Guid projectId, string? filter)
    {
        await CheckIfProjectValid(userName, projectId);

        var filtered = filter?.ToUpper() switch
        {
            "TARGETS" => _db.Images.Where(i => i.ProjectId == projectId && i.IsTarget),
            "TILES" => _db.Images.Where(i => i.ProjectId == projectId && !i.IsTarget),
            "ALL" or null => _db.Images.Where(i => i.ProjectId == projectId), // default
            _ => throw new ArgumentException("Invalid filter. Allowed values: ALL, TARGETS, TILES")
        };
        var imageRefs = await filtered.ToListAsync();

        foreach (var imageRef in imageRefs)
        {
            // TODO: check if task uses image
            if (File.Exists(imageRef.FilePath))
            {
                File.Delete(imageRef.FilePath);
            }
            _db.Images.Remove(imageRef);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<ImageRefDto>> GetImageRefsAsync(string userName, Guid projectId, string? filter)
    {
        await CheckIfProjectValid(userName, projectId);

        var filtered = filter?.ToUpper() switch
        {
            "TARGETS" => _db.Images.Where(i => i.ProjectId == projectId && i.IsTarget),
            "TILES" => _db.Images.Where(i => i.ProjectId == projectId && !i.IsTarget),
            "ALL" or null => _db.Images.Where(i => i.ProjectId == projectId), // default
            _ => throw new ArgumentException("Invalid filter. Allowed values: ALL, TARGETS, TILES")
        };

        return await filtered.Select(i => new ImageRefDto(i)).ToListAsync();
    }

    private async Task CheckIfProjectValid(string userName, Guid projectId)
    {
        var userHasProject = await _db.Projects.AnyAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (!userHasProject)
        {
            throw new InvalidOperationException($"Specified project for user '{userName}' does not exist.");
        }
    }
}