using Epecps.Application.DTOs.Admin;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

public class AdminProjectService : IAdminProjectService
{
    private readonly EpecpsDbContext _context;

    public AdminProjectService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.ProjectManagerUser)
            .Include(p => p.SupervisorUser)
            .Include(p => p.UserProjectAssignments)
                .ThenInclude(upa => upa.User)
            .ToListAsync();

        return projects.Select(MapToDto).ToList();
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectManagerUser)
            .Include(p => p.SupervisorUser)
            .Include(p => p.UserProjectAssignments)
                .ThenInclude(upa => upa.User)
            .FirstOrDefaultAsync(p => p.ProjectId == id);

        if (project == null) return null;

        return MapToDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        var project = new Project
        {
            ProjectCode = dto.ProjectCode,
            ProjectName = dto.ProjectName,
            Status = dto.Status,
            ProjectManagerUserId = dto.ProjectManagerUserId,
            SupervisorUserId = dto.SupervisorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return await GetProjectByIdAsync(project.ProjectId) ?? throw new Exception("Created project not found");
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectDto dto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) throw new KeyNotFoundException($"Project with ID {id} not found");

        project.ProjectCode = dto.ProjectCode;
        project.ProjectName = dto.ProjectName;
        project.Status = dto.Status;
        project.ProjectManagerUserId = dto.ProjectManagerUserId;
        project.SupervisorUserId = dto.SupervisorUserId;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetProjectByIdAsync(id) ?? throw new Exception("Updated project not found");
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) throw new KeyNotFoundException($"Project with ID {id} not found");

        // Clean up assignments first
        var assignments = await _context.UserProjectAssignments.Where(a => a.ProjectId == id).ToListAsync();
        _context.UserProjectAssignments.RemoveRange(assignments);

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public async Task AssignTechLeadAsync(int projectId, int userId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) throw new KeyNotFoundException($"Project with ID {projectId} not found");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new KeyNotFoundException($"User with ID {userId} not found");

        var existingAssignment = await _context.UserProjectAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.UserId == userId);

        if (existingAssignment == null)
        {
            _context.UserProjectAssignments.Add(new UserProjectAssignment
            {
                ProjectId = projectId,
                UserId = userId,
                AssignmentRole = "TL",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingAssignment.AssignmentRole = "TL";
            existingAssignment.IsActive = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveTechLeadAsync(int projectId, int userId)
    {
        var assignment = await _context.UserProjectAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.UserId == userId && a.AssignmentRole == "TL");

        if (assignment != null)
        {
            _context.UserProjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    private ProjectDto MapToDto(Project p)
    {
        return new ProjectDto
        {
            ProjectId = p.ProjectId,
            ProjectCode = p.ProjectCode,
            ProjectName = p.ProjectName,
            Status = p.Status,
            ProjectManagerUserId = p.ProjectManagerUserId,
            ProjectManagerName = p.ProjectManagerUser?.FullName,
            SupervisorUserId = p.SupervisorUserId,
            SupervisorName = p.SupervisorUser?.FullName,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            TechLeads = p.UserProjectAssignments
                .Where(a => a.AssignmentRole == "TL" && a.IsActive)
                .Select(a => new ProjectTlDto
                {
                    UserId = a.UserId,
                    FullName = a.User.FullName,
                    Email = a.User.Email
                }).ToList()
        };
    }
}
