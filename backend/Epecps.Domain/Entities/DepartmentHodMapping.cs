namespace Epecps.Domain.Entities;

/// <summary>
/// Maps one or more HOD users to a department.
/// </summary>
public class DepartmentHodMapping
{
    public int DeptId { get; set; }
    public int HodUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Department Department { get; set; } = null!;
    public User HodUser { get; set; } = null!;
}

