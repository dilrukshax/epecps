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
    if (!this.goal || this.goal.targetScore === 0) return 0;
    return Math.min(100, (this.goal.currentScore / this.goal.targetScore) * 100);
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

    this.goalsService.updateActivity(
      this.goalId,
      this.editingActivity.id,
      this.activityFormData
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

    const dto: UpdatePersonalGoalActivityDto = {
      description: activity.description,
      status: newStatus,
      dueDate: activity.dueDate,
      evidenceUrl: activity.evidenceUrl,
      evidenceNotes: activity.evidenceNotes
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
}
