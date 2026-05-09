using Epecps.Application.DTOs.Admin;

namespace Epecps.Application.Interfaces;

public interface IAdminProjectService
{
    Task<List<ProjectDto>> GetAllProjectsAsync();
    Task<ProjectDto?> GetProjectByIdAsync(int id);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectDto dto);
    Task DeleteProjectAsync(int id);
    
    Task AssignTechLeadAsync(int projectId, int userId);
    Task RemoveTechLeadAsync(int projectId, int userId);
}
