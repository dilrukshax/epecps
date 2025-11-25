namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// DTO for available peer reviewer
/// </summary>
public class AvailablePeerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
}
