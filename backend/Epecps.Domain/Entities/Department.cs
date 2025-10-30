namespace Epecps.Domain.Entities;

public class Department
{
    public int DeptId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentDeptId { get; set; }

    // Navigation properties
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
