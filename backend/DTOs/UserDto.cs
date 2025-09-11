using backend.Models;

namespace backend.DTOs;

public class UserDto(User user)
{
    public Guid UserId { get; set; } = user.Id;
    public string UserName { get; set; } = user.UserName!;
    public DateTime CreatedAt { get; set; } = user.CreatedAt;
}