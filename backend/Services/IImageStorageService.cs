using backend.DTOs;
using backend.Models;
using Task = System.Threading.Tasks.Task;

namespace backend.Services;

public interface IImageStorageService
{
    Task<ImageRef> StoreImageAsync(string userName, Guid projectId, bool isTarget, IFormFile? file);
    
    Task<ImageRef> GetImageRefAsync(string userName, Guid projectId, Guid imageId);
    
    Task DeleteImageAsync(string userName, Guid projectId, Guid imageId);

    Task DeleteImagesAsync(string userName, Guid projectId, string? filter);
    
    Task<List<ImageRefDto>> GetImageRefsAsync(string userName, Guid projectId, string? filter);
    
}