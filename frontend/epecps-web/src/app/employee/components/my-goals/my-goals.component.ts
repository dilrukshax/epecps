import { Component, OnInit } from '@angular/core';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import { EvaluationService } from '../../../services/evaluation.service';
import {
  ActivityStatus,
  PersonalGoalActivityDto,
  PersonalGoalDetailDto,
  PersonalGoalSetDto,
  PersonalGoalStatus,
  PersonalGoalListDto,
  GoalSetApprovalHistoryEventDto,
  UpdatePersonalGoalActivityDto
} from '../../../models/employee-goals.models';
import { CompleteGoalRequestDto } from '../../../models/evaluation.models';

/**
 * Component for displaying all personal goals grouped by goal sets
 */
@Component({
  selector: 'app-my-goals',
  templateUrl: './my-goals.component.html',
  styleUrls: ['./my-goals.component.css'],
  standalone: false
})
export class MyGoalsComponent implements OnInit {
  goalSets: PersonalGoalSetDto[] = [];
  filteredGoalSets: PersonalGoalSetDto[] = [];
  private goalSetDisplayNames: Record<string, string> = {};
  loading = false;
  error: string | null = null;
  submittingGoalSetId: string | null = null;

  // Filter state
  selectedStatus: PersonalGoalStatus | 'all' = 'all';
  searchQuery = '';

  // Expanded goal sets
  expandedSetIds: Set<string> = new Set();
  expandedGoalIds: Set<string> = new Set();

  // Reference to enum for template
  PersonalGoalStatus = PersonalGoalStatus;
  ActivityStatus = ActivityStatus;

  // Inline goal details state (merged Goal Details experience)
  goalDetails: { [goalId: string]: PersonalGoalDetailDto } = {};
  goalDetailErrors: { [goalId: string]: string } = {};
  loadingGoalDetailIds: Set<string> = new Set();

  editingScoreGoalId: string | null = null;
  tempScore = 0;

  addingActivityGoalId: string | null = null;
  newActivityDescription = '';
  newActivityDueDate: string | null = null;

  editingActivityGoalId: string | null = null;
  editingActivity: PersonalGoalActivityDto | null = null;
  activityFormData: UpdatePersonalGoalActivityDto | null = null;

  // Workflow v2 activation state (employee stage)
  activationMethods: { [goalAssignmentId: string]: string } = {};
  submittingActivationGoalSetId: string | null = null;

  // ====== NEW: Start/Complete flow state ======
  startingGoalId: string | null = null;
  completingGoalId: string | null = null;
  
  // Complete goal modal state
  showCompleteModal = false;
  completeModalGoal: PersonalGoalListDto | null = null;
  completeModalGoalSetId: string | null = null;
  completeFormEvidenceUrl = '';
  completeFormCertificationUrl = '';
  completeFormSummary = '';
  completeFormComment = '';
  completeFormCurrentScore: number | null = null;

  constructor(
    private goalsService: EmployeeGoalsService,
    private evaluationService: EvaluationService
  ) {}

  ngOnInit(): void {
    this.loadGoals();
  }

  loadGoals(): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getMyGoalSets().subscribe({
      next: (goalSets) => {
        this.goalSets = goalSets;
        this.goalSetDisplayNames = this.buildGoalSetDisplayNames(goalSets);
        this.initializeActivationMethods(goalSets);
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load goals. Please try again.';
        this.loading = false;
        console.error('Error loading goals:', err);
      }
    });
  }

  applyFilters(): void {
    this.filteredGoalSets = this.goalSets.filter(goalSet => {
      // Status filter
      const statusMatch = this.selectedStatus === 'all' || goalSet.status === this.selectedStatus;

      // Search filter
      const searchMatch = !this.searchQuery || 
        this.getGoalSetDisplayName(goalSet).toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        goalSet.categories.some(cat => cat.toLowerCase().includes(this.searchQuery.toLowerCase())) ||
        goalSet.goals.some(g => g.title.toLowerCase().includes(this.searchQuery.toLowerCase()));

      return statusMatch && searchMatch;
    });
  }

  onStatusFilterChange(status: PersonalGoalStatus | 'all'): void {
    this.selectedStatus = status;
    this.applyFilters();
  }

  onSearchChange(query: string): void {
    this.searchQuery = query;
    this.applyFilters();
  }

  toggleGoalSet(goalSetId: string): void {
    if (this.expandedSetIds.has(goalSetId)) {
      this.expandedSetIds.delete(goalSetId);
    } else {
      this.expandedSetIds.add(goalSetId);
    }
  }

  isGoalSetExpanded(goalSetId: string): boolean {
    return this.expandedSetIds.has(goalSetId);
  }

  toggleGoalInlineDetails(goalId: string, event: Event): void {
    event.stopPropagation();

    if (this.expandedGoalIds.has(goalId)) {
      this.expandedGoalIds.delete(goalId);
      this.cancelInlineEditsForGoal(goalId);
      return;
    }

    this.expandedGoalIds.add(goalId);
    this.loadGoalDetailsInline(goalId);
  }

  isGoalInlineExpanded(goalId: string): boolean {
    return this.expandedGoalIds.has(goalId);
  }

  private initializeActivationMethods(goalSets: PersonalGoalSetDto[]): void {
    for (const goalSet of goalSets) {
      for (const goal of goalSet.goals) {
        if (!goal.goalAssignmentId) continue;
        if (this.activationMethods[goal.goalAssignmentId] !== undefined) continue;
        this.activationMethods[goal.goalAssignmentId] = goal.activationMethod || '';
      }
    }
  }

  private cancelInlineEditsForGoal(goalId: string): void {
    if (this.editingScoreGoalId === goalId) {
      this.cancelEditingGoalScore();
    }
    if (this.addingActivityGoalId === goalId) {
      this.cancelAddingActivity();
    }
    if (this.editingActivityGoalId === goalId) {
      this.cancelEditingActivity();
    }
  }

  loadGoalDetailsInline(goalId: string, forceRefresh = false): void {
    if (!forceRefresh && this.goalDetails[goalId]) {
      return;
    }

    this.loadingGoalDetailIds.add(goalId);
    delete this.goalDetailErrors[goalId];

    this.goalsService.getGoalDetails(goalId).subscribe({
      next: (goal) => {
        this.goalDetails[goalId] = goal;
        this.loadingGoalDetailIds.delete(goalId);
      },
      error: (err) => {
        this.goalDetailErrors[goalId] = 'Failed to load goal details.';
        this.loadingGoalDetailIds.delete(goalId);
        console.error('Error loading goal details:', err);
      }
    });
  }

  getGoalDetails(goalId: string): PersonalGoalDetailDto | null {
    return this.goalDetails[goalId] || null;
  }

  isLoadingGoalDetails(goalId: string): boolean {
    return this.loadingGoalDetailIds.has(goalId);
  }

  getGoalDetailError(goalId: string): string | null {
    return this.goalDetailErrors[goalId] || null;
  }

  getStatusLabel(status: PersonalGoalStatus): string {
    switch (status) {
      case PersonalGoalStatus.Draft:
        return 'Draft';
      case PersonalGoalStatus.InProgress:
        return 'In Progress';
      case PersonalGoalStatus.Completed:
        return 'Completed';
      case PersonalGoalStatus.Cancelled:
        return 'Cancelled';
      case PersonalGoalStatus.UnderEvaluation:
        return 'Under Evaluation';
      case PersonalGoalStatus.PendingRMReview:
        return 'Pending RM Review';
      case PersonalGoalStatus.ApprovedByRM:
        return 'Approved by RM';
      case PersonalGoalStatus.ReturnedToEmployee:
        return 'Returned to Employee';
      default:
        return 'Unknown';
    }
  }

  getStatusClass(status: PersonalGoalStatus): string {
    switch (status) {
      case PersonalGoalStatus.Draft:
        return 'bg-gray-100 text-gray-800';
      case PersonalGoalStatus.InProgress:
        return 'bg-blue-100 text-blue-800';
      case PersonalGoalStatus.Completed:
        return 'bg-green-100 text-green-800';
      case PersonalGoalStatus.Cancelled:
        return 'bg-red-100 text-red-800';
      case PersonalGoalStatus.UnderEvaluation:
        return 'bg-purple-100 text-purple-800';
      case PersonalGoalStatus.PendingRMReview:
        return 'bg-yellow-100 text-yellow-800';
      case PersonalGoalStatus.ApprovedByRM:
        return 'bg-teal-100 text-teal-800';
      case PersonalGoalStatus.ReturnedToEmployee:
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getProgressPercentage(totalCurrent: number, totalTarget: number): number {
    if (totalTarget === 0) return 0;
    return Math.min(100, (totalCurrent / totalTarget) * 100);
  }

  getGoalProgressPercentage(goal: PersonalGoalListDto): number {
    return goal.progressPercent || 0;
  }

  getSetProgressPercentage(goalSet: PersonalGoalSetDto): number {
    return goalSet.progressPercent || 0;
  }

  getProgressBarClass(percentage: number): string {
    if (percentage >= 75) return 'bg-green-600';
    if (percentage >= 50) return 'bg-blue-600';
    if (percentage >= 25) return 'bg-yellow-600';
    return 'bg-red-600';
  }

  isOverdue(dueDate: Date | string): boolean {
    const due = new Date(dueDate);
    return due < new Date();
  }

  getGoalCountByStatus(status: PersonalGoalStatus | 'all'): number {
    if (status === 'all') {
      return this.goalSets.reduce((sum, set) => sum + set.goalCount, 0);
    }
    return this.goalSets.filter(s => s.status === status).reduce((sum, set) => sum + set.goalCount, 0);
  }

  getGoalPeriodLabel(startDate: Date | string, dueDate: Date | string): string {
    const start = new Date(startDate);
    const due = new Date(dueDate);
    const monthsDiff = Math.round((due.getTime() - start.getTime()) / (1000 * 60 * 60 * 24 * 30));
    return `${monthsDiff}-month goal period`;
  }

  getGoalSetDisplayName(goalSet: PersonalGoalSetDto): string {
    return this.goalSetDisplayNames[goalSet.goalSetId] || `${new Date(goalSet.startDate).getFullYear()} - Evaluation Cycle 01`;
  }

  submitGoalSetForEvaluation(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    
    if (!goalSet.canSubmitForEvaluation) {
      this.showToast('error', 'This goal set cannot be submitted yet. All goals must be 100% complete.');
      return;
    }

    if (!confirm(`Are you sure you want to submit this goal set for evaluation?\n\nCycle: ${this.getGoalSetDisplayName(goalSet)}\nProgress: ${goalSet.progressPercent.toFixed(0)}%\n\nOnce submitted, your supervisor will be notified and you won't be able to edit these goals.`)) {
      return;
    }

    this.submittingGoalSetId = goalSet.goalSetId;

    this.evaluationService.submitGoalSetForEvaluation(goalSet.goalSetId).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal set submitted for evaluation successfully!');
        this.submittingGoalSetId = null;
        this.loadGoals(); // Refresh the list
      },
      error: (err) => {
        this.submittingGoalSetId = null;
        const errorMessage = err.error?.message || err.error || 'Failed to submit goal set for evaluation. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error submitting goal set:', err);
      }
    });
  }

  isSubmitting(goalSetId: string): boolean {
    return this.submittingGoalSetId === goalSetId;
  }

  getTimelineProgress(evaluationInfo: any): number {
    if (!evaluationInfo || !evaluationInfo.approvalSteps) return 0;
    
    const totalSteps = evaluationInfo.approvalSteps.length;
    if (totalSteps === 0) return 0;
    
    const completedSteps = evaluationInfo.approvalSteps.filter((s: any) => s.isCompleted).length;
    return (completedSteps / totalSteps) * 100;
  }

  formatEvaluationStatus(status: string): string {
    return status
      .replace(/_/g, ' ')
      .replace(/\b\w/g, (l: string) => l.toUpperCase());
  }

  getEvaluationStatusClass(status: string): string {
    const normalizedStatus = (status || '').toLowerCase();
    if (normalizedStatus.includes('completed')) return 'text-green-600';
    if (normalizedStatus.includes('reject') || normalizedStatus.includes('return')) return 'text-red-600';
    if (normalizedStatus.includes('pending')) return 'text-blue-600';
    return 'text-gray-700';
  }

  getEvaluationHistory(goalSet: PersonalGoalSetDto): GoalSetApprovalHistoryEventDto[] {
    if (!goalSet.evaluationInfo) return [];

    const chronologicalHistory = goalSet.evaluationInfo.approvalHistory || [];
    if (chronologicalHistory.length > 0) {
      return [...chronologicalHistory].sort((a, b) => {
        const dateDiff = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
        if (dateDiff !== 0) return dateDiff;
        return (a.id || 0) - (b.id || 0);
      });
    }

    // Backward-compatible fallback for older payloads where only approvalSteps exist.
    return (goalSet.evaluationInfo.approvalSteps || []).map((step, index) => ({
      id: index + 1,
      actorUserId: 0,
      actorName: step.actorName || step.role || 'System',
      actorRole: step.role || 'System',
      action: step.action || 'Updated',
      comment: step.comment,
      fromStatus: '',
      toStatus: '',
      createdAt: step.actionDate || goalSet.evaluationInfo!.submittedDate
    }));
  }

  formatHistoryAction(action: string): string {
    if (!action) return 'Updated';

    const normalized = action.toLowerCase();
    if (normalized.includes('employeecompletedallgoals') || normalized.includes('workflowcontinued')) {
      return 'All goals completed and auto-submitted to RM';
    }

    return action
      .replace(/;/g, ' -> ')
      .replace(/_/g, ' ')
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/\b\w/g, (l: string) => l.toUpperCase());
  }

  getHistoryActionClass(action: string): string {
    const normalized = (action || '').toLowerCase();
    if (normalized.includes('reject') || normalized.includes('return')) return 'text-red-600';
    if (normalized.includes('approve') || normalized.includes('complete') || normalized.includes('process')) return 'text-green-600';
    if (normalized.includes('submit') || normalized.includes('assign') || normalized.includes('workflowcontinued')) return 'text-blue-600';
    return 'text-gray-700';
  }

  deleteGoalSet(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    
    // Check if already submitted for evaluation
    if (goalSet.evaluationInfo) {
      this.showToast('error', 'Cannot delete a goal set that has been submitted for evaluation. Please contact your supervisor if you need to make changes.');
      return;
    }

    if (!confirm(`Are you sure you want to delete this entire goal set?\n\nCycle: ${this.getGoalSetDisplayName(goalSet)}\nGoals: ${goalSet.goalCount}\n\nThis action cannot be undone.`)) {
      return;
    }

    this.loading = true;

    this.goalsService.deleteGoalSet(goalSet.goalSetId).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal set deleted successfully!');
        this.loadGoals(); // Refresh the list
      },
      error: (err) => {
        this.loading = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to delete goal set. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error deleting goal set:', err);
      }
    });
  }

  deleteGoal(goal: PersonalGoalListDto, event: Event): void {
    event.stopPropagation();
    
    // Check if part of a goal set that's been submitted
    const goalSet = this.goalSets.find(gs => gs.goalSetId === goal.goalSetId);
    if (goalSet?.evaluationInfo) {
      this.showToast('error', 'Cannot delete a goal that has been submitted for evaluation.');
      return;
    }

    if (!confirm(`Are you sure you want to delete this goal?\n\nTitle: ${goal.title}\nCategory: ${goal.categoryName}\n\nThis action cannot be undone.`)) {
      return;
    }

    this.loading = true;

    this.goalsService.deletePersonalGoal(goal.id).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal deleted successfully!');
        this.loadGoals(); // Refresh the list
      },
      error: (err) => {
        this.loading = false;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to delete goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error deleting goal:', err);
      }
    });
  }

  canDeleteGoalSet(goalSet: PersonalGoalSetDto): boolean {
    return !goalSet.evaluationInfo; // Can only delete if not submitted for evaluation
  }

  /**
   * Check if a goal can be started (approved by RM)
   */
  canStartGoal(goal: PersonalGoalListDto, goalSet: PersonalGoalSetDto): boolean {
    if (goal.status !== PersonalGoalStatus.ApprovedByRM) {
      return false;
    }

    const evaluationStatus = (goalSet.evaluationInfo?.status || '').toLowerCase();
    if (!evaluationStatus) {
      return true;
    }

    if (evaluationStatus.includes('v2_')) {
      return evaluationStatus.includes('v2_active_goals');
    }

    return evaluationStatus.includes('approved_by_rm') || evaluationStatus.includes('approvedbyrm');
  }

  /**
   * Check if a goal can be completed (currently in progress)
   */
  canCompleteGoal(goal: PersonalGoalListDto): boolean {
    return goal.status === PersonalGoalStatus.InProgress;
  }

  /**
   * Check if evaluation is in "Returned to Employee" state
   */
  isReturnedToEmployee(goalSet: PersonalGoalSetDto): boolean {
    if (!goalSet.evaluationInfo) return false;
    const status = goalSet.evaluationInfo.status.toLowerCase();
    return status.includes('returned') || status.includes('rejected');
  }

  /**
   * Get the RM rejection comment if available
   */
  getReturnedComment(goalSet: PersonalGoalSetDto): string | null {
    if (!goalSet.evaluationInfo) return null;

    const fullHistory = this.getEvaluationHistory(goalSet);
    const rejectedEvent = [...fullHistory].reverse().find(
      event => event.comment && event.action?.toLowerCase().includes('reject')
    );
    if (rejectedEvent?.comment) {
      return rejectedEvent.comment;
    }

    const rejectedStep = goalSet.evaluationInfo.approvalSteps?.find(
      step => step.isRejected && step.comment
    );
    return rejectedStep?.comment || null;
  }

  /**
   * Handle start goal button click
   */
  startGoal(goal: PersonalGoalListDto, goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    
    if (!this.canStartGoal(goal, goalSet)) {
      this.showToast('error', 'This goal cannot be started yet. Wait for RM activation approval.');
      return;
    }

    if (!confirm(`Are you sure you want to start working on this goal?\n\nTitle: ${goal.title}\n\nOnce started, you can begin tracking your progress.`)) {
      return;
    }

    this.startingGoalId = goal.id;

    this.evaluationService.startGoal(goal.id).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal started successfully! You can now work on this goal.');
        this.startingGoalId = null;
        this.loadGoalDetailsInline(goal.id, true);
        this.loadGoals(); // Refresh to show updated status
      },
      error: (err) => {
        this.startingGoalId = null;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to start goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error starting goal:', err);
      }
    });
  }

  /**
   * Check if goal is currently being started
   */
  isStarting(goalId: string): boolean {
    return this.startingGoalId === goalId;
  }

  /**
   * Open complete goal modal
   */
  openCompleteModal(goal: PersonalGoalListDto, goalSetId: string, event: Event): void {
    event.stopPropagation();
    
    if (!this.canCompleteGoal(goal)) {
      this.showToast('error', 'This goal cannot be completed. It must be in progress first.');
      return;
    }

    this.completeModalGoal = goal;
    this.completeModalGoalSetId = goalSetId;
    this.completeFormEvidenceUrl = goal.completionEvidenceUrl || '';
    this.completeFormCertificationUrl = goal.completionCertificationUrl || '';
    this.completeFormSummary = goal.completionSummary || '';
    this.completeFormComment = goal.completionComment || '';
    this.completeFormCurrentScore = goal.targetScore; // Default to target score
    this.showCompleteModal = true;
  }

  /**
   * Close complete goal modal
   */
  closeCompleteModal(): void {
    this.showCompleteModal = false;
    this.completeModalGoal = null;
    this.completeModalGoalSetId = null;
    this.completeFormEvidenceUrl = '';
    this.completeFormCertificationUrl = '';
    this.completeFormSummary = '';
    this.completeFormComment = '';
    this.completeFormCurrentScore = null;
  }

  /**
   * Submit complete goal action
   */
  confirmCompleteGoal(): void {
    if (!this.completeModalGoal) return;
    if (!this.completeFormComment.trim()) {
      this.showToast('error', 'Please describe how you achieved this goal before completing it.');
      return;
    }

    const payload: CompleteGoalRequestDto = {};
    
    if (this.completeFormEvidenceUrl.trim()) {
      payload.evidenceUrl = this.completeFormEvidenceUrl.trim();
    }
    if (this.completeFormCertificationUrl.trim()) {
      payload.certificationUrl = this.completeFormCertificationUrl.trim();
    }
    if (this.completeFormSummary.trim()) {
      payload.summary = this.completeFormSummary.trim();
    }
    if (this.completeFormComment.trim()) {
      payload.comment = this.completeFormComment.trim();
    }
    if (this.completeFormCurrentScore !== null && this.completeFormCurrentScore !== this.completeModalGoal.targetScore) {
      payload.currentScore = this.completeFormCurrentScore;
    }

    this.completingGoalId = this.completeModalGoal.id;

    this.evaluationService.completeGoal(this.completeModalGoal.id, payload).subscribe({
      next: (response) => {
        let message = response.message || 'Goal completed successfully!';
        
        // If workflow continued, show special message
        if (response.workflowContinued) {
          message = 'Goal completed! All goals are now complete. The evaluation has been forwarded to RM post-completion review.';
        }
        
        this.showToast('success', message);
        this.completingGoalId = null;
        if (this.completeModalGoal) {
          this.loadGoalDetailsInline(this.completeModalGoal.id, true);
        }
        this.closeCompleteModal();
        this.loadGoals(); // Refresh to show updated status and evaluation progress
      },
      error: (err) => {
        this.completingGoalId = null;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to complete goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error completing goal:', err);
      }
    });
  }

  /**
   * Check if goal is currently being completed
   */
  isCompleting(goalId: string): boolean {
    return this.completingGoalId === goalId;
  }

  /**
   * Navigate to edit goals after RM returned evaluation
   */
  editAndResubmit(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    this.expandedSetIds.add(goalSet.goalSetId);
    if (goalSet.goals.length > 0) {
      const firstGoalId = goalSet.goals[0].id;
      this.expandedGoalIds.add(firstGoalId);
      this.loadGoalDetailsInline(firstGoalId);
    }
  }

  // ===== Workflow v2 activation plan (employee side) =====

  private normalizeEvaluationStatus(goalSet: PersonalGoalSetDto): string {
    return (goalSet.evaluationInfo?.status || '').toLowerCase();
  }

  private buildGoalSetDisplayNames(goalSets: PersonalGoalSetDto[]): Record<string, string> {
    const goalSetsByYear = new Map<number, PersonalGoalSetDto[]>();

    for (const goalSet of goalSets) {
      const year = new Date(goalSet.startDate).getFullYear();
      const setsForYear = goalSetsByYear.get(year) || [];
      setsForYear.push(goalSet);
      goalSetsByYear.set(year, setsForYear);
    }

    const displayNames: Record<string, string> = {};

    for (const [year, setsForYear] of goalSetsByYear.entries()) {
      const orderedSets = [...setsForYear].sort((a, b) => {
        const startDateDiff = new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
        if (startDateDiff !== 0) {
          return startDateDiff;
        }

        return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      });

      orderedSets.forEach((goalSet, index) => {
        displayNames[goalSet.goalSetId] = `${year} - Evaluation Cycle ${String(index + 1).padStart(2, '0')}`;
      });
    }

    return displayNames;
  }

  isEmployeeActivationStage(goalSet: PersonalGoalSetDto): boolean {
    const status = this.normalizeEvaluationStatus(goalSet);
    return status.includes('v2_pending_employee_activation') || status.includes('v2_returned_for_activation');
  }

  isPendingRmActivationReviewStage(goalSet: PersonalGoalSetDto): boolean {
    const status = this.normalizeEvaluationStatus(goalSet);
    return status.includes('v2_pending_rm_activation_review') || status.includes('v2_pending_tl_activation_review');
  }

  getActivationMethod(goal: PersonalGoalListDto): string {
    if (!goal.goalAssignmentId) return '';
    if (this.activationMethods[goal.goalAssignmentId] === undefined) {
      this.activationMethods[goal.goalAssignmentId] = goal.activationMethod || '';
    }
    return this.activationMethods[goal.goalAssignmentId];
  }

  setActivationMethod(goal: PersonalGoalListDto, value: string): void {
    if (!goal.goalAssignmentId) return;
    this.activationMethods[goal.goalAssignmentId] = value;
  }

  getGoalActivationFeedback(goal: PersonalGoalListDto): string | null {
    if (goal.activationComment && goal.activationComment.trim()) {
      return goal.activationComment.trim();
    }
    return null;
  }

  canSubmitActivationPlan(goalSet: PersonalGoalSetDto): boolean {
    if (!this.isEmployeeActivationStage(goalSet)) return false;

    const activationGoals = goalSet.goals.filter(g => !!g.goalAssignmentId);
    if (activationGoals.length === 0) return false;

    return activationGoals.every(goal => this.getActivationMethod(goal).trim().length > 0);
  }

  isSubmittingActivation(goalSetId: string): boolean {
    return this.submittingActivationGoalSetId === goalSetId;
  }

  submitActivationPlan(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();

    if (!this.canSubmitActivationPlan(goalSet) || this.submittingActivationGoalSetId) {
      return;
    }

    const goals = goalSet.goals
      .filter(g => !!g.goalAssignmentId)
      .map(g => ({
        goalAssignmentId: g.goalAssignmentId!,
        method: this.getActivationMethod(g).trim()
      }));

    this.submittingActivationGoalSetId = goalSet.goalSetId;

    this.evaluationService.submitActivationPlan(goalSet.goalSetId, { goals }).subscribe({
      next: () => {
        this.showToast('success', 'Activation plan submitted to RM successfully.');
        this.submittingActivationGoalSetId = null;
        this.loadGoals();
      },
      error: (err) => {
        this.submittingActivationGoalSetId = null;
        const errorMessage = err.error?.error || err.error?.message || 'Failed to submit activation plan.';
        this.showToast('error', errorMessage);
      }
    });
  }

  // ===== Inline goal details + activity management =====

  startEditingGoalScore(goal: PersonalGoalDetailDto, event: Event): void {
    event.stopPropagation();
    this.editingScoreGoalId = goal.id;
    this.tempScore = goal.currentScore;
  }

  cancelEditingGoalScore(): void {
    this.editingScoreGoalId = null;
    this.tempScore = 0;
  }

  saveGoalScore(goalId: string): void {
    const goal = this.goalDetails[goalId];
    if (!goal) return;

    if (this.tempScore < 0 || this.tempScore > goal.targetScore) {
      this.showToast('error', `Score must be between 0 and ${goal.targetScore}.`);
      return;
    }

    this.goalsService.updateGoalScore(goalId, { currentScore: this.tempScore }).subscribe({
      next: () => {
        this.showToast('success', 'Goal score updated.');
        this.cancelEditingGoalScore();
        this.loadGoalDetailsInline(goalId, true);
        this.loadGoals();
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to update score.';
        this.showToast('error', errorMessage);
      }
    });
  }

  getCompletedActivitiesCount(goal: PersonalGoalDetailDto): number {
    return goal.activities.filter(a => a.status === ActivityStatus.Done).length;
  }

  startAddingActivity(goalId: string, event: Event): void {
    event.stopPropagation();
    this.addingActivityGoalId = goalId;
    this.newActivityDescription = '';
    this.newActivityDueDate = null;
  }

  cancelAddingActivity(): void {
    this.addingActivityGoalId = null;
    this.newActivityDescription = '';
    this.newActivityDueDate = null;
  }

  addActivity(goalId: string): void {
    if (!this.newActivityDescription.trim()) {
      this.showToast('error', 'Please enter an activity description.');
      return;
    }

    const dueDate = this.newActivityDueDate ? new Date(this.newActivityDueDate).toISOString() : undefined;

    this.goalsService.addActivity(goalId, {
      description: this.newActivityDescription.trim(),
      dueDate
    }).subscribe({
      next: () => {
        this.showToast('success', 'Activity added.');
        this.cancelAddingActivity();
        this.loadGoalDetailsInline(goalId, true);
        this.loadGoals();
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to add activity.';
        this.showToast('error', errorMessage);
      }
    });
  }

  startEditingActivity(goalId: string, activity: PersonalGoalActivityDto, event: Event): void {
    event.stopPropagation();
    this.editingActivityGoalId = goalId;
    this.editingActivity = activity;
    this.activityFormData = {
      description: activity.description,
      status: activity.status,
      dueDate: activity.dueDate ? new Date(activity.dueDate).toISOString().split('T')[0] : undefined,
      evidenceUrl: activity.evidenceUrl,
      evidenceNotes: activity.evidenceNotes
    };
  }

  cancelEditingActivity(): void {
    this.editingActivityGoalId = null;
    this.editingActivity = null;
    this.activityFormData = null;
  }

  saveActivity(goalId: string): void {
    if (!this.editingActivity || !this.activityFormData) {
      return;
    }

    const dueDate = this.activityFormData.dueDate
      ? new Date(this.activityFormData.dueDate).toISOString()
      : undefined;

    this.goalsService.updateActivity(goalId, this.editingActivity.id, {
      description: this.activityFormData.description,
      status: this.activityFormData.status,
      dueDate,
      evidenceUrl: this.activityFormData.evidenceUrl || undefined,
      evidenceNotes: this.activityFormData.evidenceNotes || undefined
    }).subscribe({
      next: () => {
        this.showToast('success', 'Activity updated.');
        this.cancelEditingActivity();
        this.loadGoalDetailsInline(goalId, true);
        this.loadGoals();
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to update activity.';
        this.showToast('error', errorMessage);
      }
    });
  }

  quickUpdateActivityStatus(goalId: string, activity: PersonalGoalActivityDto, event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    const newStatus = parseInt(selectElement.value, 10) as ActivityStatus;

    const dueDate = activity.dueDate ? new Date(activity.dueDate).toISOString() : undefined;

    this.goalsService.updateActivity(goalId, activity.id, {
      description: activity.description,
      status: newStatus,
      dueDate,
      evidenceUrl: activity.evidenceUrl || undefined,
      evidenceNotes: activity.evidenceNotes || undefined
    }).subscribe({
      next: () => {
        this.loadGoalDetailsInline(goalId, true);
        this.loadGoals();
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to update activity status.';
        this.showToast('error', errorMessage);
      }
    });
  }

  deleteActivity(goalId: string, activity: PersonalGoalActivityDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Delete this activity?\n\n"${activity.description}"`)) {
      return;
    }

    this.goalsService.deleteActivity(goalId, activity.id).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Activity deleted.');
        this.loadGoalDetailsInline(goalId, true);
        this.loadGoals();
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to delete activity.';
        this.showToast('error', errorMessage);
      }
    });
  }

  canManageInlineGoal(goal: PersonalGoalDetailDto): boolean {
    return goal.status !== PersonalGoalStatus.Completed && goal.status !== PersonalGoalStatus.Cancelled;
  }

  getActivityStatusLabel(status: ActivityStatus): string {
    switch (status) {
      case ActivityStatus.NotStarted:
        return 'Not Started';
      case ActivityStatus.InProgress:
        return 'In Progress';
      case ActivityStatus.Done:
        return 'Done';
      default:
        return 'Unknown';
    }
  }

  getActivityStatusClass(status: ActivityStatus): string {
    switch (status) {
      case ActivityStatus.NotStarted:
        return 'bg-gray-100 text-gray-800';
      case ActivityStatus.InProgress:
        return 'bg-blue-100 text-blue-800';
      case ActivityStatus.Done:
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  private showToast(type: 'success' | 'error', message: string): void {
    // Simple toast implementation - can be replaced with a library like ngx-toastr
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
}
