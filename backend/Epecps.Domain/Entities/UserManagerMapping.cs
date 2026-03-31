namespace Epecps.Domain.Entities;

/// <summary>
/// Maps employees to one or more reporting managers imported from Excel.
/// </summary>
public class UserManagerMapping
{
    public int EmployeeUserId { get; set; }
    public int ManagerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User EmployeeUser { get; set; } = null!;
    public User ManagerUser { get; set; } = null!;
}

