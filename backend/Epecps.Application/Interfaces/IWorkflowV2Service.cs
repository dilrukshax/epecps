using Epecps.Application.DTOs.WorkflowV2;

namespace Epecps.Application.Interfaces;

public interface IWorkflowV2Service
{
    Task SubmitActivationPlanAsync(Guid goalSetId, int employeeUserId, SubmitActivationPlanRequestDto request, CancellationToken cancellationToken = default);
    Task ProcessActivationDecisionAsync(int evaluationId, int rmUserId, ActivationPlanDecisionDto request, CancellationToken cancellationToken = default);
    Task SubmitSelfEvaluationAsync(int evaluationId, int employeeUserId, SubmitSelfEvaluationV2Dto request, CancellationToken cancellationToken = default);
    Task TryAdvanceAfterParallelReviewAsync(int evaluationId, CancellationToken cancellationToken = default);
    Task HODFinalizeAsync(int evaluationId, int hodUserId, string? comment, CancellationToken cancellationToken = default);
    Task GmDecisionAsync(int evaluationId, int gmUserId, GmV2DecisionDto request, CancellationToken cancellationToken = default);
    Task<List<WorkflowReviewWeightDto>> GetReviewWeightsAsync(CancellationToken cancellationToken = default);
    Task<List<WorkflowReviewWeightDto>> UpdateReviewWeightsAsync(UpdateWorkflowReviewWeightsDto request, CancellationToken cancellationToken = default);
    Task<List<PipCaseDto>> GetPipCasesAsync(int? assignedHrUserId, string? status, CancellationToken cancellationToken = default);
    Task<PipCaseDto> AddPipActionItemAsync(int pipCaseId, PipActionItemCreateDto request, CancellationToken cancellationToken = default);
    Task<PipCaseDto> UpdatePipCaseAsync(int pipCaseId, PipCaseUpdateDto request, CancellationToken cancellationToken = default);
    Task<PipCaseDto> UpdatePipActionItemAsync(int pipActionItemId, PipActionItemUpdateDto request, CancellationToken cancellationToken = default);
}
