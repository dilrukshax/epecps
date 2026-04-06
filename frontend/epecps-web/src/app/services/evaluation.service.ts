import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  SubmitGoalSetResponseDto,
  PendingApprovalDto,
  EvaluationDetailDto,
  ApprovalActionDto,
  AssignPeersDto,
  PromotionDecisionDto,
  AvailablePeerDto,
  MyEvaluationDto,
  GoalActionResponseDto,
  CompleteGoalRequestDto,
  SubmitRmScoringDto,
  SubmitOverallScoringDto,
  SubmitReviewWithGoalScoresDto,
  ReviewScoringResponseDto,
  BulkApprovalStatsDto,
  BulkApprovalCandidateDto,
  BulkApprovalRequestDto,
  BulkApprovalResponseDto,
  HodScoreSubmissionDto,
  HodScoreSubmissionResponseDto,
  EvaluationReportFilterDto,
  EvaluationReportDataDto,
  CycleDto,
  DepartmentDto,
  SubmitActivationPlanRequestDto,
  ActivationPlanDecisionDto,
  SubmitSelfEvaluationV2Dto,
  GmV2DecisionDto,
  WorkflowReviewWeightDto,
  UpdateWorkflowReviewWeightsDto,
  PipCaseDto,
  PipCaseUpdateDto,
  PipActionItemCreateDto,
  PipActionItemUpdateDto,
  SubmitTlCombinedReviewDto
} from '../models/evaluation.models';

@Injectable({
  providedIn: 'root'
})
export class EvaluationService {
  private apiUrl = environment.apiUrl;
  private workflowV2BaseUrl = `${environment.apiUrl}/api/v2/workflow`;

  constructor(private http: HttpClient) {}

  /**
   * Submit a goal set for evaluation
   */
  submitGoalSetForEvaluation(goalSetId: string): Observable<SubmitGoalSetResponseDto> {
    return this.http.post<SubmitGoalSetResponseDto>(
      `${this.apiUrl}/api/employee-goals/sets/${goalSetId}/submit-for-evaluation`,
      {}
    );
  }

  /**
   * Get all pending approvals for the current user
   */
  getPendingApprovals(): Observable<PendingApprovalDto[]> {
    return this.http.get<PendingApprovalDto[]>(
      `${this.apiUrl}/api/evaluations/pending-approvals`
    );
  }

  /**
   * Get all evaluations where current user is involved (pending + completed)
   */
  getMyEvaluations(): Observable<MyEvaluationDto[]> {
    return this.http.get<MyEvaluationDto[]>(
      `${this.apiUrl}/api/evaluations/my-evaluations`
    );
  }

  /**
   * Get detailed evaluation information
   */
  getEvaluationDetail(evaluationId: number): Observable<EvaluationDetailDto> {
    return this.http.get<EvaluationDetailDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}`
    );
  }

  /**
   * Approve an evaluation at the current stage
   */
  approveEvaluation(evaluationId: number, comment?: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/approve`,
      body
    );
  }

  /**
   * Reject an evaluation and return it to the employee
   */
  rejectEvaluation(evaluationId: number, comment: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/reject`,
      body
    );
  }

  /**
   * Get available peer reviewers for an evaluation
   */
  getAvailablePeers(evaluationId: number): Observable<AvailablePeerDto[]> {
    return this.http.get<AvailablePeerDto[]>(
      `${this.apiUrl}/api/evaluations/${evaluationId}/available-peers`
    );
  }

  /**
   * Assign peer reviewers (Team Lead only)
   */
  assignPeerReviewers(evaluationId: number, peerUserId1: number, peerUserId2: number): Observable<any> {
    const body: AssignPeersDto = { peerUserId1, peerUserId2 };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/assign-peers`,
      body
    );
  }

  submitTlCombinedReview(evaluationId: number, payload: SubmitTlCombinedReviewDto): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/tl/combined-submit`,
      payload
    );
  }

  /**
   * Process promotion decision (GM only)
   */
  processPromotionDecision(evaluationId: number, approve: boolean, comment?: string): Observable<any> {
    const body: PromotionDecisionDto = { approve, comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/promotion-decision`,
      body
    );
  }

  /**
   * HOD recommends employee for promotion (HOD only)
   */
  recommendPromotion(evaluationId: number, comment?: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/hod/recommend`,
      body
    );
  }

  /**
   * HOD rejects evaluation (HOD only)
   */
  hodReject(evaluationId: number, comment: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/hod/reject`,
      body
    );
  }

  /**
   * GM makes promotion decision (GM only)
   */
  gmDecision(evaluationId: number, approve: boolean, comment?: string): Observable<any> {
    const body: PromotionDecisionDto = { approve, comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/promotion-decision`,
      body
    );
  }

  gmV2Decision(evaluationId: number, approve: boolean, vacancyAvailable: boolean, comment?: string): Observable<any> {
    const body: GmV2DecisionDto = { approve, vacancyAvailable, comment };
    return this.http.post(
      `${this.workflowV2BaseUrl}/evaluations/${evaluationId}/gm/decision`,
      body
    );
  }

  /**
   * HR processes promotion (HR only)
   */
  hrProcessPromotion(evaluationId: number, proceed: boolean, comment?: string): Observable<any> {
    const body = { proceed, comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/hr/process`,
      body
    );
  }

  // ====== NEW: Goal Start/Complete Methods for RM-first flow ======

  /**
   * Start working on a goal after RM approval
   * Goal must be in ApprovedByRM status
   * @param goalId The GUID of the goal to start
   */
  startGoal(goalId: string): Observable<GoalActionResponseDto> {
    return this.http.post<GoalActionResponseDto>(
      `${this.apiUrl}/api/employee-goals/${goalId}/start`,
      {}
    );
  }

  /**
   * Mark a goal as completed
   * Goal must be in InProgress status
   * If all goals in the evaluation are completed, triggers workflow continuation
   * @param goalId The GUID of the goal to complete
   * @param payload Optional evidence URL and comment
   */
  completeGoal(goalId: string, payload?: CompleteGoalRequestDto): Observable<GoalActionResponseDto> {
    return this.http.post<GoalActionResponseDto>(
      `${this.apiUrl}/api/employee-goals/${goalId}/complete`,
      payload || {}
    );
  }

  /**
   * Refresh evaluation detail after goal actions
   * Useful for updating UI state after start/complete
   */
  refreshEvaluationDetail(evaluationId: number): Observable<EvaluationDetailDto> {
    return this.getEvaluationDetail(evaluationId);
  }

  // ====== NEW: Review Scoring Methods ======

  /**
   * Submit RM review scores (item-level scores for each goal)
   * @param evaluationId The evaluation ID
   * @param reviewId The review ID for the RM review
   * @param payload Item-level scores for each goal
   */
  submitRmScoring(evaluationId: number, reviewId: number, payload: SubmitRmScoringDto): Observable<ReviewScoringResponseDto> {
    return this.http.post<ReviewScoringResponseDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}/reviews/${reviewId}/rm-scores`,
      payload
    );
  }

  /**
   * Submit overall review score (TL/Peer/HOD/GM)
   * @param evaluationId The evaluation ID
   * @param reviewId The review ID
   * @param payload Overall score and comment
   */
  submitOverallScoring(evaluationId: number, reviewId: number, payload: SubmitOverallScoringDto): Observable<ReviewScoringResponseDto> {
    return this.http.post<ReviewScoringResponseDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}/reviews/${reviewId}/overall-score`,
      payload
    );
  }

  /**
   * Submit per-goal scores for any reviewer role (TL/Peer/HOD).
   * Enables individual goal scoring for all reviewer roles.
   * @param evaluationId The evaluation ID
   * @param reviewId The review ID
   * @param payload Per-goal scores and optional overall score/comment
   */
  submitGoalScores(evaluationId: number, reviewId: number, payload: SubmitReviewWithGoalScoresDto): Observable<ReviewScoringResponseDto> {
    return this.http.post<ReviewScoringResponseDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}/reviews/${reviewId}/goal-scores`,
      payload
    );
  }

  // ========== NEW: Bulk Approval Methods ==========

  /**
   * Get bulk approval statistics for GM/HR dashboard
   */
  getBulkApprovalStats(): Observable<BulkApprovalStatsDto> {
    return this.http.get<BulkApprovalStatsDto>(
      `${this.apiUrl}/api/evaluations/bulk-approval/stats`
    );
  }

  /**
   * Get all evaluations pending GM approval (for bulk approval)
   */
  getPendingGmBulkApprovals(): Observable<BulkApprovalCandidateDto[]> {
    return this.http.get<BulkApprovalCandidateDto[]>(
      `${this.apiUrl}/api/evaluations/bulk-approval/gm-pending`
    );
  }

  /**
   * Get all evaluations pending HR processing (for bulk processing)
   */
  getPendingHrBulkProcessing(): Observable<BulkApprovalCandidateDto[]> {
    return this.http.get<BulkApprovalCandidateDto[]>(
      `${this.apiUrl}/api/evaluations/bulk-approval/hr-pending`
    );
  }

  /**
   * GM bulk approves multiple evaluations at once
   */
  gmBulkApprove(request: BulkApprovalRequestDto): Observable<BulkApprovalResponseDto> {
    return this.http.post<BulkApprovalResponseDto>(
      `${this.apiUrl}/api/evaluations/bulk-approval/gm-approve`,
      request
    );
  }

  /**
   * HR bulk processes multiple promotions at once
   */
  hrBulkProcess(request: BulkApprovalRequestDto): Observable<BulkApprovalResponseDto> {
    return this.http.post<BulkApprovalResponseDto>(
      `${this.apiUrl}/api/evaluations/bulk-approval/hr-process`,
      request
    );
  }

  /**
   * HOD submits overall score for an evaluation
   * If score >= 8.5 (85%), routes to GM
   * If score < 8.5 (85%), routes directly to HR
   */
  hodSubmitScore(evaluationId: number, payload: HodScoreSubmissionDto): Observable<HodScoreSubmissionResponseDto> {
    return this.http.post<HodScoreSubmissionResponseDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}/hod/submit-score`,
      payload
    );
  }

  // ========== REPORT METHODS ==========

  /**
   * Get evaluation report data with filters
   */
  getEvaluationReportData(filter: EvaluationReportFilterDto): Observable<EvaluationReportDataDto[]> {
    return this.http.post<EvaluationReportDataDto[]>(
      `${this.apiUrl}/api/reports/evaluations/data`,
      filter
    );
  }

  /**
   * Download evaluation report as Excel
   */
  downloadEvaluationReport(filter: EvaluationReportFilterDto): Observable<Blob> {
    return this.http.post(
      `${this.apiUrl}/api/reports/evaluations/download`,
      filter,
      { responseType: 'blob' }
    );
  }

  /**
   * Get available cycles for filtering
   */
  getCycles(): Observable<CycleDto[]> {
    return this.http.get<CycleDto[]>(
      `${this.apiUrl}/api/reports/cycles`
    );
  }

  /**
   * Get available departments for filtering
   */
  getDepartments(): Observable<DepartmentDto[]> {
    return this.http.get<DepartmentDto[]>(
      `${this.apiUrl}/api/reports/departments`
    );
  }

  /**
   * Get database statistics for debugging
   */
  getDatabaseStats(): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/api/reports/stats`
    );
  }

  // ========== Workflow v2 ==========

  submitActivationPlan(goalSetId: string, payload: SubmitActivationPlanRequestDto): Observable<any> {
    return this.http.post(
      `${this.workflowV2BaseUrl}/goal-sets/${goalSetId}/activation`,
      payload
    );
  }

  tlActivationDecision(evaluationId: number, payload: ActivationPlanDecisionDto): Observable<any> {
    return this.http.post(
      `${this.workflowV2BaseUrl}/evaluations/${evaluationId}/activation/decision`,
      payload
    );
  }

  submitSelfEvaluationV2(evaluationId: number, payload: SubmitSelfEvaluationV2Dto): Observable<any> {
    return this.http.post(
      `${this.workflowV2BaseUrl}/evaluations/${evaluationId}/self-evaluation`,
      payload
    );
  }

  hodFinalizeV2(evaluationId: number, comment?: string): Observable<any> {
    return this.http.post(
      `${this.workflowV2BaseUrl}/evaluations/${evaluationId}/hod/finalize`,
      { comment }
    );
  }

  getReviewWeights(): Observable<WorkflowReviewWeightDto[]> {
    return this.http.get<WorkflowReviewWeightDto[]>(
      `${this.workflowV2BaseUrl}/review-weights`
    );
  }

  updateReviewWeights(payload: UpdateWorkflowReviewWeightsDto): Observable<WorkflowReviewWeightDto[]> {
    return this.http.put<WorkflowReviewWeightDto[]>(
      `${this.workflowV2BaseUrl}/review-weights`,
      payload
    );
  }

  getPipCases(assignedHrUserId?: number, status?: string): Observable<PipCaseDto[]> {
    const query: string[] = [];
    if (assignedHrUserId !== undefined && assignedHrUserId !== null) {
      query.push(`assignedHrUserId=${encodeURIComponent(assignedHrUserId)}`);
    }
    if (status) {
      query.push(`status=${encodeURIComponent(status)}`);
    }
    const queryPart = query.length > 0 ? `?${query.join('&')}` : '';

    return this.http.get<PipCaseDto[]>(
      `${this.workflowV2BaseUrl}/pip-cases${queryPart}`
    );
  }

  updatePipCase(pipCaseId: number, payload: PipCaseUpdateDto): Observable<PipCaseDto> {
    return this.http.patch<PipCaseDto>(
      `${this.workflowV2BaseUrl}/pip-cases/${pipCaseId}`,
      payload
    );
  }

  addPipActionItem(pipCaseId: number, payload: PipActionItemCreateDto): Observable<PipCaseDto> {
    return this.http.post<PipCaseDto>(
      `${this.workflowV2BaseUrl}/pip-cases/${pipCaseId}/action-items`,
      payload
    );
  }

  updatePipActionItem(pipActionItemId: number, payload: PipActionItemUpdateDto): Observable<PipCaseDto> {
    return this.http.patch<PipCaseDto>(
      `${this.workflowV2BaseUrl}/pip-action-items/${pipActionItemId}`,
      payload
    );
  }
}
