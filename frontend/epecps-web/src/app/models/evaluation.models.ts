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
  submittedAt?: Date;
  items: ReviewItemDto[];
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

export enum ReviewerRole {
  Self = 'Self',
  Peer = 'Peer',
  TL = 'TL',
  RM = 'RM',
  HOD = 'HOD',
  GM = 'GM'
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
