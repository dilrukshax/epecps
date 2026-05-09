namespace Epecps.Application.DTOs.Admin;

public class UserAdminDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DeptId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
}

public class CreateUserAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int DeptId { get; set; }
    public List<string> Roles { get; set; } = new();
    public int? ReportingManagerId { get; set; }
}

public class UpdateUserAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DeptId { get; set; }
    public List<string> Roles { get; set; } = new();
    public int? ReportingManagerId { get; set; }
}
