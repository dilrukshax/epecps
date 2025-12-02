import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import {
  PersonalGoalDetailDto,
  PersonalGoalActivityDto,
  ActivityStatus,
  PersonalGoalStatus,
  UpdatePersonalGoalActivityDto,
  UpdatePersonalGoalScoreDto
} from '../../../models/employee-goals.models';

/**
 * Component for viewing and managing a single personal goal
 */
@Component({
  selector: 'app-goal-details',
  templateUrl: './goal-details.component.html',
  styleUrls: ['./goal-details.component.css'],
  standalone: false
})
export class GoalDetailsComponent implements OnInit {
  goalId: string | null = null;
  goal: PersonalGoalDetailDto | null = null;
  loading = false;
  error: string | null = null;

  // Edit modes
  editingScore = false;
  tempScore = 0;

  addingActivity = false;
  newActivityDescription = '';
  newActivityDueDate: string | null = null;

  editingActivity: PersonalGoalActivityDto | null = null;
  activityFormData: UpdatePersonalGoalActivityDto | null = null;

  // Reference to enums for template
  ActivityStatus = ActivityStatus;
  PersonalGoalStatus = PersonalGoalStatus;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private goalsService: EmployeeGoalsService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.goalId = params['id'];
      if (this.goalId) {
        this.loadGoalDetails();
      }
    });
  }

  loadGoalDetails(): void {
    if (!this.goalId) return;

    this.loading = true;
    this.error = null;

    this.goalsService.getGoalDetails(this.goalId).subscribe({
      next: (goal) => {
        this.goal = goal;
        this.tempScore = goal.currentScore;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load goal details. Please try again.';
        this.loading = false;
        console.error('Error loading goal:', err);
      }
    });
  }

  // ===========================
  // Score Management
  // ===========================

  startEditingScore(): void {
    if (!this.goal) return;
    this.editingScore = true;
    this.tempScore = this.goal.currentScore;
  }

  cancelEditingScore(): void {
    this.editingScore = false;
    if (this.goal) {
      this.tempScore = this.goal.currentScore;
    }
  }

  saveScore(): void {
    if (!this.goal || !this.goalId) return;

    if (this.tempScore < 0 || this.tempScore > this.goal.targetScore) {
      alert(`Score must be between 0 and ${this.goal.targetScore}`);
      return;
    }

    const dto: UpdatePersonalGoalScoreDto = {
      currentScore: this.tempScore
    };

    this.goalsService.updateGoalScore(this.goalId, dto).subscribe({
      next: () => {
        this.editingScore = false;
        this.loadGoalDetails();
      },
      error: (err) => {
        alert('Failed to update score. Please try again.');
        console.error('Error updating score:', err);
      }
    });
  }

  getProgressPercentage(): number {
    if (!this.goal) return 0;
    return this.goal.progressPercent || 0;
  }

  getActivityProgressPercentage(): number {
    if (!this.goal || this.goal.activities.length === 0) return 0;
    const completed = this.getCompletedActivitiesCount();
    const total = this.goal.activities.length;
    return Math.round((completed / total) * 100);
  }

  recalculateScore(): void {
    if (!this.goalId) return;

    if (!confirm('This will recalculate the goal score based on completed activities. Continue?')) {
      return;
    }

    this.goalsService.recalculateGoalScore(this.goalId).subscribe({
      next: () => {
        this.loadGoalDetails();
        alert('Score recalculated successfully!');
      },
      error: (err) => {
        alert('Failed to recalculate score. Please try again.');
        console.error('Error recalculating score:', err);
      }
    });
  }

  // ===========================
  // Activity Management
  // ===========================

  startAddingActivity(): void {
    this.addingActivity = true;
    this.newActivityDescription = '';
    this.newActivityDueDate = null;
  }

  cancelAddingActivity(): void {
    this.addingActivity = false;
    this.newActivityDescription = '';
    this.newActivityDueDate = null;
  }

  addActivity(): void {
    if (!this.goalId || !this.newActivityDescription.trim()) {
      alert('Please enter an activity description');
      return;
    }

    const dto = {
      description: this.newActivityDescription.trim(),
      dueDate: this.newActivityDueDate ? new Date(this.newActivityDueDate).toISOString() : undefined
    };

    this.goalsService.addActivity(this.goalId, dto).subscribe({
      next: () => {
        this.cancelAddingActivity();
        this.loadGoalDetails();
      },
      error: (err) => {
        alert('Failed to add activity. Please try again.');
        console.error('Error adding activity:', err);
      }
    });
  }

  startEditingActivity(activity: PersonalGoalActivityDto): void {
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
    this.editingActivity = null;
    this.activityFormData = null;
  }

  saveActivity(): void {
    if (!this.goalId || !this.editingActivity || !this.activityFormData) return;

    // Properly convert dueDate to ISO string or undefined
    let dueDate: string | undefined = undefined;
    if (this.activityFormData.dueDate) {
      try {
        dueDate = new Date(this.activityFormData.dueDate).toISOString();
      } catch (e) {
        console.error('Invalid date format:', this.activityFormData.dueDate);
        dueDate = undefined;
      }
    }

    const dto: UpdatePersonalGoalActivityDto = {
      description: this.activityFormData.description,
      status: this.activityFormData.status,
      dueDate: dueDate,
      evidenceUrl: this.activityFormData.evidenceUrl || undefined,
      evidenceNotes: this.activityFormData.evidenceNotes || undefined
    };

    this.goalsService.updateActivity(
      this.goalId,
      this.editingActivity.id,
      dto
    ).subscribe({
      next: () => {
        this.cancelEditingActivity();
        this.loadGoalDetails();
      },
      error: (err) => {
        alert('Failed to update activity. Please try again.');
        console.error('Error updating activity:', err);
      }
    });
  }

  quickUpdateActivityStatus(activity: PersonalGoalActivityDto, newStatus: ActivityStatus): void {
    if (!this.goalId) return;

    // Properly convert dueDate to ISO string or undefined
    let dueDate: string | undefined = undefined;
    if (activity.dueDate) {
      try {
        dueDate = new Date(activity.dueDate).toISOString();
      } catch (e) {
        console.error('Invalid date format:', activity.dueDate);
        dueDate = undefined;
      }
    }

    const dto: UpdatePersonalGoalActivityDto = {
      description: activity.description,
      status: newStatus,
      dueDate: dueDate,
      evidenceUrl: activity.evidenceUrl || undefined,
      evidenceNotes: activity.evidenceNotes || undefined
    };

    this.goalsService.updateActivity(this.goalId, activity.id, dto).subscribe({
      next: () => {
        this.loadGoalDetails();
      },
      error: (err) => {
        alert('Failed to update activity status. Please try again.');
        console.error('Error updating activity:', err);
      }
    });
  }

  onStatusChange(event: Event, activity: PersonalGoalActivityDto): void {
    const selectElement = event.target as HTMLSelectElement;
    const newStatus = parseInt(selectElement.value, 10) as ActivityStatus;
    this.quickUpdateActivityStatus(activity, newStatus);
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

  getCompletedActivitiesCount(): number {
    if (!this.goal) return 0;
    return this.goal.activities.filter(a => a.status === ActivityStatus.Done).length;
  }

  // ===========================
  // Navigation
  // ===========================

  goBack(): void {
    this.router.navigate(['/employee/goals']);
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

  // ===========================
  // Delete Operations
  // ===========================

  deleteActivity(activity: PersonalGoalActivityDto): void {
    if (!this.goalId) return;

    if (!confirm(`Are you sure you want to delete this activity?\n\n"${activity.description}"\n\nThis action cannot be undone.`)) {
      return;
    }

    this.goalsService.deleteActivity(this.goalId, activity.id).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Activity deleted successfully!');
        this.loadGoalDetails(); // Refresh the goal
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to delete activity. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error deleting activity:', err);
      }
    });
  }

  deleteGoal(): void {
    if (!this.goalId || !this.goal) return;

    if (!confirm(`Are you sure you want to delete this goal?\n\nTitle: ${this.goal.title}\n\nThis action cannot be undone and you will be returned to the goals list.`)) {
      return;
    }

    this.goalsService.deletePersonalGoal(this.goalId).subscribe({
      next: (response) => {
        this.showToast('success', response.message || 'Goal deleted successfully!');
        setTimeout(() => {
          this.router.navigate(['/employee/goals']);
        }, 1000);
      },
      error: (err) => {
        const errorMessage = err.error?.error || err.error?.message || 'Failed to delete goal. Please try again.';
        this.showToast('error', errorMessage);
        console.error('Error deleting goal:', err);
      }
    });
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
