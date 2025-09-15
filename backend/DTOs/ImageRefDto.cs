using backend.Models;

namespace backend.DTOs;

public class ImageRefDto(ImageRef imageRef)
{
    public Guid ImageId { get; init; } = imageRef.ImageId;
    public string Name { get; set; } = imageRef.Name;
    public bool IsTarget { get; set; } = imageRef.IsTarget;
}