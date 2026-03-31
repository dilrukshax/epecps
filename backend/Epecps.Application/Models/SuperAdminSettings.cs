namespace Epecps.Application.Models;

public class SuperAdminSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "Super Admin";
    public int? DepartmentId { get; set; }
}
