import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import { EvaluationService } from '../../../services/evaluation.service';
import { PersonalGoalSetDto, PersonalGoalStatus, PersonalGoalListDto } from '../../../models/employee-goals.models';

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
  loading = false;
  error: string | null = null;
  submittingGoalSetId: string | null = null;

  // Filter state
  selectedStatus: PersonalGoalStatus | 'all' = 'all';
  searchQuery = '';

  // Expanded goal sets
  expandedSetIds: Set<string> = new Set();

  // Reference to enum for template
  PersonalGoalStatus = PersonalGoalStatus;

  constructor(
    private goalsService: EmployeeGoalsService,
    private evaluationService: EvaluationService,
    private router: Router
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
        goalSet.templateName.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
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

  viewGoalDetails(goalId: string, event: Event): void {
    event.stopPropagation(); // Prevent card expansion toggle
    this.router.navigate(['/employee/goals', goalId]);
  }

  createNewGoal(): void {
    this.router.navigate(['/employee/goals/new']);
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

  submitGoalSetForEvaluation(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    
    if (!goalSet.canSubmitForEvaluation) {
      this.showToast('error', 'This goal set cannot be submitted yet. All goals must be 100% complete.');
      return;
    }

    if (!confirm(`Are you sure you want to submit this goal set for evaluation?\n\nTemplate: ${goalSet.templateName}\nProgress: ${goalSet.progressPercent.toFixed(0)}%\n\nOnce submitted, your supervisor will be notified and you won't be able to edit these goals.`)) {
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

  deleteGoalSet(goalSet: PersonalGoalSetDto, event: Event): void {
    event.stopPropagation();
    
    // Check if already submitted for evaluation
    if (goalSet.evaluationInfo) {
      this.showToast('error', 'Cannot delete a goal set that has been submitted for evaluation. Please contact your supervisor if you need to make changes.');
      return;
    }

    if (!confirm(`Are you sure you want to delete this entire goal set?\n\nTemplate: ${goalSet.templateName}\nGoals: ${goalSet.goalCount}\n\nThis action cannot be undone.`)) {
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
