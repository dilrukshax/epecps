import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import { PersonalGoalListDto, PersonalGoalStatus } from '../../../models/employee-goals.models';

/**
 * Component for displaying all personal goals for the current user
 */
@Component({
  selector: 'app-my-goals',
  templateUrl: './my-goals.component.html',
  styleUrls: ['./my-goals.component.css'],
  standalone: false
})
export class MyGoalsComponent implements OnInit {
  goals: PersonalGoalListDto[] = [];
  filteredGoals: PersonalGoalListDto[] = [];
  loading = false;
  error: string | null = null;

  // Filter state
  selectedStatus: PersonalGoalStatus | 'all' = 'all';
  searchQuery = '';

  // Reference to enum for template
  PersonalGoalStatus = PersonalGoalStatus;

  constructor(
    private goalsService: EmployeeGoalsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadGoals();
  }

  loadGoals(): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getMyGoals().subscribe({
      next: (goals) => {
        this.goals = goals;
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
    this.filteredGoals = this.goals.filter(goal => {
      // Status filter
      const statusMatch = this.selectedStatus === 'all' || goal.status === this.selectedStatus;

      // Search filter
      const searchMatch = !this.searchQuery || 
        goal.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        goal.categoryName.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        goal.goalItemName.toLowerCase().includes(this.searchQuery.toLowerCase());

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

  viewGoalDetails(goalId: string): void {
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

  getProgressPercentage(goal: PersonalGoalListDto): number {
    if (goal.targetScore === 0) return 0;
    return Math.min(100, (goal.currentScore / goal.targetScore) * 100);
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
    if (status === 'all') return this.goals.length;
    return this.goals.filter(g => g.status === status).length;
  }
}
