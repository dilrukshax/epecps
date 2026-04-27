namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// Request to approve or reject an evaluation at a specific approval stage
/// </summary>
public class ApprovalActionDto
{
    public string? Comment { get; set; }
}

/// <summary>
/// Request to assign peer reviewers (only for Team Lead)
/// </summary>
public class AssignPeersDto
{
    public int PeerUserId1 { get; set; }
    public int PeerUserId2 { get; set; }
}

/// <summary>
/// Team Lead combined review request:
/// submit TL per-goal scores and assign both peer reviewers in one action.
/// </summary>
public class SubmitTlCombinedReviewDto
{
    public List<ReviewItemScoreDto> GoalScores { get; set; } = new();
    public string? OverallComment { get; set; }
    public int PeerUserId1 { get; set; }
    public int PeerUserId2 { get; set; }
}

/// <summary>
/// Response after submitting a goal set for evaluation
/// </summary>
public class SubmitGoalSetResponseDto
{
    public int EvaluationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request for HR to process promotion (after GM approval)
/// </summary>
public class HrProcessDto
{
    public bool Proceed { get; set; }
    public string? Comment { get; set; }
}
