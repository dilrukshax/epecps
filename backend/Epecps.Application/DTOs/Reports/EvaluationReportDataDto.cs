namespace Epecps.Application.DTOs.Reports;

/// <summary>
/// Data row for evaluation report
/// </summary>
public class EvaluationReportDataDto
{
    public int EvaluationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    public string? ReportingManagerName { get; set; }
    public string? TeamLeadName { get; set; }
    public bool IsPromoted { get; set; }
    public string? PromotionStatus { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    
    // Review scores
    public decimal? RmScore { get; set; }
    public decimal? TlScore { get; set; }
    public decimal? PeerScore1 { get; set; }
    public decimal? PeerScore2 { get; set; }
    public decimal? HodScore { get; set; }
    public decimal? GmScore { get; set; }
}
