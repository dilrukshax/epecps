// Dashboard Models

export interface DashboardStatsDto {
  // Approval Statistics
  pendingMyApproval: number;
  totalEvaluationsUnderReview: number;
  completedThisMonth: number;
  rejectedThisMonth: number;
  
  // Goal Statistics
  employeesWithPendingGoals: number;
  totalGoalsUnderReview: number;
  goalsApprovedThisMonth: number;
  goalsReturnedThisMonth: number;
  
  // Score Statistics
  averageScore: number;
  highPerformers: number; // Score >= 80
  lowPerformers: number; // Score < 50
  promotionCandidates: number; // Score >= 80
  
  // Role-Specific Statistics
  roleStats?: RoleSpecificStatsDto;
  
  // Trend Data (for charts)
  approvalTrend: TrendDataPointDto[];
  scoreDistribution: ScoreDistributionDto[];
  statusBreakdown: StatusBreakdownDto[];
}

export interface RoleSpecificStatsDto {
  role: string;
  
  // RM-specific
  directReports?: number;
  goalSetsAwaitingReview?: number;
  goalSetsApprovedThisWeek?: number;
  
  // TL-specific
  teamMembersCount?: number;
  peerAssignmentsPending?: number;
  evaluationsReadyForPeers?: number;
  
  // HOD-specific
  departmentSize?: number;
  promotionRecommendationsPending?: number;
  departmentAverageScore?: number;
  
  // GM-specific
  totalEmployees?: number;
  pendingPromotionDecisions?: number;
  promotionsApprovedThisQuarter?: number;
}

export interface TrendDataPointDto {
  label: string; // Date or period label
  value: number;
  category: string; // Approved, Rejected, Pending, etc.
}

export interface ScoreDistributionDto {
  range: string; // "0-50", "51-70", "71-80", "81-100"
  count: number;
  percentage: number;
}

export interface StatusBreakdownDto {
  status: string;
  count: number;
  percentage: number;
  color: string; // For chart colors
}

export interface LatestActivityDto {
  evaluationId: number;
  employeeName: string;
  employeeEmail: string;
  status: string;
  action: string;
  actorName: string;
  actorRole: string;
  comment?: string;
  timestamp: Date;
  requiresMyAction: boolean;
  overallScore?: number;
}

export interface DashboardDataDto {
  stats: DashboardStatsDto;
  latestActivities: LatestActivityDto[];
  recentApprovals: LatestActivityDto[];
  userRole: string;
  userRoles: string[];
}
