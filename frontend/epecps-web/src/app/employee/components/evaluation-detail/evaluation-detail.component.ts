import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { EvaluationService } from '../../../services/evaluation.service';
import {
  EvaluationDetailDto,
  ReviewDto,
  ApprovalHistoryItemDto,
  ReviewerRole,
  GoalDto,
  CompleteGoalRequestDto,
  SubmitRmScoringDto,
  RmItemScoreDto,
  SubmitOverallScoringDto
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

  // Peer assignment state
  showPeerAssignment = false;
  assigningPeers = false;
  loadingPeers = false;
  peerUserId1: number | null = null;
  peerUserId2: number | null = null;
  availablePeers: Array<{ userId: number; name: string }> = [];

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

  // HR action state
  showHrActions = false;
  processingHrAction = false;
  hrComment = '';
  hrProceedDecision = true;

  // Current user state (would come from auth service in real app)
  currentUserId = 0; // This should come from AuthService
  isActiveApprover = false;
  currentUserRole = '';
  needsPeerAssignment = false;
  userRoles: string[] = []; // Store all user roles

  // ====== NEW: Employee Goal Actions State ======
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
  showScoringPanel = false;
  submittingScores = false;
  pendingReviewId: number | null = null;
  
  // RM scoring - item-level scores per goal
  rmGoalScores: { [goalId: string]: { score: number; comment: string } } = {};
  rmOverallComment = '';
  
  // Overall scoring for TL/HOD/GM/Peer
  overallScore: number = 5;
  overallComment = '';

  ReviewerRole = ReviewerRole;

  // ✅ NEW: Track if current user has a pending scoring requirement
  hasPendingScoringRequirement = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private evaluationService: EvaluationService,
    private authService: MsalService
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
        this.determineUserRole();
        this.determineGoalOwnership();
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
   * Check if current user has a pending review that needs scoring
   */
  checkForPendingReview(): void {
    if (!this.evaluation) return;

    const account = this.authService.instance.getActiveAccount();
    const currentUserEmail = account?.username?.toLowerCase() || '';

    // Find pending review for current user
    const pendingReview = this.evaluation.reviews.find(r => 
      r.status.toLowerCase() === 'pending' && 
      r.reviewerName.toLowerCase().includes(currentUserEmail.split('@')[0].toLowerCase())
    );

    if (pendingReview) {
      this.pendingReviewId = pendingReview.reviewId;
      this.hasPendingScoringRequirement = true;
      this.initializeScoringForReview(pendingReview);
    } else {
      this.hasPendingScoringRequirement = false;
    }
  }

  /**
   * Initialize scoring based on reviewer role and load existing scores if available
   */
  initializeScoringForReview(review: ReviewDto): void {
    const role = review.reviewerRole;
    
    // RM (role=3) does item-level scoring
    if (role === 3) {
      this.initializeRmScoring();
    }
    // TL(2), Peer(1), HOD(4), GM(5) do overall scoring
    else {
      // Check if there's an existing overall score
      if (review.overallScore !== null && review.overallScore !== undefined) {
        this.overallScore = review.overallScore;
      } else {
        this.overallScore = 5; // default
      }
      
      // Load existing overall comment
      this.overallComment = review.overallComment || '';
    }
  }

  /**
   * Initialize RM scoring with default or existing values for each goal
   */
  initializeRmScoring(): void {
    if (!this.evaluation) return;
    
    this.rmGoalScores = {};
    
    // Find the RM review for this evaluation
    const rmReview = this.evaluation.reviews.find(r => r.reviewerRole === 3); // RM = 3
    
    console.log('Initializing RM scoring. RM Review:', rmReview);
    console.log('Available scores in RM review:', rmReview?.scores);
    
    this.evaluation.goals.forEach(goal => {
      // Use personalGoalId if available, otherwise fall back to goalId (for backward compatibility)
      const goalKey = goal.personalGoalId || goal.goalId.toString();
      
      // Check if there's an existing score for this goal
      let existingScore: number = 5; // default
      let existingComment: string = '';
      
      if (rmReview && rmReview.scores && rmReview.scores.length > 0) {
        // Look for a score that matches this goal's personalGoalId
        const scoreForThisGoal = rmReview.scores.find(s => s.personalGoalId === goal.personalGoalId);
        if (scoreForThisGoal) {
          existingScore = scoreForThisGoal.scoreValue;
          existingComment = scoreForThisGoal.comment || '';
          console.log(`Found existing score for goal "${goal.title}": ${existingScore}`);
        } else {
          console.log(`No existing score found for goal "${goal.title}" (personalGoalId: ${goal.personalGoalId})`);
        }
      }
      
      // Initialize with existing score or default
      this.rmGoalScores[goalKey] = {
        score: existingScore,
        comment: existingComment
      };
    });
    
    console.log('Initialized rmGoalScores:', this.rmGoalScores);
    
    // Load existing overall comment if available
    this.rmOverallComment = rmReview?.overallComment || '';
  }

  /**
   * Open scoring panel for a specific review
   */
  openScoringPanel(review: ReviewDto): void {
    this.pendingReviewId = review.reviewId;
    this.initializeScoringForReview(review);
    this.showScoringPanel = true;
  }

  /**
   * Close scoring panel
   */
  closeScoringPanel(): void {
    this.showScoringPanel = false;
    this.pendingReviewId = null;
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
        this.closeScoringPanel();
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
        this.closeScoringPanel();
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
   * Check if current user can score a specific review
   */
  canScoreReview(review: ReviewDto): boolean {
    if (review.status.toLowerCase() !== 'pending') return false;
    
    // Check if user is the reviewer
    const account = this.authService.instance.getActiveAccount();
    const currentUserEmail = account?.username?.toLowerCase() || '';
    
    // Simple check - in real app, compare user IDs
    return review.reviewerName.toLowerCase().includes(currentUserEmail.split('@')[0].toLowerCase());
  }

  /**
   * Get the scoring type label based on reviewer role
   */
  getScoringTypeLabel(role: number): string {
    if (role === 3) return 'Item-Level Scoring (1-10 per goal)';
    return 'Overall Scoring (1-10)';
  }

  /**
   * Check if review requires item-level scoring (RM only)
   */
  isItemLevelScoring(role: number): boolean {
    return role === 3; // RM = 3
  }

  determineUserRole(): void {
    if (!this.evaluation) return;

    const status = this.evaluation.status.toLowerCase();
    
    // TODO: Get actual user roles from AuthService
    // For now, simulate roles based on status
    this.userRoles = this.getUserRolesFromStatus(status);
    
    // Reset all action flags
    this.isActiveApprover = false;
    this.needsPeerAssignment = false;
    this.showHodActions = false;
    this.showGmActions = false;
    this.showHrActions = false;
    
    // Check if peer assignment is needed
    if (status.includes('pending_peer_assignment')) {
      this.needsPeerAssignment = true;
      this.isActiveApprover = true;
      this.currentUserRole = 'TL';
      this.loadAvailablePeers();
      return;
    }

    // Determine active approver role
    if (status.includes('pending_hod')) {
      // HOD stage - only show HOD promotion panel
      this.isActiveApprover = false;
      this.showHodActions = true;
      this.currentUserRole = 'HOD';
    } else if (status.includes('pending_gm')) {
      // GM stage - only show GM decision panel
      this.isActiveApprover = false;
      this.showGmActions = true;
      this.currentUserRole = 'GM';
    } else if (status.includes('pending_hr')) {
      // HR stage - only show HR processing panel
      this.isActiveApprover = false;
      this.showHrActions = true;
      this.currentUserRole = 'HR';
    } else if (status.includes('pending_rm')) {
      this.isActiveApprover = true;
      this.currentUserRole = 'RM';
      // Initialize RM scoring when RM is the active approver
      this.initializeRmScoring();
    } else if (status.includes('pending_tl')) {
      this.isActiveApprover = true;
      this.currentUserRole = 'TL';
    } else if (status.includes('pending_peer')) {
      this.isActiveApprover = true;
      this.currentUserRole = 'Peer';
    }
  }

  getUserRolesFromStatus(status: string): string[] {
    // TODO: Replace with actual roles from AuthService
    // For demo purposes, return roles based on status
    const roles: string[] = ['Employee'];
    if (status.includes('hod')) roles.push('HOD');
    if (status.includes('gm')) roles.push('GM');
    if (status.includes('hr')) roles.push('HR');
    return roles;
  }

  loadAvailablePeers(): void {
    this.loadingPeers = true;
    console.log('Loading available peers for evaluation:', this.evaluationId);
    
    // Load available peers from the API
    this.evaluationService.getAvailablePeers(this.evaluationId).subscribe({
      next: (peers) => {
        console.log('Available peers loaded:', peers);
        this.availablePeers = peers.map(p => ({
          userId: p.userId,
          name: `${p.fullName}${p.department ? ` (${p.department})` : ''}`
        }));
        this.loadingPeers = false;
        console.log('Mapped peers:', this.availablePeers);
      },
      error: (err) => {
        console.error('Error loading peers:', err);
        this.loadingPeers = false;
        
        // Show error details
        const errorMessage = err.error?.message || err.message || 'Failed to load available peers';
        console.error('Error details:', errorMessage, err);
        
        this.showToast('error', `Failed to load available peers: ${errorMessage}`);
        
        // Fallback: empty array so user sees the issue
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

    // Allow same user for testing when only one user exists
    // In production, uncomment this validation:
    // if (this.peerUserId1 === this.peerUserId2) {
    //   this.showToast('error', 'Please select two different peer reviewers');
    //   return;
    // }

    this.assigningPeers = true;

    this.evaluationService.assignPeerReviewers(this.evaluationId, this.peerUserId1, this.peerUserId2).subscribe({
      next: () => {
        this.showToast('success', 'Peer reviewers assigned successfully!');
        this.assigningPeers = false;
        this.showPeerAssignment = false;
        this.loadEvaluation(); // Reload to show updated state
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

    // ✅ NEW: Check if user has pending scoring requirement that hasn't been fulfilled
    if (this.hasPendingScoringRequirement && this.pendingReviewId) {
      // Check if they are RM and have selected scores
      if (this.currentUserRole === 'RM') {
        const hasScores = this.hasAllScoresSelected();
        
        if (hasScores) {
          // Submit scores first, then approve
          this.submitScoresAndApprove();
          return;
        } else {
          this.showToast('error', 'Please score all goals before approving.');
          return;
        }
      }
      // Check if they are TL/Peer and have selected a score
      else if ((this.currentUserRole === 'TL' || this.currentUserRole === 'Peer') && this.overallScore) {
        // Submit score first, then approve
        this.submitScoresAndApprove();
        return;
      }
    }

    // If no scoring requirement or scores already submitted, proceed with normal approval
    this.proceedWithApproval();
  }

  /**
   * ✅ NEW: Combined method to submit scores and approve in one flow
   */
  private submitScoresAndApprove(): void {
    if (!this.evaluation || !this.pendingReviewId) return;

    this.approving = true;
    this.error = null;

    // Determine which type of score to submit
    if (this.currentUserRole === 'RM') {
      // Submit RM item-level scores
      const itemScores: RmItemScoreDto[] = [];
      
      for (const goal of this.evaluation.goals) {
        const goalKey = goal.personalGoalId || goal.goalId.toString();
        const scoreData = this.rmGoalScores[goalKey];
        
        if (scoreData && scoreData.score > 0) {
          if (goal.personalGoalId) {
            itemScores.push({
              personalGoalId: goal.personalGoalId,
              scoreValue: scoreData.score,
              comment: scoreData.comment || undefined
            });
          } else {
            this.approving = false;
            this.showToast('error', `Goal "${goal.title}" is missing personalGoalId. Cannot submit scores.`);
            return;
          }
        }
      }

      const scoreDto: SubmitRmScoringDto = {
        itemScores,
        overallComment: this.rmOverallComment || this.comment || undefined
      };

      console.log('Submitting RM scores:', scoreDto);

      this.evaluationService.submitRmScoring(this.evaluationId, this.pendingReviewId, scoreDto).subscribe({
        next: (scoreResponse) => {
          console.log('RM Scores submitted:', scoreResponse);
          // Now proceed with approval
          this.proceedWithApproval();
        },
        error: (err) => {
          this.approving = false;
          const errorMessage = err.error?.error || 'Failed to submit scores. Please try again.';
          this.showToast('error', errorMessage);
          console.error('Error submitting RM scores:', err);
        }
      });
    }
    else if (this.currentUserRole === 'TL' || this.currentUserRole === 'Peer') {
      // Submit overall score
      const dto: SubmitOverallScoringDto = {
        overallScore: this.overallScore,
        comment: this.overallComment || this.comment || undefined
      };

      console.log('Submitting overall score:', dto);

      this.evaluationService.submitOverallScoring(this.evaluationId, this.pendingReviewId, dto).subscribe({
        next: (scoreResponse) => {
          console.log('Overall Score submitted:', scoreResponse);
          // Now proceed with approval
          this.proceedWithApproval();
        },
        error: (err) => {
          this.approving = false;
          const errorMessage = err.error?.error || 'Failed to submit score. Please try again.';
          this.showToast('error', errorMessage);
          console.error('Error submitting overall score:', err);
        }
      });
    }
    else {
      // No scoring requirement, proceed with normal approval
      this.proceedWithApproval();
    }
  }

  /**
   * Proceed with the approval after scores are submitted (or if no scores needed)
   */
  private proceedWithApproval(): void {
    if (!this.evaluation) return;

    // If already approving from submitScoresAndApprove, don't set it again
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
        this.loadEvaluation(); // Reload to show updated state
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
        this.loadEvaluation(); // Reload to show updated state
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

    // Get current user email from MSAL
    const account = this.authService.instance.getActiveAccount();
    const currentUserEmail = account?.username?.toLowerCase() || '';
    
    // Check if current user is the employee of this evaluation
    this.isGoalOwner = this.evaluation.employeeEmail?.toLowerCase() === currentUserEmail;
    
    // Check if status allows goal actions (ApprovedByRM or similar)
    const status = this.evaluation.status.toLowerCase();
    this.canPerformGoalActions = this.isGoalOwner && (
      status.includes('approved_by_rm') ||
      status.includes('approvedby') ||
      status === 'approved_by_rm' ||
      status === 'approvedbyrm'
    );
  }

  // ====== NEW: Goal Action Helpers ======

  /**
   * Check if a specific goal can be started (must be ApprovedByRM status)
   */
  canStartGoal(goal: any): boolean {
    if (!this.isGoalOwner) return false;
    // Check goal status if available, otherwise check evaluation status
    const goalStatus = goal.status?.toLowerCase() || '';
    const evalStatus = this.evaluation?.status?.toLowerCase() || '';
    return goalStatus.includes('approved') || evalStatus.includes('approved_by_rm');
  }

  /**
   * Check if a specific goal can be completed (must be InProgress status)
   */
  canCompleteGoal(goal: any): boolean {
    if (!this.isGoalOwner) return false;
    const goalStatus = goal.status?.toLowerCase() || '';
    return goalStatus.includes('inprogress') || goalStatus === 'in_progress';
  }

  /**
   * Start working on a goal
   */
  startGoal(goal: GoalDto): void {
    if (!confirm(`Start working on "${goal.title}"?`)) return;

    this.startingGoalId = goal.goalId.toString();

    this.evaluationService.startGoal(goal.goalId.toString()).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal started successfully!');
        this.startingGoalId = null;
        this.loadEvaluation(); // Refresh to show updated status
      },
      error: (err) => {
        this.startingGoalId = null;
        const errorMessage = err.error?.error || 'Failed to start goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error starting goal:', err);
      }
    });
  }

  /**
   * Open complete goal modal
   */
  openCompleteGoalModal(goal: GoalDto): void {
    this.completeModalGoal = goal;
    this.completeFormEvidenceUrl = '';
    this.completeFormComment = '';
    this.completeFormCurrentScore = null;
    this.showCompleteGoalModal = true;
  }

  /**
   * Close complete goal modal
   */
  closeCompleteGoalModal(): void {
    this.showCompleteGoalModal = false;
    this.completeModalGoal = null;
    this.completeFormEvidenceUrl = '';
    this.completeFormComment = '';
    this.completeFormCurrentScore = null;
  }

  /**
   * Submit goal completion
   */
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
        this.loadEvaluation(); // Refresh to show updated evaluation status
      },
      error: (err) => {
        this.completingGoalId = null;
        const errorMessage = err.error?.error || 'Failed to complete goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error completing goal:', err);
      }
    });
  }

  /**
   * Check if a goal is currently being started
   */
  isStartingGoal(goalId: number): boolean {
    return this.startingGoalId === goalId.toString();
  }

  /**
   * Check if a goal is currently being completed
   */
  isCompletingGoal(goalId: number): boolean {
    return this.completingGoalId === goalId.toString();
  }

  /**
   * Check if Reviews & Ratings section should be shown
   * Hide during initial Pending_RM_Review stage (before goal completion)
   * Show for Pending_RM_Review_PostCompletion and all other stages
   */
  shouldShowReviewsSection(): boolean {
    if (!this.evaluation) return false;
    
    const status = this.evaluation.status.toLowerCase();
    
    // Hide during initial RM review (before goals completed)
    // Backend sends "Pending_RM_Review" for initial review
    if (status === 'pending_rm_review' || status === 'pendingrm' || status === 'pending rm') {
      return false;
    }
    
    // Show for all other stages including:
    // - Pending_RM_Review_PostCompletion (after goals completed)
    // - Pending_TL_Review
    // - Pending_Peer_Reviews
    // - Pending_HOD_Review
    // - Pending_GM_Decision
    // - Completed
    // etc.
    return true;
  }

  /**
   * Select a score for a specific goal (clickable number button)
   */
  selectGoalScore(goal: GoalDto, score: number): void {
    const goalKey = goal.personalGoalId || goal.goalId.toString();
    // Ensure the object exists before setting the score
    if (!this.rmGoalScores[goalKey]) {
      this.rmGoalScores[goalKey] = { score: score, comment: '' };
    } else {
      this.rmGoalScores[goalKey].score = score;
    }
  }

  /**
   * Check if all goals have been scored
   */
  hasAllScoresSelected(): boolean {
    if (!this.evaluation || !this.evaluation.goals) return false;
    
    return this.evaluation.goals.every(goal => {
      const goalKey = goal.personalGoalId || goal.goalId.toString();
      return this.rmGoalScores[goalKey] && this.rmGoalScores[goalKey].score > 0;
    });
  }

  getReviewsByRole(role: ReviewerRole): ReviewDto[] {
    if (!this.evaluation) return [];
    return this.evaluation.reviews.filter(r => r.reviewerRole === role);
  }

  getReviewStatusClass(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower === 'completed' || statusLower === 'approved') return 'bg-green-100 text-green-800';
    if (statusLower === 'pending') return 'bg-yellow-100 text-yellow-800';
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

  getRoleName(role: ReviewerRole | number): string {
    // Handle both numeric and enum values since backend sends numbers
    const roleNum = typeof role === 'number' ? role : role;
    const roleNames: { [key: number]: string } = {
      0: 'Self Evaluation',       // ReviewerRole.Self
      1: 'Peer Reviewer',          // ReviewerRole.Peer
      2: 'Team Lead',              // ReviewerRole.TL
      3: 'Reporting Manager',      // ReviewerRole.RM
      4: 'Head of Department',     // ReviewerRole.HOD
      5: 'General Manager'         // ReviewerRole.GM
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
    this.router.navigate(['/evaluations/my-approvals']),
    { queryParams: { refresh: new Date().getTime() }};
  }

  /**
   * Get the pending review object for the scoring modal
   */
  getPendingReview(): ReviewDto | undefined {
    if (!this.pendingReviewId || !this.evaluation) return undefined;
    return this.evaluation.reviews.find(r => r.reviewId === this.pendingReviewId);
  }

  /**
   * Get the reviewer role for the pending review
   */
  getPendingReviewerRole(): number {
    const review = this.getPendingReview();
    return review?.reviewerRole || 0;
  }

  /**
   * Check if the pending review uses item-level scoring
   */
  isPendingReviewItemLevel(): boolean {
    return this.isItemLevelScoring(this.getPendingReviewerRole());
  }

  /**
   * Get the scoring type label for the pending review
   */
  getPendingScoringLabel(): string {
    return this.getScoringTypeLabel(this.getPendingReviewerRole());
  }

  /**
   * Submit scores based on review type
   */
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
  processGmDecision(): void {
    if (this.processingGmDecision || !this.evaluation) return;

    const confirmMessage = this.gmApproveDecision
      ? 'Are you sure you want to approve this promotion? HR will be notified for processing.'
      : 'Are you sure you want to decline this promotion? The employee will be notified.';

    if (!confirm(confirmMessage)) {
      return;
    }

    this.processingGmDecision = true;
    this.error = null;

    this.evaluationService.gmDecision(this.evaluationId, this.gmApproveDecision, this.gmComment || undefined).subscribe({
      next: () => {
        const successMessage = this.gmApproveDecision
          ? 'Promotion approved successfully. HR has been notified.'
          : 'Promotion declined. Employee has been notified.';
        this.showToast('success', successMessage);
        this.processingGmDecision = false;
        this.gmComment = '';
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
  processHrAction(): void {
    if (this.processingHrAction || !this.evaluation) return;

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

  /**
   * ✅ UPDATED: Show only current submission's scores in Reviews section
   * This method checks if a review belongs to the current user and has been submitted in this session
   */
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

  /**
   * ✅ NEW: Helper to show score submission status in UI
   */
  getScoreSubmissionStatus(): string {
    if (!this.hasPendingScoringRequirement) {
      return 'No scoring required';
    }

    if (this.currentUserRole === 'RM') {
      const hasScores = this.hasAllScoresSelected();
      return hasScores ? 'Scores ready to submit' : 'Please score all goals';
    }

    if ((this.currentUserRole === 'TL' || this.currentUserRole === 'Peer') && this.overallScore) {
      return 'Score ready to submit';
    }

    return 'Score required before approval';
  }
}
