namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for suggested activity in the goal framework
/// </summary>
public class SuggestedActivityDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
}
