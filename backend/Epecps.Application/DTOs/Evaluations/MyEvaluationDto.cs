namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// DTO for showing all evaluations where the user is involved
/// </summary>
public class MyEvaluationDto
{
    public int EvaluationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MyRole { get; set; } = string.Empty;  // Employee, RM, TL, Peer, HOD, GM
    public DateTime? SubmittedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
}
