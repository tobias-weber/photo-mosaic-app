using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
    private const int ThumbnailSizePx = 32;

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
        var relativePath = Path.Combine(
            "users", userName,
            "projects", projectId.ToString(),
            $"{imageId}{Path.GetExtension(file.FileName)}"
        ).Replace("\\", "/");
        var imageRef = new ImageRef
        {
            ImageId = imageId,
            IsTarget = isTarget,
            ProjectId = projectId,
            Name = file.FileName,
            ContentType = file.ContentType,
            FilePath = relativePath
        };

        var absPath = Path.Combine(_uploadPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);

        await using (var stream = new FileStream(absPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        if (!isTarget)
        {
            await SaveDownscaledRgbImageAsync(file, absPath);
        }

        _db.Images.Add(imageRef);
        await _db.SaveChangesAsync();

        return imageRef;
    }

    public async Task<(ImageRef, string)> GetImageRefAsync(string userName, Guid projectId, Guid imageId)
    {
        await CheckIfProjectValid(userName, projectId);
        var imageRef = await _db.Images.FindAsync(imageId);

        if (imageRef is null)
        {
            throw new InvalidOperationException($"Image {imageId} does not exist.");
        }


        var absPath = Path.Combine(_uploadPath, imageRef.FilePath);
        if (!File.Exists(absPath))
        {
            throw new FileNotFoundException($"Image {imageId} does not exist on disk.");
        }

        return (imageRef, absPath);
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
        var absPath = Path.Combine(_uploadPath, imageRef.FilePath);
        if (File.Exists(absPath))
        {
            File.Delete(absPath);
        }

        if (!imageRef.IsTarget)
        {
            var smPath = GetDownscaledPath(absPath);
            if (File.Exists(smPath))
            {
                File.Delete(smPath);
            }
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
            var absPath = Path.Combine(_uploadPath, imageRef.FilePath);
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
            }

            _db.Images.Remove(imageRef);
        }

        await _db.SaveChangesAsync();
    }

    public void DeleteMosaic(string userName, Guid projectId, Guid jobId)
    {
        var dirPath = GetMosaicDirPath(userName, projectId, jobId);
        Console.WriteLine($"Deleting {dirPath}");
        if (Directory.Exists(dirPath))
        {
            Console.WriteLine($"Deleted {dirPath}");
            Directory.Delete(dirPath, true);
        }
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

    public bool MosaicExists(string userName, Guid projectId, Guid jobId)
    {
        return File.Exists(GetMosaicPath(userName, projectId, jobId));
    }

    public bool DeepZoomExists(string userName, Guid projectId, Guid jobId)
    {
        return File.Exists(GetDeepZoomPath(userName, projectId, jobId, "dz.jpg.dzi").absPath);
    }

    private string GetMosaicDirPath(string userName, Guid projectId, Guid jobId)
    {
        return Path.Combine(_uploadPath,
            "users", userName,
            "projects", projectId.ToString(),
            "mosaics", jobId.ToString());
    }

    public string GetMosaicPath(string userName, Guid projectId, Guid jobId)
    {
        return Path.Combine(GetMosaicDirPath(userName, projectId, jobId), "mosaic.jpg");
    }

    public (string absPath, string contentType) GetDeepZoomPath(string userName, Guid projectId, Guid jobId,
        string filePath)
    {
        var path = Path.Combine(GetMosaicDirPath(userName, projectId, jobId), "dz", filePath);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".dzi" or ".xml" => "application/xml",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return (path, contentType);
    }

    private async Task CheckIfProjectValid(string userName, Guid projectId)
    {
        var userHasProject = await _db.Projects.AnyAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (!userHasProject)
        {
            throw new InvalidOperationException($"Specified project for user '{userName}' does not exist.");
        }
    }

    private async Task SaveDownscaledRgbImageAsync(IFormFile file, string originalPath)
    {
        var outputPath = GetDownscaledPath(originalPath);
        using var image = await Image.LoadAsync(file.OpenReadStream());

        // Compute new size keeping aspect ratio
        int width, height;
        if (image.Width < image.Height)
        {
            width = ThumbnailSizePx;
            height = (int)(image.Height / (float)image.Width * ThumbnailSizePx);
        }
        else
        {
            height = ThumbnailSizePx;
            width = (int)(image.Width / (float)image.Height * ThumbnailSizePx);
        }

        // Resize
        image.Mutate(x => x.Resize(width, height));

        // Convert to RGB (3 channels)
        if (image.PixelType.BitsPerPixel != 24)
        {
            using var rgbImage = image.CloneAs<Rgb24>();
            await rgbImage.SaveAsync(outputPath, new JpegEncoder());
        }
        else
        {
            await image.SaveAsync(outputPath, new JpegEncoder());
        }
    }

    private string GetDownscaledPath(string originalPath)
    {
        var dir = Path.GetDirectoryName(originalPath)!;
        var filenameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        return Path.Combine(dir, $"{filenameWithoutExt}_sm.jpg");
    }
}