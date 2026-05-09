namespace Epecps.Application.DTOs.Admin;

public class DepartmentDto
{
    public int DeptId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentDeptId { get; set; }
    public string? ParentDeptName { get; set; }
    public List<DepartmentHodDto> Hods { get; set; } = new();
}

public class DepartmentHodDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentDeptId { get; set; }
}

public class UpdateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentDeptId { get; set; }
}

public class AssignHodDto
{
    public int UserId { get; set; }
}
