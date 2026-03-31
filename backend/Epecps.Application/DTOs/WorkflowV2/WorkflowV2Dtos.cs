namespace Epecps.Application.DTOs.WorkflowV2;

public class GoalActivationMethodDto
{
    public Guid GoalAssignmentId { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class SubmitActivationPlanRequestDto
{
    public List<GoalActivationMethodDto> Goals { get; set; } = new();
}

public class ActivationPlanDecisionDto
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
    public List<Guid> RejectedGoalAssignmentIds { get; set; } = new();
}

public class SubmitSelfEvaluationV2Dto
{
    public string? OverallComment { get; set; }
    public List<SelfEvaluationGoalInputDto> Goals { get; set; } = new();
}

public class SelfEvaluationGoalInputDto
{
    public Guid PersonalGoalId { get; set; }
    public decimal Score { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string EvidenceUrl { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class GmV2DecisionDto
{
    public bool Approve { get; set; }
    public bool VacancyAvailable { get; set; }
    public string? Comment { get; set; }
}

public class HodFinalizeRequestDto
{
    public string? Comment { get; set; }
}

public class WorkflowReviewWeightDto
{
    public string ReviewerKey { get; set; } = string.Empty; // Self, TL, RM, Peer1, Peer2
    public decimal WeightPercent { get; set; }
}

public class UpdateWorkflowReviewWeightsDto
{
    public List<WorkflowReviewWeightDto> Weights { get; set; } = new();
}

public class PipActionItemCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TrainingMaterialId { get; set; }
    public string? ExternalTrainingLink { get; set; }
    public DateTime? DueDate { get; set; }
}

public class PipCaseUpdateDto
{
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Reason { get; set; }
}

public class PipActionItemUpdateDto
{
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}
