import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import {
  RmGoalAssignmentService,
  GoalLibraryItemDto,
  RmEmployeeDto,
  AssignGoalItemDto,
  RmAssignGoalsDto,
  GoalAssignmentListDto
} from '../../../services/rm-goal-assignment.service';

@Component({
  selector: 'app-rm-assign-goals',
  templateUrl: './rm-assign-goals.component.html',
  styleUrls: ['./rm-assign-goals.component.css'],
  standalone: false
})
export class RmAssignGoalsComponent implements OnInit {
  // Active tab
  activeTab: 'assign' | 'history' = 'assign';

  // Step management for assign tab
  currentStep = 1; // 1: Select Employee, 2: Select Goals, 3: Configure & Confirm

  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Step 1: Employee selection
  employees: RmEmployeeDto[] = [];
  filteredEmployees: RmEmployeeDto[] = [];
  employeeSearchQuery = '';
  selectedEmployee: RmEmployeeDto | null = null;

  // Step 2: Goal library
  goalLibrary: GoalLibraryItemDto[] = [];
  filteredGoalLibrary: GoalLibraryItemDto[] = [];
  goalSearchQuery = '';
  selectedCategoryFilter = 'all';
  categories: string[] = [];
  selectedGoalIds: Set<string> = new Set();

  // Step 3: Configuration
  goalPeriodMonths = 6;
  goalStartDate = '';
  goalEndDate = '';
  goalConfigurations: Map<string, { title: string; description: string; customActivities: string[] }> = new Map();
  newActivityText = '';

  // History tab
  assignments: GoalAssignmentListDto[] = [];
  filteredAssignments: GoalAssignmentListDto[] = [];
  assignmentSearchQuery = '';

  constructor(
    private rmService: RmGoalAssignmentService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.calculateGoalPeriod();
    this.loadEmployees();
    this.loadGoalLibrary();
  }

  // ===========================
  // Tab management
  // ===========================

  switchTab(tab: 'assign' | 'history'): void {
    this.activeTab = tab;
    this.error = null;
    this.successMessage = null;
    if (tab === 'history' && this.assignments.length === 0) {
      this.loadAssignments();
    }
  }

  // ===========================
  // Goal Period
  // ===========================

  calculateGoalPeriod(): void {
    const start = new Date();
    const end = new Date();
    end.setMonth(end.getMonth() + this.goalPeriodMonths);
    this.goalStartDate = this.formatDate(start);
    this.goalEndDate = this.formatDate(end);
  }

  onPeriodChange(): void {
    this.calculateGoalPeriod();
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // ===========================
  // Step 1: Select Employee
  // ===========================

  loadEmployees(): void {
    this.loading = true;
    this.rmService.getMyEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
        this.filteredEmployees = employees;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load employees. Please try again.';
        this.loading = false;
        console.error('Error loading employees:', err);
      }
    });
  }

  filterEmployees(): void {
    const query = this.employeeSearchQuery.toLowerCase();
    this.filteredEmployees = this.employees.filter(e =>
      e.fullName.toLowerCase().includes(query) ||
      e.email.toLowerCase().includes(query) ||
      e.department.toLowerCase().includes(query)
    );
  }

  selectEmployee(employee: RmEmployeeDto): void {
    this.selectedEmployee = employee;
    this.currentStep = 2;
    this.error = null;
  }

  // ===========================
  // Step 2: Select Goals from Library
  // ===========================

  loadGoalLibrary(): void {
    this.rmService.getGoalLibrary().subscribe({
      next: (goals) => {
        this.goalLibrary = goals;
        this.filteredGoalLibrary = goals;
        this.categories = [...new Set(goals.map(g => g.categoryName))].sort();
      },
      error: (err) => {
        console.error('Error loading goal library:', err);
      }
    });
  }

  filterGoals(): void {
    const query = this.goalSearchQuery.toLowerCase();
    this.filteredGoalLibrary = this.goalLibrary.filter(g => {
      const matchesSearch = !query ||
        g.name.toLowerCase().includes(query) ||
        (g.description || '').toLowerCase().includes(query) ||
        g.categoryName.toLowerCase().includes(query);
      const matchesCategory = this.selectedCategoryFilter === 'all' || g.categoryName === this.selectedCategoryFilter;
      return matchesSearch && matchesCategory;
    });
  }

  onCategoryFilterChange(): void {
    this.filterGoals();
  }

  toggleGoalSelection(goalId: string): void {
    if (this.selectedGoalIds.has(goalId)) {
      this.selectedGoalIds.delete(goalId);
      this.goalConfigurations.delete(goalId);
    } else {
      this.selectedGoalIds.add(goalId);
      const goal = this.goalLibrary.find(g => g.id === goalId);
      if (goal) {
        this.goalConfigurations.set(goalId, {
          title: goal.name,
          description: goal.description || '',
          customActivities: []
        });
      }
    }
  }

  isGoalSelected(goalId: string): boolean {
    return this.selectedGoalIds.has(goalId);
  }

  getSelectedGoals(): GoalLibraryItemDto[] {
    return this.goalLibrary.filter(g => this.selectedGoalIds.has(g.id));
  }

  proceedToConfiguration(): void {
    if (this.selectedGoalIds.size === 0) {
      this.error = 'Please select at least one goal to assign.';
      return;
    }
    this.error = null;
    this.currentStep = 3;
  }

  // ===========================
  // Step 3: Configure & Confirm
  // ===========================

  getGoalConfig(goalId: string): { title: string; description: string; customActivities: string[] } | undefined {
    return this.goalConfigurations.get(goalId);
  }

  addCustomActivity(goalId: string): void {
    if (!this.newActivityText.trim()) return;
    const config = this.goalConfigurations.get(goalId);
    if (config) {
      config.customActivities.push(this.newActivityText.trim());
      this.newActivityText = '';
    }
  }

  removeCustomActivity(goalId: string, index: number): void {
    const config = this.goalConfigurations.get(goalId);
    if (config) {
      config.customActivities.splice(index, 1);
    }
  }

  // ===========================
  // Submit Assignment
  // ===========================

  async assignGoals(): Promise<void> {
    if (!this.selectedEmployee) return;

    this.loading = true;
    this.error = null;
    this.successMessage = null;

    const goals: AssignGoalItemDto[] = [];
    for (const goalId of this.selectedGoalIds) {
      const config = this.goalConfigurations.get(goalId);
      goals.push({
        goalItemId: goalId,
        title: config?.title || undefined,
        description: config?.description || undefined,
        customActivities: config?.customActivities || []
      });
    }

    const dto: RmAssignGoalsDto = {
      employeeUserId: this.selectedEmployee.userId,
      goalPeriodMonths: this.goalPeriodMonths,
      startDate: new Date(this.goalStartDate + 'T00:00:00').toISOString(),
      dueDate: new Date(this.goalEndDate + 'T00:00:00').toISOString(),
      goals: goals
    };

    this.rmService.assignGoals(dto).subscribe({
      next: (response) => {
        this.loading = false;
        this.successMessage = response.message;
        // Reset the form
        this.resetForm();
        // Switch to history tab
        this.switchTab('history');
        this.loadAssignments();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.error || err.error?.message || err.message || 'Failed to assign goals. Please try again.';
        console.error('Error assigning goals:', err);
        console.error('Error body:', JSON.stringify(err.error));
      }
    });
  }

  resetForm(): void {
    this.currentStep = 1;
    this.selectedEmployee = null;
    this.selectedGoalIds.clear();
    this.goalConfigurations.clear();
    this.employeeSearchQuery = '';
    this.goalSearchQuery = '';
    this.selectedCategoryFilter = 'all';
    this.filterEmployees();
    this.filterGoals();
  }

  // ===========================
  // History tab
  // ===========================

  loadAssignments(): void {
    this.loading = true;
    this.rmService.getMyAssignments().subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        this.filteredAssignments = assignments;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load assignments.';
        this.loading = false;
        console.error('Error loading assignments:', err);
      }
    });
  }

  filterAssignments(): void {
    const query = this.assignmentSearchQuery.toLowerCase();
    this.filteredAssignments = this.assignments.filter(a =>
      a.employeeName.toLowerCase().includes(query) ||
      a.title.toLowerCase().includes(query) ||
      a.categoryName.toLowerCase().includes(query)
    );
  }

  // ===========================
  // Navigation
  // ===========================

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.error = null;
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
