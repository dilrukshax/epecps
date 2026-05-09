namespace Epecps.Application.DTOs.Admin;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ProjectManagerUserId { get; set; }
    public string? ProjectManagerName { get; set; }
    public int? SupervisorUserId { get; set; }
    public string? SupervisorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProjectTlDto> TechLeads { get; set; } = new();
}

public class ProjectTlDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateProjectDto
{
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int? ProjectManagerUserId { get; set; }
    public int? SupervisorUserId { get; set; }
}

public class UpdateProjectDto
{
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ProjectManagerUserId { get; set; }
    public int? SupervisorUserId { get; set; }
}

public class AssignTlDto
{
    public int UserId { get; set; }
}
