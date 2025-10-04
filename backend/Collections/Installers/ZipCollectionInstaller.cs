using System.IO.Compression;
using backend.Data;
using backend.Helpers;

namespace backend.Collections.Installers;

public class ZipCollectionInstaller : ICollectionInstaller
{
    private readonly HttpClient _httpClient;


    public ZipCollectionInstaller(HttpClient httpClient, AppDbContext db)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> InstallAsync(TileCollectionConfig config, string dirPath)
    {
        var zipPath = Path.Combine(dirPath, "collection.zip");
        await DownloadAsync(config.DownloadUrl, zipPath);
        return UnzipAndDeleteZip(zipPath, dirPath, config.SubDirectory);
    }

    private async Task DownloadAsync(string url, string path)
    {
        // Use HttpCompletionOption.ResponseHeadersRead to download the file in a streaming fashion.
        // This tells HttpClient to return as soon as the response headers are read,
        // before the entire content is buffered in memory.
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await contentStream.CopyToAsync(fileStream);

        Console.WriteLine($"Successfully downloaded file from {url} to {path}.");
    }

    private bool UnzipAndDeleteZip(string zipPath, string dirPath, string? pathInZip = null)
    {
        var normalizedPathInZip = pathInZip is null || pathInZip.Length == 0
            ? null
            : pathInZip.Trim('/').Replace('\\', '/') + "/";
        var isSuccess = true;
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            // Iterate through each entry (file or folder) in the zip archive (includes files in subdirectories).
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || !FileHelpers.IsImageFile(entry.FullName)) continue;

                // If a target path is specified, ensure the entry is inside it
                if (normalizedPathInZip is not null)
                {
                    var normalized = entry.FullName.Replace('\\', '/');
                    if (!normalized.StartsWith(normalizedPathInZip, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var extractedFilePath = Path.Combine(dirPath, Path.GetFileName(entry.FullName));
                entry.ExtractToFile(extractedFilePath,
                    overwrite: true); // this might drop files if they do not have unique names
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during extraction: {ex.Message}");
            isSuccess = false;
        }

        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"Could not delete the zip file: {ioEx.Message}");
            isSuccess = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while deleting the file: {ex.Message}");
            isSuccess = false;
        }

        return isSuccess;
    }
}