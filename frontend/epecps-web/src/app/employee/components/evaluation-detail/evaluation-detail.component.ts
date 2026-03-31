import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EvaluationService } from '../../../services/evaluation.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  EvaluationDetailDto,
  ReviewDto,
  ReviewScoreDto,
  ApprovalHistoryItemDto,
  ReviewerRole,
  GoalDto,
  CompleteGoalRequestDto,
  SubmitRmScoringDto,
  RmItemScoreDto,
  SubmitOverallScoringDto,
  SubmitReviewWithGoalScoresDto,
  SubmitActivationPlanRequestDto,
  ActivationPlanDecisionDto,
  SubmitSelfEvaluationV2Dto
} from '../../../models/evaluation.models';

@Component({
  selector: 'app-evaluation-detail',
  templateUrl: './evaluation-detail.component.html',
  styleUrls: ['./evaluation-detail.component.css'],
  standalone: false
})
export class EvaluationDetailComponent implements OnInit {
  evaluationId!: number;
  evaluation: EvaluationDetailDto | null = null;
  loading = false;
  error: string | null = null;
  
  // Action state
  approving = false;
  rejecting = false;
  comment = '';
  showCommentBox = false;
  actionType: 'approve' | 'reject' | 'assign-peers' | null = null;

  // Workflow v2 activation state
  activationMethods: { [goalAssignmentId: string]: string } = {};
  submittingActivationPlan = false;
  processingActivationDecision = false;
  activationDecisionComment = '';

  // Workflow v2 self-evaluation state
  submittingSelfEvaluation = false;
  loadingSelfEvaluationPeers = false;
  selfEvaluationScore = 70;
  selfEvaluationComment = '';
  selfEvaluationPeerUserId1: number | null = null;
  selfEvaluationPeerUserId2: number | null = null;

  // Peer assignment state
  showPeerAssignment = false;
  assigningPeers = false;
  loadingPeers = false;
  peerUserId1: number | null = null;
  peerUserId2: number | null = null;
  availablePeers: Array<{ userId: number; name: string }> = [];

  // TL Combined Review State (Score + Peer Assignment in one action)
  tlCombinedReviewMode = false;
  submittingTlCombinedReview = false;

  // HOD action state
  showHodActions = false;
  recommendingPromotion = false;
  hodRejectingEval = false;
  hodComment = '';

  // GM action state
  showGmActions = false;
  processingGmDecision = false;
  gmComment = '';
  gmApproveDecision = true;
  gmVacancyAvailable = true;
  gmVacancySelection: '' | 'yes' | 'no' = '';

  // HR action state
  showHrActions = false;
  processingHrAction = false;
  hrComment = '';
  hrProceedDecision = true;

  // Current user state
  currentUserId: number | null = null;
  isActiveApprover = false;
  currentUserRole = '';
  needsPeerAssignment = false;
  userRoles: string[] = [];

  // ====== Employee Goal Actions State ======
  isGoalOwner = false;
  canPerformGoalActions = false;
  startingGoalId: string | null = null;
  completingGoalId: string | null = null;
  
  // Complete goal modal state
  showCompleteGoalModal = false;
  completeModalGoal: GoalDto | null = null;
  completeFormEvidenceUrl = '';
  completeFormComment = '';
  completeFormCurrentScore: number | null = null;

  // ====== Scoring State ======
  submittingScores = false;
  pendingReviewId: number | null = null;
  
  // RM scoring - item-level scores per goal
  rmGoalScores: { [goalId: string]: { score: number; comment: string } } = {};
  rmOverallComment = '';
  
  // Overall scoring for TL/HOD/GM/Peer
  overallScore: number = 5;
  overallComment = '';

  // Per-goal scoring mode for TL/Peer/HOD (reuses rmGoalScores map)
  useGoalLevelScoring = true;

  ReviewerRole = ReviewerRole;

  // Track if current user has a pending scoring requirement
  hasPendingScoringRequirement = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private evaluationService: EvaluationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.evaluationId = +params['id'];
      this.loadEvaluation();
    });
  }

  loadEvaluation(): void {
    this.loading = true;
    this.error = null;

    this.evaluationService.getEvaluationDetail(this.evaluationId).subscribe({
      next: (evaluation) => {
        this.evaluation = evaluation;
        this.resolveCurrentUserId();
        this.determineGoalOwnership();
        this.determineUserRole();
        this.initializeActivationState();
        this.checkForPendingReview();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load evaluation details. Please try again.';
        this.loading = false;
        console.error('Error loading evaluation:', err);
      }
    });
  }

  /**
   * Resolve the current user's ID by matching their email with known users in the evaluation
   */
  resolveCurrentUserId(): void {
    if (!this.evaluation) return;

    const currentUserEmail = this.authService.getCurrentUser()?.email?.toLowerCase() || '';
    const emailPrefix = currentUserEmail.split('@')[0];
    
    console.log('=== resolveCurrentUserId ===');
    console.log('Current user email:', currentUserEmail);
    console.log('Email prefix:', emailPrefix);

    // Check if current user is the employee
    if (this.evaluation.employeeEmail?.toLowerCase() === currentUserEmail) {
      this.currentUserId = this.evaluation.employeeId;
      console.log('Current user is Employee, userId:', this.currentUserId);
      return;
    }

    // Check all reviews to find a match - collect all matching user IDs
    const matchingUserIds: number[] = [];
    
    for (const review of this.evaluation.reviews) {
      const reviewerName = review.reviewerName.toLowerCase();
      
      // Check various matching patterns
      const nameMatches = 
        reviewerName.includes(emailPrefix) || 
        reviewerName === currentUserEmail ||
        emailPrefix.includes(reviewerName.split(' ')[0]?.toLowerCase() || 'NOMATCH');
      
      if (nameMatches && !matchingUserIds.includes(review.reviewerUserId)) {
        matchingUserIds.push(review.reviewerUserId);
        console.log('Found matching reviewer:', review.reviewerName, 'userId:', review.reviewerUserId);
      }
    }

    // If we found matching user IDs, use the first one (they should all be the same for the same user)
    if (matchingUserIds.length > 0) {
      this.currentUserId = matchingUserIds[0];
      console.log('Resolved currentUserId from reviews:', this.currentUserId);
      return;
    }

    // Fallback: try to match with RM, TL by checking if their name matches
    const rmName = this.evaluation.reportingManagerName?.toLowerCase() || '';
    const tlName = this.evaluation.teamLeadName?.toLowerCase() || '';

    if (rmName.includes(emailPrefix) || emailPrefix.includes(rmName.split(' ')[0] || 'NOMATCH')) {
      this.currentUserId = this.evaluation.reportingManagerId;
      console.log('Current user is RM, userId:', this.currentUserId);
      return;
    }

    if (tlName.includes(emailPrefix) || emailPrefix.includes(tlName.split(' ')[0] || 'NOMATCH')) {
      this.currentUserId = this.evaluation.teamLeadId;
      console.log('Current user is TL, userId:', this.currentUserId);
      return;
    }

    console.log('Could not resolve currentUserId from evaluation data');
  }

  /**
   * Check if current user has a pending review that needs scoring
   */
  checkForPendingReview(): void {
    if (!this.evaluation) return;

    const evaluationStatus = this.evaluation.status.toLowerCase();

    console.log('=== checkForPendingReview ===');
    console.log('Current userId:', this.currentUserId);
    console.log('Evaluation status:', this.evaluation.status);
    console.log('Available reviews:', this.evaluation.reviews.map(r => ({
      reviewId: r.reviewId,
      reviewerUserId: r.reviewerUserId,
      reviewerName: r.reviewerName,
      reviewerRole: r.reviewerRole,
      status: r.status
    })));
    
    // Find ALL pending reviews for current user
    const allPendingReviewsForCurrentUser = this.evaluation.reviews.filter(r => {
      const reviewStatus = r.status.toLowerCase();
      const isCurrentUserReview = this.currentUserId !== null && r.reviewerUserId === this.currentUserId;
      return reviewStatus === 'pending' && isCurrentUserReview;
    });

    console.log('All pending reviews for current user:', allPendingReviewsForCurrentUser.map(r => ({
      reviewId: r.reviewId,
      reviewerRole: r.reviewerRole,
      status: r.status
    })));

    // In workflow-v2, HOD finalization is a dedicated action and does not require
    // manual per-goal scoring in the UI.
    if (this.isWorkflowV2() && this.evaluation.status.toLowerCase().includes('v2_pending_hod_review')) {
      this.hasPendingScoringRequirement = false;
      this.pendingReviewId = null;
      return;
    }

    // Find the first pending review for current user that requires action
    const pendingReview = allPendingReviewsForCurrentUser.find((r) => {
      // Skip scoring for RM's FIRST approval (Pending_RM_Review)
      if (r.reviewerRole === ReviewerRole.RM) {
        const isFirstRmReview = evaluationStatus === 'pending_rm_review';
        console.log(`Review ${r.reviewId}: RM review, isFirstRmReview=${isFirstRmReview}`);
        
        if (isFirstRmReview) {
          console.log(`Review ${r.reviewId}: Skipping RM scoring for first approval stage`);
          return false;
        }
        
        console.log(`Review ${r.reviewId}: RM post-completion scoring REQUIRED`);
        return true;
      }
      
      // For peer reviews, we want the first pending one
      if (r.reviewerRole === ReviewerRole.Peer) {
        const allPeerReviewsForUser = this.evaluation!.reviews
          .filter(rev => rev.reviewerRole === ReviewerRole.Peer && rev.reviewerUserId === this.currentUserId)
          .sort((a, b) => a.reviewId - b.reviewId);
        
        const pendingPeerReviews = allPeerReviewsForUser.filter(rev => rev.status.toLowerCase() === 'pending');
        
        console.log(`Review ${r.reviewId}: Peer review check, pending count=${pendingPeerReviews.length}`);
        
        if (pendingPeerReviews.length > 0) {
          const isFirstPending = r.reviewId === pendingPeerReviews[0].reviewId;
          console.log(`Review ${r.reviewId}: isFirstPending=${isFirstPending}`);
          return isFirstPending;
        }
        return false;
      }
      
      console.log(`Review ${r.reviewId}: Scoring REQUIRED for role ${r.reviewerRole}`);
      return true;
    });

    console.log('Found pending review for scoring:', pendingReview);

    if (pendingReview) {
      this.pendingReviewId = pendingReview.reviewId;
      this.hasPendingScoringRequirement = true;
      console.log(`✅ Scoring requirement: pendingReviewId=${this.pendingReviewId}, role=${pendingReview.reviewerRole}`);
      this.initializeScoringForReview(pendingReview);
    } else {
      this.hasPendingScoringRequirement = false;
      this.pendingReviewId = null;
      console.log('❌ No pending scoring requirement found');
    }
  }

  /**
   * Initialize scoring based on reviewer role and load existing scores if available
   */
  initializeScoringForReview(review: ReviewDto): void {
    const role = review.reviewerRole;
    
    // RM (role=3) does item-level scoring
    if (role === ReviewerRole.RM) {
      this.initializeRmScoring();
    }
    // TL(2), Peer(1), HOD(4), GM(5) - initialize per-goal scores AND overall
    else {
      // Initialize per-goal scores for goal-level scoring mode
      this.initializeGoalLevelScoring(review);

      if (review.overallScore !== null && review.overallScore !== undefined) {
        this.overallScore = review.overallScore;
      } else {
        this.overallScore = 5;
      }
      this.overallComment = review.overallComment || '';
    }
  }

  /**
   * Initialize RM scoring with default or existing values for each goal
   */
  initializeRmScoring(): void {
    if (!this.evaluation) return;
    
    this.rmGoalScores = {};
    
    // Start fresh with no pre-selected score (0 = nothing selected)
    this.evaluation.goals.forEach(goal => {
      const goalKey = goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
      this.rmGoalScores[goalKey] = { score: 0, comment: '' };
    });
    
    this.rmOverallComment = '';
  }

  /**
   * Initialize per-goal scoring for non-RM reviewers (TL/Peer/HOD).
   * Reuses rmGoalScores map so the same UI buttons work.
   */
  initializeGoalLevelScoring(review: ReviewDto): void {
    if (!this.evaluation) return;

    this.rmGoalScores = {};

    // Start fresh with no pre-selected score (0 = nothing selected)
    this.evaluation.goals.forEach(goal => {
      const goalKey = goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
      this.rmGoalScores[goalKey] = { score: 0, comment: '' };
    });

    this.rmOverallComment = '';
  }

  /**
   * Submit RM item-level scores
   */
  submitRmScores(): void {
    if (!this.evaluation || !this.pendingReviewId) return;

    const itemScores: RmItemScoreDto[] = Object.entries(this.rmGoalScores).map(([goalId, data]) => ({
      personalGoalId: goalId,
      scoreValue: data.score,
      comment: data.comment || undefined
    }));

    const dto: SubmitRmScoringDto = {
      itemScores,
      overallComment: this.rmOverallComment || undefined
    };

    this.submittingScores = true;

    this.evaluationService.submitRmScoring(this.evaluationId, this.pendingReviewId, dto).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Scores submitted successfully!');
        this.submittingScores = false;
        this.loadEvaluation();
      },
      error: (err) => {
        this.submittingScores = false;
        const errorMessage = err.error?.error || 'Failed to submit scores. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error submitting RM scores:', err);
      }
    });
  }

  /**
   * Submit overall score for TL/HOD/GM/Peer
   */
  submitOverallScore(): void {
    if (!this.evaluation || !this.pendingReviewId) return;

    const dto: SubmitOverallScoringDto = {
      overallScore: this.overallScore,
      comment: this.overallComment || undefined
    };

    this.submittingScores = true;

    this.evaluationService.submitOverallScoring(this.evaluationId, this.pendingReviewId, dto).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Score submitted successfully!');
        this.submittingScores = false;
        this.loadEvaluation();
      },
      error: (err) => {
        this.submittingScores = false;
        const errorMessage = err.error?.error || 'Failed to submit score. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error submitting overall score:', err);
      }
    });
  }

  /**
   * Get which peer review number the user is on
   */
  getPeerReviewNumber(): string {
    if (!this.evaluation || this.currentUserRole !== 'Peer' || this.currentUserId === null) return '';
    
    const allPeerReviewsByCurrentUser = this.evaluation.reviews
      .filter(r => r.reviewerRole === ReviewerRole.Peer && r.reviewerUserId === this.currentUserId)
      .sort((a, b) => a.reviewId - b.reviewId);
    
    const completedCount = allPeerReviewsByCurrentUser.filter(r => 
      r.status.toLowerCase() === 'approved' || r.status.toLowerCase() === 'completed'
    ).length;
    
    const totalCount = allPeerReviewsByCurrentUser.length;
    
    return `Peer Review ${completedCount + 1} of ${totalCount}`;
  }

  /**
   * Check if review requires item-level scoring (RM only)
   */
  isItemLevelScoring(role: number): boolean {
    return role === ReviewerRole.RM;
  }

  /**
   * Check if there are any scores to display in Reviews & Ratings section
   */
  hasAnyScores(): boolean {
    if (!this.evaluation || !this.evaluation.reviews) return false;
    
    // Check reviews for scores
    const hasReviewScores = this.evaluation.reviews.some(review => {
      const isCompleted = review.status.toLowerCase() === 'completed' || review.status.toLowerCase() === 'approved';
      const hasItemScores = review.scores && review.scores.length > 0;
      const hasOverallScore = review.overallScore !== null && review.overallScore !== undefined;
      return isCompleted && (hasItemScores || hasOverallScore);
    });
    if (hasReviewScores) return true;

    // Also check per-goal reviewerScores on goals themselves
    return this.evaluation.goals?.some(g => g.reviewerScores && g.reviewerScores.length > 0) ?? false;
  }

  determineUserRole(): void {
    if (!this.evaluation) return;

    const status = this.evaluation.status.toLowerCase();
    
    console.log('=== determineUserRole ===');
    console.log('Evaluation status:', this.evaluation.status);
    console.log('Current userId:', this.currentUserId);
    
    this.userRoles = this.getUserRolesFromStatus(status);
    
    // Reset all action flags
    this.isActiveApprover = false;
    this.needsPeerAssignment = false;
    this.showGmActions = false;
    this.showHrActions = false;
    this.tlCombinedReviewMode = false;
    this.gmVacancySelection = '';

    if (status.includes('v2_pending_employee_activation') || status.includes('v2_returned_for_activation')) {
      if (this.isGoalOwner) {
        this.isActiveApprover = true;
        this.currentUserRole = 'Employee';
      }
      return;
    }

    if (status.includes('v2_pending_tl_activation_review')) {
      if (this.currentUserHasRole('TL')) {
        this.isActiveApprover = true;
        this.currentUserRole = 'TL';
      }
      return;
    }

    if (status.includes('v2_active_goals')) {
      if (this.isGoalOwner) {
        this.isActiveApprover = true;
        this.currentUserRole = 'Employee';
        this.loadSelfEvaluationPeers();
      }
      return;
    }
    
    // Check if peer assignment is needed (after TL has already approved)
    if (status.includes('pending_peer_assignment')) {
      if (this.currentUserHasRole('TL')) {
        this.needsPeerAssignment = true;
        this.isActiveApprover = false;
        this.currentUserRole = 'TL';
        console.log('User role: TL (peer assignment stage)');
        this.loadAvailablePeers();
      }
      return;
    }

    // Determine active approver role
    if (status.includes('pending_hod')) {
      if (this.currentUserHasRole('HOD')) {
        this.isActiveApprover = true;
        this.showGmActions = false;
        this.currentUserRole = 'HOD';
        console.log('User role: HOD');
      }
    } else if (status.includes('pending_gm')) {
      if (this.currentUserHasRole('GM')) {
        this.isActiveApprover = false;
        this.showGmActions = true;
        this.currentUserRole = 'GM';
        console.log('User role: GM');
      }
    } else if (status.includes('pending_hr')) {
      if (this.currentUserHasRole('HR')) {
        this.isActiveApprover = false;
        this.showHrActions = true;
        this.currentUserRole = 'HR';
        console.log('User role: HR');
      }
    } else if (status.includes('pending_rm_review_postcompletion') || 
               status === 'pending_rm_review_postcompletion') {
      if (this.currentUserHasRole('RM')) {
        this.isActiveApprover = true;
        this.currentUserRole = 'RM';
        console.log('User role: RM (post-completion - scoring required)');
        this.initializeRmScoring();
      }
    } else if (status.includes('pending_rm')) {
      if (this.currentUserHasRole('RM')) {
        this.isActiveApprover = true;
        this.currentUserRole = 'RM';
        console.log('User role: RM (first approval - no scoring)');
      }
    } else if (status.includes('pending_tl')) {
      // TL Review stage - enable combined mode (score + peer assignment)
      if (this.currentUserHasRole('TL')) {
        this.isActiveApprover = true;
        this.currentUserRole = 'TL';
        this.tlCombinedReviewMode = true;
        console.log('User role: TL (combined review mode - score + peer assignment)');
        // Load available peers for the combined form
        this.loadAvailablePeers();
      }
    } else if (status.includes('pending_peer')) {
      // For peer reviews, check if current user has ANY pending review using userId
      console.log('Checking for pending peer reviews...');
      console.log('Current userId:', this.currentUserId);
      
      if (this.currentUserHasRole('Peer') && this.currentUserId !== null) {
        // Get all peer reviews for the current user
        const allPeerReviewsForUser = this.evaluation.reviews.filter(r => 
          r.reviewerRole === ReviewerRole.Peer && r.reviewerUserId === this.currentUserId
        );
        
        console.log('All peer reviews for current user:', allPeerReviewsForUser.map(r => ({
          reviewId: r.reviewId,
          status: r.status,
          reviewerUserId: r.reviewerUserId
        })));
        
        // Check if there are any pending peer reviews for current user
        const pendingPeerReviews = allPeerReviewsForUser.filter(r => 
          r.status.toLowerCase() === 'pending'
        );
        
        console.log('Pending peer reviews for current user:', pendingPeerReviews.length);
        
        if (pendingPeerReviews.length > 0) {
          this.isActiveApprover = true;
          this.currentUserRole = 'Peer';
          console.log('✅ User role: Peer (found', pendingPeerReviews.length, 'pending peer review(s))');
          console.log('First pending review ID:', pendingPeerReviews[0].reviewId);
        } else {
          console.log('❌ No pending peer reviews found for current user');
        }
      } else {
        console.log('❌ Cannot check peer reviews - currentUserId is null');
      }
    }

    console.log('Final: isActiveApprover=', this.isActiveApprover, ', currentUserRole=', this.currentUserRole, ', tlCombinedReviewMode=', this.tlCombinedReviewMode);
  }

  private initializeActivationState(): void {
    if (!this.evaluation) return;

    this.activationMethods = {};
    this.evaluation.goals.forEach(goal => {
      if (!goal.goalAssignmentId) {
        return;
      }

      this.activationMethods[goal.goalAssignmentId] = goal.activationMethod || '';
    });
  }

  isEmployeeActivationStage(): boolean {
    if (!this.evaluation) return false;
    const status = this.evaluation.status.toLowerCase();
    return this.isGoalOwner &&
      (status.includes('v2_pending_employee_activation') || status.includes('v2_returned_for_activation'));
  }

  isTlActivationStage(): boolean {
    if (!this.evaluation) return false;
    return this.currentUserRole === 'TL' && this.evaluation.status.toLowerCase().includes('v2_pending_tl_activation_review');
  }

  isWorkflowV2(): boolean {
    if (!this.evaluation) return false;
    return (this.evaluation.workflowVersion || '').toLowerCase() === 'v2'
      || this.evaluation.status.toLowerCase().startsWith('v2_');
  }

  isEmployeeSelfEvaluationStage(): boolean {
    if (!this.evaluation) return false;
    return this.isGoalOwner && this.isWorkflowV2() && this.evaluation.status.toLowerCase().includes('v2_active_goals');
  }

  isV2HodFinalizeStage(): boolean {
    if (!this.evaluation) return false;
    return this.currentUserRole === 'HOD'
      && this.isWorkflowV2()
      && this.evaluation.status.toLowerCase().includes('v2_pending_hod_review');
  }

  private loadSelfEvaluationPeers(): void {
    if (!this.isEmployeeSelfEvaluationStage()) {
      return;
    }

    if (this.availablePeers.length > 0 || this.loadingSelfEvaluationPeers) {
      return;
    }

    this.loadingSelfEvaluationPeers = true;
    this.evaluationService.getAvailablePeers(this.evaluationId).subscribe({
      next: (peers) => {
        this.availablePeers = peers.map(p => ({
          userId: p.userId,
          name: `${p.fullName}${p.department ? ` (${p.department})` : ''}`
        }));
        this.loadingSelfEvaluationPeers = false;
      },
      error: (err) => {
        this.loadingSelfEvaluationPeers = false;
        const errorMessage = err.error?.message || err.message || 'Failed to load available peer options.';
        this.showToast('error', errorMessage);
      }
    });
  }

  getSelfEvaluationPeerOptions(): Array<{ userId: number; name: string }> {
    if (!this.evaluation) return [];

    const excluded = new Set<number>([
      this.evaluation.employeeId,
      this.evaluation.reportingManagerId,
      this.evaluation.teamLeadId
    ]);

    return this.availablePeers.filter(peer => !excluded.has(peer.userId));
  }

  canSubmitSelfEvaluationV2(): boolean {
    if (!this.evaluation || !this.isEmployeeSelfEvaluationStage()) return false;
    if (this.selfEvaluationScore < 0 || this.selfEvaluationScore > 100) return false;
    if (!this.selfEvaluationPeerUserId1 || !this.selfEvaluationPeerUserId2) return false;
    if (this.selfEvaluationPeerUserId1 === this.selfEvaluationPeerUserId2) return false;
    return true;
  }

  submitSelfEvaluationV2(): void {
    if (!this.evaluation || this.submittingSelfEvaluation) return;

    if (!this.canSubmitSelfEvaluationV2()) {
      this.showToast('error', 'Please provide score and two different peer reviewers before submitting.');
      return;
    }

    const payload: SubmitSelfEvaluationV2Dto = {
      selfScore: this.selfEvaluationScore,
      comment: this.selfEvaluationComment || undefined,
      peerUserId1: this.selfEvaluationPeerUserId1!,
      peerUserId2: this.selfEvaluationPeerUserId2!
    };

    this.submittingSelfEvaluation = true;
    this.error = null;

    this.evaluationService.submitSelfEvaluationV2(this.evaluationId, payload).subscribe({
      next: () => {
        this.submittingSelfEvaluation = false;
        this.showToast('success', 'Self-evaluation submitted. TL, RM, and peer reviews are now in progress.');
        this.loadEvaluation();
      },
      error: (err) => {
        this.submittingSelfEvaluation = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to submit self-evaluation.';
        this.showToast('error', errorMessage);
      }
    });
  }

  getActivationMethod(goal: GoalDto): string {
    if (!goal.goalAssignmentId) return '';
    return this.activationMethods[goal.goalAssignmentId] || '';
  }

  setActivationMethod(goal: GoalDto, value: string): void {
    if (!goal.goalAssignmentId) return;
    this.activationMethods[goal.goalAssignmentId] = value;
  }

  canSubmitActivationPlan(): boolean {
    if (!this.evaluation || !this.evaluation.goalSetId) return false;
    const activationGoals = this.evaluation.goals.filter(g => !!g.goalAssignmentId);
    if (activationGoals.length < 5) return false;

    return activationGoals.every(goal => {
      const method = this.getActivationMethod(goal).trim();
      return method.length > 0;
    });
  }

  submitActivationPlan(): void {
    if (!this.evaluation || !this.evaluation.goalSetId || this.submittingActivationPlan) return;

    const goals = this.evaluation.goals
      .filter(g => !!g.goalAssignmentId)
      .map(g => ({
        goalAssignmentId: g.goalAssignmentId!,
        method: this.getActivationMethod(g).trim()
      }));

    const payload: SubmitActivationPlanRequestDto = { goals };

    this.submittingActivationPlan = true;
    this.error = null;

    this.evaluationService.submitActivationPlan(this.evaluation.goalSetId, payload).subscribe({
      next: () => {
        this.submittingActivationPlan = false;
        this.showToast('success', 'Activation plan submitted successfully.');
        this.loadEvaluation();
      },
      error: (err) => {
        this.submittingActivationPlan = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to submit activation plan.';
        this.showToast('error', errorMessage);
      }
    });
  }

  processActivationDecision(approved: boolean): void {
    if (!this.evaluation || this.processingActivationDecision) return;

    if (!approved && !this.activationDecisionComment.trim()) {
      this.showToast('error', 'Comment is required when returning activation plan.');
      return;
    }

    const rejectedGoalIds = approved
      ? []
      : this.evaluation.goals
          .filter(g => !!g.goalAssignmentId)
          .map(g => g.goalAssignmentId!);

    const payload: ActivationPlanDecisionDto = {
      approved,
      comment: this.activationDecisionComment || undefined,
      rejectedGoalAssignmentIds: rejectedGoalIds
    };

    this.processingActivationDecision = true;
    this.error = null;

    this.evaluationService.tlActivationDecision(this.evaluationId, payload).subscribe({
      next: () => {
        this.processingActivationDecision = false;
        this.activationDecisionComment = '';
        this.showToast('success', approved ? 'Activation plan approved.' : 'Activation plan returned for rework.');
        this.loadEvaluation();
      },
      error: (err) => {
        this.processingActivationDecision = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to process activation decision.';
        this.showToast('error', errorMessage);
      }
    });
  }

  getUserRolesFromStatus(status: string): string[] {
    const roles: string[] = ['Employee'];
    if (status.includes('hod')) roles.push('HOD');
    if (status.includes('gm')) roles.push('GM');
    if (status.includes('hr')) roles.push('HR');
    return roles;
  }

  private currentUserHasRole(role: string): boolean {
    const currentRoles = (this.authService.getCurrentUser()?.roles || []).map(r => r.toUpperCase());
    return currentRoles.includes(role.toUpperCase()) || currentRoles.includes('SUPERADMIN');
  }

  loadAvailablePeers(): void {
    this.loadingPeers = true;
    console.log('Loading available peers for evaluation:', this.evaluationId);
    
    this.evaluationService.getAvailablePeers(this.evaluationId).subscribe({
      next: (peers) => {
        console.log('Available peers loaded:', peers);
        this.availablePeers = peers.map(p => ({
          userId: p.userId,
          name: `${p.fullName}${p.department ? ` (${p.department})` : ''}`
        }));
        this.loadingPeers = false;
      },
      error: (err) => {
        console.error('Error loading peers:', err);
        this.loadingPeers = false;
        const errorMessage = err.error?.message || err.message || 'Failed to load available peers';
        this.showToast('error', `Failed to load available peers: ${errorMessage}`);
        this.availablePeers = [];
      }
    });
  }

  initiateAction(type: 'approve' | 'reject'): void {
    this.actionType = type;
    this.showCommentBox = true;
    this.comment = '';
  }

  initiatePeerAssignment(): void {
    this.showPeerAssignment = true;
    this.peerUserId1 = null;
    this.peerUserId2 = null;
  }

  cancelAction(): void {
    this.showCommentBox = false;
    this.actionType = null;
    this.comment = '';
  }

  cancelPeerAssignment(): void {
    this.showPeerAssignment = false;
    this.peerUserId1 = null;
    this.peerUserId2 = null;
  }

  confirmAction(): void {
    if (this.actionType === 'approve') {
      this.approveEvaluation();
    } else if (this.actionType === 'reject') {
      this.rejectEvaluation();
    }
  }

  confirmPeerAssignment(): void {
    if (!this.peerUserId1 || !this.peerUserId2) {
      this.showToast('error', 'Please select both peer reviewers');
      return;
    }

    this.assigningPeers = true;

    this.evaluationService.assignPeerReviewers(this.evaluationId, this.peerUserId1, this.peerUserId2).subscribe({
      next: () => {
        this.showToast('success', 'Peer reviewers assigned successfully!');
        this.assigningPeers = false;
        this.showPeerAssignment = false;
        this.loadEvaluation();
      },
      error: (err) => {
        this.assigningPeers = false;
        const errorMessage = err.error?.message || 'Failed to assign peer reviewers. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error assigning peers:', err);
      }
    });
  }

  approveEvaluation(): void {
    if (this.approving || !this.evaluation) return;

    if (this.isV2HodFinalizeStage()) {
      this.finalizeHodV2();
      return;
    }

    // Check if user has pending scoring requirement that hasn't been fulfilled
    if (this.hasPendingScoringRequirement && this.pendingReviewId) {
      if (this.currentUserRole === 'RM') {
        const hasScores = this.hasAllScoresSelected();
        
        if (hasScores) {
          this.submitScoresAndApprove();
          return;
        } else {
          this.showToast('error', 'Please score all goals before approving.');
          return;
        }
      }
      else if ((this.currentUserRole === 'TL' || this.currentUserRole === 'Peer' || this.currentUserRole === 'HOD')) {
        if (this.useGoalLevelScoring) {
          if (this.hasAllScoresSelected()) {
            this.submitScoresAndApprove();
            return;
          } else {
            this.showToast('error', 'Please score all goals before approving.');
            return;
          }
        } else if (this.overallScore) {
          this.submitScoresAndApprove();
          return;
        }
      }
    }

    // For HOD, check if they want to recommend for promotion
    if (this.currentUserRole === 'HOD' && this.evaluation.overallScore && this.evaluation.overallScore > 80) {
      if (confirm('This employee has scored above 80. Do you want to recommend them for promotion to GM?\n\nClick OK to recommend for promotion, or Cancel to just approve without promotion recommendation.')) {
        this.recommendPromotionToGm();
        return;
      }
    }

    this.proceedWithApproval();
  }

  /**
   * Combined method to submit scores and approve in one flow
   */
  private submitScoresAndApprove(): void {
    if (!this.evaluation || !this.pendingReviewId) return;

    this.approving = true;
    this.error = null;

    if (this.currentUserRole === 'RM') {
      const itemScores: RmItemScoreDto[] = [];
      
      for (const goal of this.evaluation.goals) {
        const goalKey = goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
        const scoreData = this.rmGoalScores[goalKey];
        
        console.log(`Processing goal "${goal.title}" (key: ${goalKey}):`, scoreData);
        
        if (scoreData && scoreData.score > 0) {
          if (goal.personalGoalId) {
            itemScores.push({
              personalGoalId: goal.personalGoalId.toString(),
              scoreValue: scoreData.score,
              comment: scoreData.comment || undefined
            });
          } else {
            console.warn(`Goal "${goal.title}" is missing personalGoalId. Skipping.`);
          }
        }
      }

      if (itemScores.length === 0) {
        this.approving = false;
        this.showToast('error', 'No valid goal scores to submit. Please score all goals.');
        return;
      }

      const scoreDto: SubmitRmScoringDto = {
        itemScores,
        overallComment: this.rmOverallComment || this.comment || undefined
      };

      console.log('Submitting RM scores:', JSON.stringify(scoreDto, null, 2));

      this.evaluationService.submitRmScoring(this.evaluationId, this.pendingReviewId, scoreDto).subscribe({
        next: (scoreResponse) => {
          console.log('RM Scores submitted successfully:', scoreResponse);
          this.proceedWithApproval();
        },
        error: (err) => {
          this.approving = false;
          const errorMessage = err.error?.error || err.error?.message || 'Failed to submit scores. Please try again.';
          this.showToast('error', errorMessage);
          console.error('Error submitting RM scores:', err);
        }
      });
    }
    else if (this.currentUserRole === 'TL' || this.currentUserRole === 'Peer' || this.currentUserRole === 'HOD') {
      if (this.useGoalLevelScoring) {
        // Per-goal scoring mode: submit individual goal scores
        const goalScores: RmItemScoreDto[] = [];

        for (const goal of this.evaluation.goals) {
          const goalKey = goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
          const scoreData = this.rmGoalScores[goalKey];

          if (scoreData && scoreData.score > 0 && goal.personalGoalId) {
            goalScores.push({
              personalGoalId: goal.personalGoalId.toString(),
              scoreValue: scoreData.score,
              comment: scoreData.comment || undefined
            });
          }
        }

        if (goalScores.length === 0) {
          this.approving = false;
          this.showToast('error', 'No valid goal scores to submit. Please score all goals.');
          return;
        }

        const goalScoreDto: SubmitReviewWithGoalScoresDto = {
          goalScores,
          overallComment: this.rmOverallComment || this.overallComment || this.comment || undefined
        };

        console.log('Submitting per-goal scores:', JSON.stringify(goalScoreDto, null, 2));

        this.evaluationService.submitGoalScores(this.evaluationId, this.pendingReviewId, goalScoreDto).subscribe({
          next: (scoreResponse) => {
            console.log('Per-goal scores submitted successfully:', scoreResponse);

            if (this.currentUserRole === 'HOD') {
              const avgScore = scoreResponse.calculatedScore ?? 0;
              if (avgScore >= 8) {
                if (confirm('This employee has scored 8 or above. Do you want to recommend them for promotion to GM?\n\nClick OK to recommend for promotion, or Cancel to approve without promotion.')) {
                  this.recommendPromotionToGm();
                  return;
                }
              }
            }

            this.proceedWithApproval();
          },
          error: (err) => {
            this.approving = false;
            const errorMessage = err.error?.error || err.error?.message || 'Failed to submit goal scores. Please try again.';
            this.showToast('error', errorMessage);
            console.error('Error submitting goal scores:', err);
          }
        });
      } else {
        // Overall scoring mode (legacy fallback)
        const dto: SubmitOverallScoringDto = {
          overallScore: this.overallScore,
          comment: this.overallComment || this.comment || undefined
        };

        console.log('Submitting overall score:', JSON.stringify(dto, null, 2));

        this.evaluationService.submitOverallScoring(this.evaluationId, this.pendingReviewId, dto).subscribe({
          next: (scoreResponse) => {
            console.log('Overall Score submitted successfully:', scoreResponse);

            if (this.currentUserRole === 'HOD' && this.overallScore >= 8) {
              if (confirm('This employee has scored 8 or above. Do you want to recommend them for promotion to GM?\n\nClick OK to recommend for promotion, or Cancel to approve without promotion.')) {
                this.recommendPromotionToGm();
                return;
              }
            }

            this.proceedWithApproval();
          },
          error: (err) => {
            this.approving = false;
            const errorMessage = err.error?.error || err.error?.message || 'Failed to submit score. Please try again.';
            this.showToast('error', errorMessage);
            console.error('Error submitting overall score:', err);
          }
        });
      }
    }
    else {
      this.proceedWithApproval();
    }
  }

  /**
   * Proceed with the approval after scores are submitted (or if no scores needed)
   */
  private proceedWithApproval(): void {
    if (!this.evaluation) return;

    if (!this.approving) {
      this.approving = true;
      this.error = null;
    }

    this.evaluationService.approveEvaluation(this.evaluationId, this.comment || undefined).subscribe({
      next: () => {
        this.showToast('success', 'Evaluation approved successfully!');
        this.approving = false;
        this.showCommentBox = false;
        this.comment = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.approving = false;
        this.error = err.error?.error || err.error?.message || 'Failed to approve evaluation. Please try again.';
        console.error('Error approving evaluation:', err);
        this.showToast('error', this.error || 'Failed to approve evaluation. Please try again.');
      }
    });
  }

  rejectEvaluation(): void {
    if (this.rejecting || !this.evaluation) return;

    if (!this.comment.trim()) {
      this.showToast('error', 'Please provide a comment explaining the rejection.');
      return;
    }

    this.rejecting = true;
    this.error = null;

    this.evaluationService.rejectEvaluation(this.evaluationId, this.comment).subscribe({
      next: () => {
        this.showToast('success', 'Evaluation rejected. Employee has been notified.');
        this.rejecting = false;
        this.showCommentBox = false;
        this.loadEvaluation();
      },
      error: (err) => {
        this.rejecting = false;
        const errorMessage = err.error?.message || 'Failed to reject evaluation. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error rejecting evaluation:', err);
      }
    });
  }

  /** 
   * Determine if current user is the goal owner and can perform start/complete actions
   */
  determineGoalOwnership(): void {
    if (!this.evaluation) return;

    const currentUserEmail = this.authService.getCurrentUser()?.email?.toLowerCase() || '';
    
    this.isGoalOwner = this.evaluation.employeeEmail?.toLowerCase() === currentUserEmail;
    
    const status = this.evaluation.status.toLowerCase();
    this.canPerformGoalActions = this.isGoalOwner && (
      status.includes('approved_by_rm') ||
      status.includes('approvedby') ||
      status === 'approved_by_rm' ||
      status === 'approvedbyrm'
    );
  }

  // ====== Goal Action Helpers ======

  canStartGoal(goal: any): boolean {
    if (!this.isGoalOwner) return false;
    const goalStatus = goal.status?.toLowerCase() || '';
    const evalStatus = this.evaluation?.status?.toLowerCase() || '';
    return goalStatus.includes('approved') || evalStatus.includes('approved_by_rm');
  }

  canCompleteGoal(goal: any): boolean {
    if (!this.isGoalOwner) return false;
    const goalStatus = goal.status?.toLowerCase() || '';
    return goalStatus.includes('inprogress') || goalStatus === 'in_progress';
  }

  startGoal(goal: GoalDto): void {
    if (!confirm(`Start working on "${goal.title}"?`)) return;

    this.startingGoalId = goal.goalId.toString();

    this.evaluationService.startGoal(goal.goalId.toString()).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal started successfully!');
        this.startingGoalId = null;
        this.loadEvaluation();
      },
      error: (err) => {
        this.startingGoalId = null;
        const errorMessage = err.error?.error || 'Failed to start goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error starting goal:', err);
      }
    });
  }

  openCompleteGoalModal(goal: GoalDto): void {
    this.completeModalGoal = goal;
    this.completeFormEvidenceUrl = '';
    this.completeFormComment = '';
    this.completeFormCurrentScore = null;
    this.showCompleteGoalModal = true;
  }

  closeCompleteGoalModal(): void {
    this.showCompleteGoalModal = false;
    this.completeModalGoal = null;
    this.completeFormEvidenceUrl = '';
    this.completeFormComment = '';
    this.completeFormCurrentScore = null;
  }

  confirmCompleteGoal(): void {
    if (!this.completeModalGoal) return;

    const payload: CompleteGoalRequestDto = {};
    if (this.completeFormEvidenceUrl.trim()) {
      payload.evidenceUrl = this.completeFormEvidenceUrl.trim();
    }
    if (this.completeFormComment.trim()) {
      payload.comment = this.completeFormComment.trim();
    }
    if (this.completeFormCurrentScore !== null) {
      payload.currentScore = this.completeFormCurrentScore;
    }

    this.completingGoalId = this.completeModalGoal.goalId.toString();

    this.evaluationService.completeGoal(this.completeModalGoal.goalId.toString(), payload).subscribe({
      next: (response) => {
        let message = response.message || 'Goal completed successfully!';
        if (response.workflowContinued) {
          message = 'All goals completed! The evaluation has been forwarded for further review.';
        }
        this.showToast('success', message);
        this.completingGoalId = null;
        this.closeCompleteGoalModal();
        this.loadEvaluation();
      },
      error: (err) => {
        this.completingGoalId = null;
        const errorMessage = err.error?.error || 'Failed to complete goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error completing goal:', err);
      }
    });
  }

  isStartingGoal(goalId: number): boolean {
    return this.startingGoalId === goalId.toString();
  }

  isCompletingGoal(goalId: number): boolean {
    return this.completingGoalId === goalId.toString();
  }

  shouldShowReviewsSection(): boolean {
    if (!this.evaluation) return false;
    
    const status = this.evaluation.status.toLowerCase();
    
    if (status === 'pending_rm_review' || status === 'pendingrm' || status === 'pending rm') {
      return false;
    }
    
    return true;
  }

  getGoalKey(goal: GoalDto): string {
    return goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
  }

  getGoalScore(goal: GoalDto): number {
    const key = this.getGoalKey(goal);
    return this.rmGoalScores[key]?.score || 0;
  }

  getGoalComment(goal: GoalDto): string {
    const key = this.getGoalKey(goal);
    return this.rmGoalScores[key]?.comment || '';
  }

  /**
   * Filter out overall score records (personalGoalId is null) from the scores list.
   * Only return per-goal scores for display.
   */
  getGoalOnlyScores(scores: ReviewScoreDto[]): ReviewScoreDto[] {
    if (!scores) return [];
    return scores.filter(s => s.personalGoalId !== null && s.personalGoalId !== undefined);
  }

  setGoalComment(goal: GoalDto, comment: string): void {
    const key = this.getGoalKey(goal);
    if (this.rmGoalScores[key]) {
      this.rmGoalScores[key].comment = comment;
    } else {
      this.rmGoalScores[key] = { score: 5, comment: comment };
    }
  }

  selectGoalScore(goal: GoalDto, score: number): void {
    const goalKey = this.getGoalKey(goal);
    
    if (!this.rmGoalScores[goalKey]) {
      this.rmGoalScores[goalKey] = { score: score, comment: '' };
    } else {
      this.rmGoalScores[goalKey].score = score;
    }
    
    console.log(`Selected score ${score} for goal "${goal.title}" (key: ${goalKey})`);
  }

  hasAllScoresSelected(): boolean {
    if (!this.evaluation || !this.evaluation.goals) return false;
    
    const result = this.evaluation.goals.every(goal => {
      const goalKey = this.getGoalKey(goal);
      const hasScore = this.rmGoalScores[goalKey] && this.rmGoalScores[goalKey].score > 0;
      console.log(`Goal "${goal.title}" (key: ${goalKey}) hasScore: ${hasScore}, value: ${this.rmGoalScores[goalKey]?.score}`);
      return hasScore;
    });
    
    console.log('hasAllScoresSelected:', result);
    return result;
  }

  getReviewsByRole(role: ReviewerRole): ReviewDto[] {
    if (!this.evaluation) return [];
    return this.evaluation.reviews.filter(r => r.reviewerRole === role);
  }

  getReviewStatusClass(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower === 'completed' || statusLower === 'approved') return 'bg-green-100 text-green-800';
    if (statusLower === 'pending' ) return 'bg-yellow-100 text-yellow-800';
    if (statusLower === 'rejected') return 'bg-red-100 text-red-800';
    return 'bg-gray-100 text-gray-800';
  }

  getStatusBadgeClass(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('completed')) return 'bg-green-100 text-green-800';
    if (statusLower.includes('pending')) return 'bg-yellow-100 text-yellow-800';
    if (statusLower.includes('rejected')) return 'bg-red-100 text-red-800';
    return 'bg-blue-100 text-blue-800';
  }

  getStatusLabel(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  shouldShowParallelReviewProgress(): boolean {
    if (!this.evaluation || !this.isWorkflowV2()) return false;
    return this.evaluation.reviews.some(r =>
      r.reviewerRole === ReviewerRole.TL ||
      r.reviewerRole === ReviewerRole.RM ||
      r.reviewerRole === ReviewerRole.Peer);
  }

  getParallelReviewerRows(): Array<{ label: string; status: string }> {
    if (!this.evaluation) return [];

    const tlReview = this.evaluation.reviews.find(r => r.reviewerRole === ReviewerRole.TL);
    const rmReview = this.evaluation.reviews.find(r => r.reviewerRole === ReviewerRole.RM);
    const peerReviews = this.evaluation.reviews
      .filter(r => r.reviewerRole === ReviewerRole.Peer)
      .slice(0, 2);

    return [
      { label: 'Team Lead', status: tlReview?.status || 'Pending' },
      { label: 'Reporting Manager', status: rmReview?.status || 'Pending' },
      { label: 'Peer Reviewer 1', status: peerReviews[0]?.status || 'Pending' },
      { label: 'Peer Reviewer 2', status: peerReviews[1]?.status || 'Pending' }
    ];
  }

  getRoleName(role: ReviewerRole | number): string {
    const roleNum = typeof role === 'number' ? role : role;
    const roleNames: { [key: number]: string } = {
      0: 'Self Evaluation',
      1: 'Peer Reviewer',
      2: 'Team Lead',
      3: 'Reporting Manager',
      4: 'Head of Department',
      5: 'General Manager'
    };
    return roleNames[roleNum] || `Unknown Role (${role})`;
  }

  getActionColor(action: string): string {
    const actionLower = action.toLowerCase();
    if (actionLower.includes('approved') || actionLower.includes('recommended') || actionLower.includes('processed')) return 'text-green-600';
    if (actionLower.includes('rejected')) return 'text-red-600';
    if (actionLower.includes('submitted')) return 'text-blue-600';
    return 'text-gray-600';
  }

  getActionIcon(action: string): string {
    const actionLower = action.toLowerCase();
    if (actionLower.includes('approved')) return 'check-circle';
    if (actionLower.includes('rejected')) return 'x-circle';
    if (actionLower.includes('submitted')) return 'arrow-up-circle';
    return 'information-circle';
  }

  backToApprovals(): void {
    this.router.navigate(['/evaluations/my-approvals'], { queryParams: { refresh: new Date().getTime() }});
  }

  getPendingReview(): ReviewDto | undefined {
    if (!this.pendingReviewId || !this.evaluation) return undefined;
    return this.evaluation.reviews.find(r => r.reviewId === this.pendingReviewId);
  }

  getPendingReviewerRole(): number {
    const review = this.getPendingReview();
    return review?.reviewerRole || 0;
  }

  isPendingReviewItemLevel(): boolean {
    return this.isItemLevelScoring(this.getPendingReviewerRole()) || this.useGoalLevelScoring;
  }

  getPendingScoringLabel(): string {
    if (!this.evaluation || !this.pendingReviewId) return 'Submit Your Scores';
    
    const pendingReview = this.evaluation.reviews.find(r => r.reviewId === this.pendingReviewId);
    if (!pendingReview) return 'Submit Your Scores';
    
    const roleName = this.getRoleName(pendingReview.reviewerRole);
    const evaluationStatus = this.evaluation.status.toLowerCase();
    
    if (pendingReview.reviewerRole === ReviewerRole.RM && 
        (evaluationStatus.includes('postcompletion') || 
         evaluationStatus === 'pending_rm_review_postcompletion')) {
      return `${roleName} Scoring - Rate Each Completed Goal`;
    }
    
    if (pendingReview.reviewerRole === ReviewerRole.RM) {
      return `${roleName} Scoring - Rate Each Goal (1-10)`;
    }
    
    return `${roleName} Scoring - Rate Each Goal (1-10)`;
  }

  submitPendingScores(): void {
    if (this.isPendingReviewItemLevel()) {
      this.submitRmScores();
    } else {
      this.submitOverallScore();
    }
  }

  private showToast(type: 'success' | 'error', message: string): void {
    const toast = document.createElement('div');
    toast.className = `fixed top-4 right-4 px-6 py-4 rounded-lg shadow-lg z-50 ${
      type === 'success' ? 'bg-green-500 text-white' : 'bg-red-500 text-white'
    }`;
    toast.textContent = message;
    document.body.appendChild(toast);
    
    setTimeout(() => {
      toast.remove();
    }, 5000);
  }

  // HOD Actions
  finalizeHodV2(): void {
    if (!this.evaluation || this.approving) return;

    if (!confirm('Finalize HOD review and route by threshold score?')) {
      return;
    }

    this.approving = true;
    this.error = null;

    this.evaluationService.hodFinalizeV2(this.evaluationId, this.hodComment || undefined).subscribe({
      next: () => {
        this.approving = false;
        this.showToast('success', 'HOD finalization completed.');
        this.hodComment = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.approving = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to finalize HOD review.';
        this.showToast('error', errorMessage);
      }
    });
  }

  recommendPromotionToGm(): void {
    if (this.recommendingPromotion || !this.evaluation) return;

    if (!confirm('Are you sure you want to recommend this employee for promotion to GM?')) {
      return;
    }

    this.recommendingPromotion = true;
    this.error = null;

    this.evaluationService.recommendPromotion(this.evaluationId, this.hodComment || undefined).subscribe({
      next: () => {
        this.showToast('success', 'Successfully recommended for promotion. GM has been notified.');
        this.recommendingPromotion = false;
        this.hodComment = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.recommendingPromotion = false;
        const errorMessage = err.error?.message || 'Failed to recommend promotion. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error recommending promotion:', err);
      }
    });
  }

  hodRejectEvaluation(): void {
    if (this.hodRejectingEval || !this.evaluation) return;

    if (!this.hodComment.trim()) {
      this.showToast('error', 'Please provide a comment explaining the rejection.');
      return;
    }

    if (!confirm('Are you sure you want to reject this evaluation? The employee will be notified.')) {
      return;
    }

    this.hodRejectingEval = true;
    this.error = null;

    this.evaluationService.hodReject(this.evaluationId, this.hodComment).subscribe({
      next: () => {
        this.showToast('success', 'Evaluation rejected at HOD stage. Employee has been notified.');
        this.hodRejectingEval = false;
        this.hodComment = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.hodRejectingEval = false;
        const errorMessage = err.error?.message || 'Failed to reject evaluation. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error rejecting evaluation:', err);
      }
    });
  }

  // GM Actions
  setGmVacancySelection(value: 'yes' | 'no'): void {
    this.gmVacancySelection = value;
    this.gmVacancyAvailable = value === 'yes';
  }

  processGmDecision(): void {
    if (this.processingGmDecision || !this.evaluation) return;

    const isWorkflowV2 =
      (this.evaluation.workflowVersion || '').toLowerCase() === 'v2' ||
      this.evaluation.status.toLowerCase().startsWith('v2_');

    if (isWorkflowV2 && this.gmApproveDecision && this.gmVacancySelection === '') {
      this.showToast('error', 'Please confirm vacancy availability before submitting GM decision.');
      return;
    }

    const confirmMessage = this.gmApproveDecision
      ? isWorkflowV2 && !this.gmVacancyAvailable
        ? 'Approve recommendation with NO vacancy available? This will defer promotion to a future cycle.'
        : 'Are you sure you want to approve this promotion? HR will be notified for processing.'
      : 'Are you sure you want to decline this promotion? The employee will be notified.';

    if (!confirm(confirmMessage)) {
      return;
    }

    this.processingGmDecision = true;
    this.error = null;

    const request$ = isWorkflowV2
      ? this.evaluationService.gmV2Decision(
          this.evaluationId,
          this.gmApproveDecision,
          this.gmVacancyAvailable,
          this.gmComment || undefined)
      : this.evaluationService.gmDecision(
          this.evaluationId,
          this.gmApproveDecision,
          this.gmComment || undefined);

    request$.subscribe({
      next: () => {
        const successMessage = this.gmApproveDecision
          ? isWorkflowV2 && !this.gmVacancyAvailable
            ? 'GM decision saved. Promotion deferred because no vacancy is available.'
            : 'Promotion approved successfully. HR has been notified.'
          : 'Promotion declined. Employee has been notified.';
        this.showToast('success', successMessage);
        this.processingGmDecision = false;
        this.gmComment = '';
        this.gmVacancySelection = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.processingGmDecision = false;
        const errorMessage = err.error?.message || 'Failed to process GM decision. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error processing GM decision:', err);
      }
    });
  }

  // HR Actions
  openPipCaseManagement(): void {
    this.router.navigate(['/employee/hr-pip-cases'], {
      queryParams: { evaluationId: this.evaluationId }
    });
  }

  processHrAction(): void {
    if (this.processingHrAction || !this.evaluation) return;

    if (this.evaluation.status.toLowerCase().includes('v2_pending_hr_low_performer')) {
      this.openPipCaseManagement();
      return;
    }

    const confirmMessage = this.hrProceedDecision
      ? 'Are you sure you want to process this promotion? The employee will be notified with congratulations.'
      : 'Are you sure you want to decline processing this promotion?';

    if (!confirm(confirmMessage)) {
      return;
    }

    this.processingHrAction = true;
    this.error = null;

    this.evaluationService.hrProcessPromotion(this.evaluationId, this.hrProceedDecision, this.hrComment || undefined).subscribe({
      next: () => {
        const successMessage = this.hrProceedDecision
          ? 'Promotion processed successfully. Employee has been notified.'
          : 'Promotion processing declined.';
        this.showToast('success', successMessage);
        this.processingHrAction = false;
        this.hrComment = '';
        this.loadEvaluation();
      },
      error: (err) => {
        this.processingHrAction = false;
        const errorMessage = err.error?.message || 'Failed to process HR action. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error processing HR action:', err);
      }
    });
  }

  isCurrentSubmission(review: ReviewDto): boolean {
    if (!review || !review.scores || review.scores.length === 0) return false;
    
    // Check if this review was submitted within the last 5 minutes (current session)
    if (review.submittedAt) {
      const submittedTime = new Date(review.submittedAt).getTime();
      const now = new Date().getTime();
      const fiveMinutesAgo = now - (5 * 60 * 1000);
      
      return submittedTime >= fiveMinutesAgo;
    }
    
    return false;
  }

  getScoreSubmissionStatus(): string {
    if (!this.evaluation || !this.pendingReviewId) return '';
    
    const pendingReview = this.evaluation.reviews.find(r => r.reviewId === this.pendingReviewId);
    if (!pendingReview) return '';
    
    const evaluationStatus = this.evaluation.status.toLowerCase();
    
    // ✅ FIX: Case-insensitive status checks for RM based on evaluation stage
    if (pendingReview.reviewerRole === ReviewerRole.RM) {
      if (evaluationStatus === 'pending_rm_review') {
        // First RM approval - no scoring needed
        return 'Review and approve/reject the goal set. No scoring required at this stage.';
      } else if (evaluationStatus.includes('postcompletion') || 
                 evaluationStatus === 'pending_rm_review_postcompletion') {
        // Second RM approval - scoring required
        return 'Please score each completed goal (1-10). You must submit scores before approving.';
      }
    }
    
    // For other roles (TL, Peer, HOD, GM)
    if (pendingReview.reviewerRole === ReviewerRole.TL) {
      return 'Please score each goal individually (1-10). You must submit scores before approving.';
    }
    
    if (pendingReview.reviewerRole === ReviewerRole.Peer) {
      return 'Please score each goal individually (1-10). You must submit your scores before approving.';
    }
    
    if (pendingReview.reviewerRole === ReviewerRole.HOD) {
      return 'Please score each goal individually (1-10). You can then approve without promotion, or recommend for promotion to GM.';
    }
    
    if (pendingReview.reviewerRole === ReviewerRole.GM) {
      return 'Please provide your GM score (1-10). You must submit your score before approving.';
    }
    
    return 'Please submit your scores before approving this evaluation.';
  }

  shouldDisplayReview(review: ReviewDto, index: number): boolean {
    if (!review) return false;
    
    // For peer reviews by current user, only show the first pending one
    if (review.reviewerRole === ReviewerRole.Peer && this.currentUserId !== null && review.reviewerUserId === this.currentUserId) {
      if (review.status.toLowerCase() === 'pending' && this.evaluation) {
        const allPeerReviewsByCurrentUser = this.evaluation.reviews
          .filter(r => r.reviewerRole === ReviewerRole.Peer && r.reviewerUserId === this.currentUserId)
          .sort((a, b) => a.reviewId - b.reviewId);
        
        const pendingPeerReviews = allPeerReviewsByCurrentUser.filter(r => r.status.toLowerCase() === 'pending');
        
        if (pendingPeerReviews.length > 0) {
          const firstPendingIndex = allPeerReviewsByCurrentUser.findIndex(r => r.reviewId === pendingPeerReviews[0].reviewId);
          const currentReviewIndex = allPeerReviewsByCurrentUser.findIndex(r => r.reviewId === review.reviewId);
          
          return currentReviewIndex === firstPendingIndex;
        }
      }
    }
    
    return true;
  }

  /**
   * TL Combined Review: Submit score, assign peers, and approve in one action
   */
  submitTlCombinedReview(): void {
    if (!this.evaluation) return;

    // Validate peer selections
    if (!this.peerUserId1 || !this.peerUserId2) {
      this.showToast('error', 'Please select both peer reviewers.');
      return;
    }

    this.submittingTlCombinedReview = true;

    if (this.pendingReviewId) {
      // Build per-goal scores
      const goalScores: RmItemScoreDto[] = [];
      for (const goal of this.evaluation.goals) {
        const goalKey = goal.personalGoalId ? goal.personalGoalId.toString() : goal.goalId.toString();
        const scoreData = this.rmGoalScores[goalKey];
        if (scoreData && scoreData.score > 0 && goal.personalGoalId) {
          goalScores.push({
            personalGoalId: goal.personalGoalId.toString(),
            scoreValue: scoreData.score,
            comment: scoreData.comment || undefined
          });
        }
      }

      if (goalScores.length === 0) {
        this.submittingTlCombinedReview = false;
        this.showToast('error', 'Please score all goals before submitting.');
        return;
      }

      const goalScoreDto: SubmitReviewWithGoalScoresDto = {
        goalScores,
        overallComment: this.rmOverallComment || this.overallComment || undefined
      };

      // Step 1: Submit TL per-goal scores
      this.evaluationService.submitGoalScores(this.evaluationId, this.pendingReviewId, goalScoreDto).subscribe({
        next: (scoreResponse) => {
          console.log('TL Goal scores submitted successfully:', scoreResponse);
          
          // Step 2: Approve the evaluation (this moves to Pending_Peer_Assignment)
          this.evaluationService.approveEvaluation(this.evaluationId, this.overallComment || undefined).subscribe({
            next: () => {
              console.log('TL Approval successful, now assigning peers...');
              
              // Step 3: Assign peer reviewers
              this.evaluationService.assignPeerReviewers(this.evaluationId, this.peerUserId1!, this.peerUserId2!).subscribe({
                next: () => {
                  this.submittingTlCombinedReview = false;
                  this.showToast('success', 'Goal scores submitted and peer reviewers assigned successfully!');
                  this.loadEvaluation();
                },
                error: (err) => {
                  this.submittingTlCombinedReview = false;
                  const errorMessage = err.error?.error || err.error?.message || 'Score submitted but failed to assign peer reviewers.';
                  this.showToast('error', errorMessage);
                  console.error('Error assigning peers:', err);
                  this.loadEvaluation();
                }
              });
            },
            error: (err) => {
              this.submittingTlCombinedReview = false;
              const errorMessage = err.error?.error || err.error?.message || 'Score submitted but failed to approve evaluation.';
              this.showToast('error', errorMessage);
              console.error('Error approving evaluation:', err);
              this.loadEvaluation();
            }
          });
        },
        error: (err) => {
          this.submittingTlCombinedReview = false;
          const errorMessage = err.error?.error || err.error?.message || 'Failed to submit TL goal scores.';
          this.showToast('error', errorMessage);
          console.error('Error submitting TL goal scores:', err);
        }
      });
    } else {
      this.submittingTlCombinedReview = false;
      this.showToast('error', 'Could not find pending review. Please refresh and try again.');
    }
  }

  /**
   * Check if TL can submit the combined review
   */
  canSubmitTlCombinedReview(): boolean {
    const hasScores = this.hasAllScoresSelected();
    return hasScores &&
           this.peerUserId1 !== null && 
           this.peerUserId2 !== null;
  }
}
