import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import { PersonalGoalSetDto, PersonalGoalStatus } from '../../../models/employee-goals.models';

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

  // Filter state
  selectedStatus: PersonalGoalStatus | 'all' = 'all';
  searchQuery = '';

  // Expanded goal sets
  expandedSetIds: Set<string> = new Set();

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
}
