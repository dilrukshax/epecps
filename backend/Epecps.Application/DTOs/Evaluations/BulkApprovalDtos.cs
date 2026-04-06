namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// DTO for bulk approval candidates (evaluations eligible for bulk approval)
/// </summary>
public class BulkApprovalCandidateDto
{
    public int EvaluationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    public decimal ScorePercentage { get; set; } // Score as percentage (0-100)
    public bool IsEligibleForPromotion { get; set; } // Score >= 85%
    public int CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateTime? LastReviewedAt { get; set; }
    public string? RecommendedByHodName { get; set; }
    public DateTime? RecommendedAt { get; set; }
}

/// <summary>
/// Request DTO for bulk approval
/// </summary>
public class BulkApprovalRequestDto
{
    public List<int> EvaluationIds { get; set; } = new List<int>();
    public string? Comment { get; set; }
}

/// <summary>
/// Response DTO for bulk approval operations
/// </summary>
public class BulkApprovalResponseDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BulkApprovalResultItemDto> Results { get; set; } = new List<BulkApprovalResultItemDto>();
}

/// <summary>
/// Individual result item for bulk approval
/// </summary>
public class BulkApprovalResultItemDto
{
    public int EvaluationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NewStatus { get; set; }
}

/// <summary>
/// Summary stats for bulk approval dashboard
/// </summary>
public class BulkApprovalStatsDto
{
    public int PendingGmApproval { get; set; }
    public int PendingHrProcessing { get; set; }
    public int EligibleForPromotion { get; set; } // Score >= 85%
    public int NotEligibleForPromotion { get; set; } // Score < 85%
    public decimal AverageScore { get; set; }
}
