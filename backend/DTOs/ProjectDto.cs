using backend.Models;

namespace backend.DTOs;

public class ProjectDto(Project project)
{
    public Guid ProjectId { get; init; } = project.ProjectId;
    public string Title { get; set; } = project.Title;
    public DateTime CreatedAt { get; init; } = project.CreatedAt;
}