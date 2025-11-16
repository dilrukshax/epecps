import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import {
  GoalFrameworkCategoryDto,
  GoalFrameworkItemDto,
  GoalFrameworkGoalItemDto,
  GoalItemSelection,
  CreatePersonalGoalDto
} from '../../../models/employee-goals.models';

/**
 * Multi-step wizard for creating new personal goals
 */
@Component({
  selector: 'app-set-new-goal',
  templateUrl: './set-new-goal.component.html',
  styleUrls: ['./set-new-goal.component.css'],
  standalone: false
})
export class SetNewGoalComponent implements OnInit {
  currentStep = 1;
  loading = false;
  error: string | null = null;

  // Step 1 - Categories
  categories: GoalFrameworkCategoryDto[] = [];
  selectedCategory: GoalFrameworkCategoryDto | null = null;

  // Step 2 - Items
  items: GoalFrameworkItemDto[] = [];
  selectedItem: GoalFrameworkItemDto | null = null;

  // Step 3 - Goal Items
  goalItems: GoalFrameworkGoalItemDto[] = [];
  selectedGoalItems: Set<string> = new Set();

  // Step 4 - Configure details
  goalConfigurations: Map<string, GoalItemSelection> = new Map();
  currentConfigIndex = 0;

  constructor(
    private goalsService: EmployeeGoalsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  // ===========================
  // Step 1: Choose Category
  // ===========================

  loadCategories(): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load categories. Please try again.';
        this.loading = false;
        console.error('Error loading categories:', err);
      }
    });
  }

  selectCategory(category: GoalFrameworkCategoryDto): void {
    this.selectedCategory = category;
    this.loadItems(category.id);
    this.currentStep = 2;
  }

  // ===========================
  // Step 2: Choose Item
  // ===========================

  loadItems(categoryId: string): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getItemsByCategory(categoryId).subscribe({
      next: (items) => {
        this.items = items;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load items. Please try again.';
        this.loading = false;
        console.error('Error loading items:', err);
      }
    });
  }

  selectItem(item: GoalFrameworkItemDto): void {
    this.selectedItem = item;
    this.loadGoalItems(item.id);
    this.currentStep = 3;
  }

  // ===========================
  // Step 3: Choose Goal Items
  // ===========================

  loadGoalItems(itemId: string): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getGoalItemsByItem(itemId).subscribe({
      next: (goalItems) => {
        this.goalItems = goalItems;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load goal items. Please try again.';
        this.loading = false;
        console.error('Error loading goal items:', err);
      }
    });
  }

  toggleGoalItemSelection(goalItemId: string): void {
    if (this.selectedGoalItems.has(goalItemId)) {
      this.selectedGoalItems.delete(goalItemId);
      this.goalConfigurations.delete(goalItemId);
    } else {
      this.selectedGoalItems.add(goalItemId);
      const goalItem = this.goalItems.find(gi => gi.id === goalItemId);
      if (goalItem) {
        this.initializeGoalConfiguration(goalItem);
      }
    }
  }

  isGoalItemSelected(goalItemId: string): boolean {
    return this.selectedGoalItems.has(goalItemId);
  }

  proceedToConfiguration(): void {
    if (this.selectedGoalItems.size === 0) {
      alert('Please select at least one goal item.');
      return;
    }
    this.currentConfigIndex = 0;
    this.currentStep = 4;
  }

  // ===========================
  // Step 4: Configure Details
  // ===========================

  initializeGoalConfiguration(goalItem: GoalFrameworkGoalItemDto): void {
    const defaultStartDate = new Date();
    const defaultDueDate = new Date();
    defaultDueDate.setMonth(defaultDueDate.getMonth() + 3); // 3 months from now

    this.goalConfigurations.set(goalItem.id, {
      goalItem: goalItem,
      title: goalItem.name,
      description: goalItem.description || '',
      startDate: defaultStartDate,
      dueDate: defaultDueDate,
      selectedSuggestedActivityIds: [], // Keep empty - not used anymore
      customActivities: []
    });
  }

  getCurrentConfiguration(): GoalItemSelection | null {
    const goalItemIds = Array.from(this.selectedGoalItems);
    if (goalItemIds.length === 0 || this.currentConfigIndex >= goalItemIds.length) {
      return null;
    }
    const goalItemId = goalItemIds[this.currentConfigIndex];
    return this.goalConfigurations.get(goalItemId) || null;
  }

  addCustomActivity(description: string): void {
    const config = this.getCurrentConfiguration();
    if (!config || !description.trim()) return;

    config.customActivities.push(description.trim());
  }

  removeCustomActivity(index: number): void {
    const config = this.getCurrentConfiguration();
    if (!config) return;

    config.customActivities.splice(index, 1);
  }

  canGoNext(): boolean {
    return this.currentConfigIndex < this.selectedGoalItems.size - 1;
  }

  canGoPrevious(): boolean {
    return this.currentConfigIndex > 0;
  }

  nextConfiguration(): void {
    if (this.canGoNext()) {
      this.currentConfigIndex++;
    }
  }

  previousConfiguration(): void {
    if (this.canGoPrevious()) {
      this.currentConfigIndex--;
    }
  }

  // ===========================
  // Create Goals
  // ===========================

  async createGoals(): Promise<void> {
    this.loading = true;
    this.error = null;

    const configurations = Array.from(this.goalConfigurations.values());
    const createdGoals: string[] = [];

    try {
      for (const config of configurations) {
        // Convert dates to ISO string format (handle both Date objects and string inputs)
        const startDateStr = config.startDate instanceof Date 
          ? config.startDate.toISOString() 
          : new Date(config.startDate).toISOString();
        
        const dueDateStr = config.dueDate instanceof Date 
          ? config.dueDate.toISOString() 
          : new Date(config.dueDate).toISOString();

        const dto: CreatePersonalGoalDto = {
          goalItemId: config.goalItem.id,
          title: config.title,
          description: config.description,
          startDate: startDateStr,
          dueDate: dueDateStr,
          selectedSuggestedActivityIds: [], // Always empty now
          customActivities: config.customActivities
        };

        const result = await this.goalsService.createPersonalGoal(dto).toPromise();
        if (result && result.id) {
          createdGoals.push(result.id);
        }
      }

      this.loading = false;
      alert(`Successfully created ${createdGoals.length} goal(s)!`);
      this.router.navigate(['/employee/goals']);
    } catch (err) {
      this.loading = false;
      this.error = 'Failed to create goals. Please try again.';
      console.error('Error creating goals:', err);
    }
  }

  // ===========================
  // Navigation
  // ===========================

  goBack(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.error = null;
    } else {
      this.router.navigate(['/employee/goals']);
    }
  }

  cancel(): void {
    if (confirm('Are you sure you want to cancel? All progress will be lost.')) {
      this.router.navigate(['/employee/goals']);
    }
  }
}
