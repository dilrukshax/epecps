namespace Epecps.Application.DTOs.Reports;

/// <summary>
/// Filter criteria for evaluation reports
/// </summary>
public class EvaluationReportFilterDto
{
    public int? CycleId { get; set; }
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public bool? OnlyPromoted { get; set; }
    public decimal? MinScore { get; set; }
    public decimal? MaxScore { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
