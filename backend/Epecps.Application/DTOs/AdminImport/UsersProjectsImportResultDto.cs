namespace Epecps.Application.DTOs.AdminImport;

public class UsersProjectsImportResultDto
{
    public int TotalRows { get; set; }
    public int CreatedUsers { get; set; }
    public int UpdatedUsers { get; set; }
    public int CreatedRoleAssignments { get; set; }
    public int RemovedRoleAssignments { get; set; }
    public int CreatedProjects { get; set; }
    public int UpdatedProjects { get; set; }
    public int CreatedAssignments { get; set; }
    public int UpdatedAssignments { get; set; }
    public int CreatedManagerMappings { get; set; }
    public int UpdatedManagerMappings { get; set; }
    public int CreatedDepartmentHodMappings { get; set; }
    public int SkippedRows { get; set; }
    public List<UsersProjectsImportRowErrorDto> Errors { get; set; } = new();
}

public class UsersProjectsImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
