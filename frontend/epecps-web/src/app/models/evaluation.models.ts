// Evaluation Models

export interface AvailablePeerDto {
  userId: number;
  fullName: string;
  email: string;
  department?: string;
}

export interface SubmitGoalSetResponseDto {
  evaluationId: number;
  status: string;
  message: string;
}

export interface PendingApprovalDto {
  evaluationId: number;
  employeeId: number;
  employeeName: string;
  status: string;
  requiredRole: string;
  submittedDate?: Date;
  cycleId: number;
  cycleName: string;
}

export interface MyEvaluationDto {
  evaluationId: number;
  employeeId: number;
  employeeName: string;
  status: string;
  myRole: string;  // The role I'm playing in this evaluation (Employee, RM, TL, Peer, HOD, GM)
  submittedDate?: Date;
  completedDate?: Date;
  cycleId: number;
  cycleName: string;
  overallScore?: number;
}

export interface EvaluationDetailDto {
  evaluationId: number;
  cycleId: number;
  cycleName: string;
  employeeId: number;
  goalSetId?: string;
  employeeName: string;
  employeeEmail: string;
  reportingManagerId: number;
  reportingManagerName: string;
  teamLeadId: number;
  teamLeadName: string;
  status: string;
  workflowVersion?: string;
  overallScore?: number;
  reviews: ReviewDto[];
  goals: GoalDto[];
  approvalHistory: ApprovalHistoryItemDto[];
  peerAssignments: PeerAssignmentDto[];
  promotionCase?: PromotionCaseDto;
}

export interface ReviewDto {
  reviewId: number;
  reviewerUserId: number;
  reviewerName: string;
  reviewerRole: ReviewerRole;
  status: string;
  overallComment?: string;
  overallScore?: number;  // Added: overall score for the review
  submittedAt?: Date;
  items: ReviewItemDto[];
  scores?: ReviewScoreDto[];  // Added: scores collection
}

export interface ReviewScoreDto {
  id: number;
  evaluationId: number;
  reviewId: number;
  reviewerId: number;
  personalGoalId?: string;
  goalTitle?: string;
  scoreValue: number;
  comment?: string;
  createdAt: Date;
}

export interface ReviewItemDto {
  itemId: number;
  goalId?: number;
  goalTitle?: string;
  competencyId?: number;
  competencyName?: string;
  ratingValue: number;
  comment?: string;
}

export interface GoalDto {
  goalId: number;
  title: string;
  description: string;
  weightPct: number;
  evidenceUri?: string;
  goalAssignmentId?: string;
  activationMethod?: string;
  activationSubmittedAt?: Date;
  activationStatus?: string;
  activationTlComment?: string;
  activationReviewedAt?: Date;
  personalGoalId?: string;  // Added: PersonalGoal GUID for linking
  // Per-goal reviewer scores and average
  reviewerScores?: GoalReviewerScoreDto[];
  averageReviewScore?: number;
}

/**
 * A single reviewer's score for a specific goal
 */
export interface GoalReviewerScoreDto {
  reviewerId: number;
  reviewerName: string;
  reviewerRole: string;
  scoreValue: number;
  comment?: string;
  scoredAt?: Date;
}

export interface ApprovalHistoryItemDto {
  id: number;
  actorUserId: number;
  actorName: string;
  actorRole: string;
  action: string;
  comment?: string;
  fromStatus: string;
  toStatus: string;
  createdAt: Date;
}

export interface PeerAssignmentDto {
  peerAssignmentId: number;
  peerUserId: number;
  peerName: string;
}

export interface PromotionCaseDto {
  promotionCaseId: number;
  recommendedByHodId?: number;
  recommendedByHodName?: string;
  recommendedAt?: Date;
  gmDecision: PromotionDecision;
  gmDecidedById?: number;
  gmDecidedByName?: string;
  gmDecidedAt?: Date;
  decisionReason?: string;
}

// ReviewerRole enum - matches backend enum values (C# enum starts at 0)
// Backend: Self=0, Peer=1, TL=2, RM=3, HOD=4, GM=5
export enum ReviewerRole {
  Self = 0,
  Peer = 1,
  TL = 2,
  RM = 3,
  HOD = 4,
  GM = 5
}

export enum PromotionDecision {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected'
}

export interface ApprovalActionDto {
  comment?: string;
}

export interface AssignPeersDto {
  peerUserId1: number;
  peerUserId2: number;
}

export interface PromotionDecisionDto {
  approve: boolean;
  vacancyAvailable?: boolean;
  comment?: string;
}

// ====== NEW: Goal Action DTOs for RM-first flow ======

/**
 * Request DTO for starting a goal (after RM approval)
 */
export interface StartGoalRequestDto {
  // Empty - no additional data required to start a goal
}

/**
 * Request DTO for completing a goal
 */
export interface CompleteGoalRequestDto {
  evidenceUrl?: string;
  comment?: string;
  currentScore?: number;
}

/**
 * Response DTO for goal start/complete actions
 */
export interface GoalActionResponseDto {
  goalId: string;
  status: string;
  message: string;
  workflowContinued: boolean;
  evaluationId?: number;
  evaluationStatus?: string;
}

// ====== Review Scoring DTOs ======

/**
 * DTO for submitting RM item-level scores (score per goal)
 */
export interface SubmitRmScoringDto {
  itemScores: RmItemScoreDto[];
  overallComment?: string;
}

/**
 * Individual goal score for RM review
 */
export interface RmItemScoreDto {
  personalGoalId: string;
  scoreValue: number;  // 1-10
  comment?: string;
}

/**
 * DTO for submitting TL/HOD/GM overall score
 */
export interface SubmitOverallScoringDto {
  overallScore: number;  // 1-10
  comment?: string;
}

/**
 * DTO for any reviewer (TL/Peer/HOD) to submit per-goal scores.
 * Enables individual goal scoring for all reviewer roles.
 */
export interface SubmitReviewWithGoalScoresDto {
  goalScores: RmItemScoreDto[];  // reuses same per-goal score shape
  overallScore?: number;         // computed as average if not provided
  overallComment?: string;
}

/**
 * Response DTO after submitting scores
 */
export interface ReviewScoringResponseDto {
  reviewId: number;
  evaluationId: number;
  message: string;
  calculatedScore: number;
  evaluationStatus: string;
}

/**
 * Extended Goal DTO with RM-first flow status information
 */
export interface GoalDetailWithStatusDto extends GoalDto {
  status: string; // PersonalGoalStatus as string
  startedAt?: Date;
  completedAt?: Date;
  completionComment?: string;
  completionEvidenceUrl?: string;
  canStart: boolean;
  canComplete: boolean;
}

// ========== NEW: Bulk Approval DTOs ==========

/**
 * DTO for bulk approval candidates (evaluations eligible for bulk approval)
 */
export interface BulkApprovalCandidateDto {
  evaluationId: number;
  employeeId: number;
  employeeName: string;
  employeeEmail: string;
  status: string;
  overallScore?: number;
  scorePercentage: number; // Score as percentage (0-100)
  isEligibleForPromotion: boolean; // Score >= 80%
  cycleId: number;
  cycleName: string;
  lastReviewedAt?: Date;
  recommendedByHodName?: string;
  recommendedAt?: Date;
}

/**
 * Request DTO for bulk approval
 */
export interface BulkApprovalRequestDto {
  evaluationIds: number[];
  comment?: string;
}

/**
 * Response DTO for bulk approval operations
 */
export interface BulkApprovalResponseDto {
  totalRequested: number;
  successCount: number;
  failedCount: number;
  message: string;
  results: BulkApprovalResultItemDto[];
}

/**
 * Individual result item for bulk approval
 */
export interface BulkApprovalResultItemDto {
  evaluationId: number;
  employeeName: string;
  success: boolean;
  message: string;
  newStatus?: string;
}

/**
 * Summary stats for bulk approval dashboard
 */
export interface BulkApprovalStatsDto {
  pendingGmApproval: number;
  pendingHrProcessing: number;
  eligibleForPromotion: number; // Score >= 80%
  notEligibleForPromotion: number; // Score < 80%
  averageScore: number;
}

/**
 * DTO for HOD score submission
 */
export interface HodScoreSubmissionDto {
  score: number; // 1-10
  comment?: string;
}

/**
 * Response DTO for HOD score submission
 */
export interface HodScoreSubmissionResponseDto {
  message: string;
  score: number;
  scorePercentage: number;
}

// ========== Report Models ==========

export interface EvaluationReportFilterDto {
  cycleId?: number;
  departmentId?: number;
  status?: string;
  onlyPromoted?: boolean;
  minScore?: number;
  maxScore?: number;
  startDate?: string;
  endDate?: string;
}

export interface EvaluationReportDataDto {
  evaluationId: number;
  employeeName: string;
  employeeEmail: string;
  department: string;
  cycleName: string;
  status: string;
  overallScore?: number;
  reportingManagerName?: string;
  teamLeadName?: string;
  isPromoted: boolean;
  promotionStatus?: string;
  submittedDate?: string;
  completedDate?: string;
  rmScore?: number;
  tlScore?: number;
  peerScore1?: number;
  peerScore2?: number;
  hodScore?: number;
  gmScore?: number;
}

export interface CycleDto {
  cycleId: number;
  name: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface DepartmentDto {
  deptId: number;
  name: string;
}

// ========== Workflow v2 ==========

export interface GoalActivationMethodDto {
  goalAssignmentId: string;
  method: string;
}

export interface SubmitActivationPlanRequestDto {
  goals: GoalActivationMethodDto[];
}

export interface ActivationPlanDecisionDto {
  approved: boolean;
  comment?: string;
  rejectedGoalAssignmentIds: string[];
}

export interface SubmitSelfEvaluationV2Dto {
  overallComment?: string;
  goals: SelfEvaluationGoalInputDto[];
}

export interface SelfEvaluationGoalInputDto {
  personalGoalId: string;
  score: number;
  summary: string;
  evidenceUrl: string;
  comment?: string;
}

export interface GmV2DecisionDto {
  approve: boolean;
  vacancyAvailable: boolean;
  comment?: string;
}

export interface WorkflowReviewWeightDto {
  reviewerKey: string;
  weightPercent: number;
}

export interface UpdateWorkflowReviewWeightsDto {
  weights: WorkflowReviewWeightDto[];
}

export interface PipActionItemDto {
  pipActionItemId: number;
  title: string;
  description?: string;
  trainingMaterialId?: number;
  externalTrainingLink?: string;
  dueDate?: Date;
  status: string;
  createdAt: Date;
  completedAt?: Date;
}

export interface PipCaseDto {
  pipCaseId: number;
  evaluationId: number;
  employeeUserId: number;
  employeeName: string;
  assignedHrUserId: number;
  assignedHrName: string;
  status: string;
  reason?: string;
  createdAt: Date;
  dueDate?: Date;
  closedAt?: Date;
  actionItems: PipActionItemDto[];
}

export interface PipCaseUpdateDto {
  status?: string;
  dueDate?: string;
  reason?: string;
}

export interface PipActionItemCreateDto {
  title: string;
  description?: string;
  trainingMaterialId?: number;
  externalTrainingLink?: string;
  dueDate?: string;
}

export interface PipActionItemUpdateDto {
  status?: string;
  dueDate?: string;
}
