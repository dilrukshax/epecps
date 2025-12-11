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
  employeeName: string;
  employeeEmail: string;
  reportingManagerId: number;
  reportingManagerName: string;
  teamLeadId: number;
  teamLeadName: string;
  status: string;
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
  personalGoalId?: string;  // Added: PersonalGoal GUID for linking
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
