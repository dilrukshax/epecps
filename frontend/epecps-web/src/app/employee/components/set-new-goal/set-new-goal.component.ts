import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EmployeeGoalsService } from '../../../services/employee-goals.service';
import {
  GoalFrameworkTemplateDto,
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

  // Expose Math to template
  Math = Math;

  // Goal Period (6 months by default)
  goalPeriodMonths = 6;
  goalStartDate: Date = new Date();
  goalEndDate: Date = new Date();

  // Step 1 - Select Template
  templates: GoalFrameworkTemplateDto[] = [];
  selectedTemplate: GoalFrameworkTemplateDto | null = null;

  // Step 2 - Select Categories
  categories: GoalFrameworkCategoryDto[] = [];
  selectedCategoryIds: Set<string> = new Set();

  // Step 3 - Select Items
  allItems: Map<string, GoalFrameworkItemDto[]> = new Map();
  selectedItemIds: Set<string> = new Set();

  // Step 4 - Select Goal Items
  allGoalItems: Map<string, GoalFrameworkGoalItemDto[]> = new Map();
  selectedGoalItems: Map<string, GoalFrameworkGoalItemDto> = new Map();
  totalTargetScore = 0;
  minRequiredScore = 100;

  // Step 5 - Configure details
  goalConfigurations: Map<string, GoalItemSelection> = new Map();
  currentConfigIndex = 0;

  constructor(
    private goalsService: EmployeeGoalsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.calculateGoalPeriod();
    this.loadTemplates();
  }

  // ===========================
  // Goal Period Management
  // ===========================

  calculateGoalPeriod(): void {
    this.goalStartDate = new Date();
    this.goalEndDate = new Date();
    this.goalEndDate.setMonth(this.goalEndDate.getMonth() + this.goalPeriodMonths);
  }

  onPeriodChange(): void {
    this.calculateGoalPeriod();
    // Update all existing configurations with new dates
    this.goalConfigurations.forEach(config => {
      config.startDate = this.formatDateForInput(this.goalStartDate);
      config.dueDate = this.formatDateForInput(this.goalEndDate);
    });
  }

  // Helper method to format Date to yyyy-MM-dd for HTML input
  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // Helper method to parse yyyy-MM-dd string to Date
  private parseDateFromInput(dateString: string): Date {
    return new Date(dateString + 'T00:00:00');
  }

  // ===========================
  // Step 1: Select Template
  // ===========================

  loadTemplates(): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getTemplates().subscribe({
      next: (templates) => {
        this.templates = templates;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load templates. Please try again.';
        this.loading = false;
        console.error('Error loading templates:', err);
      }
    });
  }

  selectTemplate(template: GoalFrameworkTemplateDto): void {
    this.selectedTemplate = template;
    this.loadCategories(template.id);
    this.currentStep = 2;
  }

  // ===========================
  // Step 2: Select Categories
  // ===========================

  loadCategories(templateId: string): void {
    this.loading = true;
    this.error = null;

    this.goalsService.getCategoriesByTemplate(templateId).subscribe({
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

  toggleCategorySelection(categoryId: string): void {
    if (this.selectedCategoryIds.has(categoryId)) {
      this.selectedCategoryIds.delete(categoryId);
      // Remove items from this category
      const itemsToRemove = this.allItems.get(categoryId);
      if (itemsToRemove) {
        itemsToRemove.forEach(item => {
          this.selectedItemIds.delete(item.id);
          // Remove goal items as well
          const goalItems = this.allGoalItems.get(item.id);
          if (goalItems) {
            goalItems.forEach(gi => {
              this.selectedGoalItems.delete(gi.id);
              this.goalConfigurations.delete(gi.id);
            });
          }
        });
      }
    } else {
      this.selectedCategoryIds.add(categoryId);
    }
    this.recalculateTotalScore();
  }

  isCategorySelected(categoryId: string): boolean {
    return this.selectedCategoryIds.has(categoryId);
  }

  proceedToItemSelection(): void {
    if (this.selectedCategoryIds.size === 0) {
      alert('Please select at least one category.');
      return;
    }
    // Load items for all selected categories
    this.loadItemsForSelectedCategories();
    this.currentStep = 3;
  }

  // ===========================
  // Step 3: Select Items
  // ===========================

  loadItemsForSelectedCategories(): void {
    this.loading = true;
    this.error = null;
    let loadedCount = 0;
    const totalToLoad = this.selectedCategoryIds.size;

    this.selectedCategoryIds.forEach(categoryId => {
      this.goalsService.getItemsByCategory(categoryId).subscribe({
        next: (items) => {
          this.allItems.set(categoryId, items);
          loadedCount++;
          if (loadedCount === totalToLoad) {
            this.loading = false;
          }
        },
        error: (err) => {
          this.error = 'Failed to load items. Please try again.';
          this.loading = false;
          console.error('Error loading items:', err);
        }
      });
    });
  }

  getItemsForDisplay(): GoalFrameworkItemDto[] {
    const items: GoalFrameworkItemDto[] = [];
    this.selectedCategoryIds.forEach(categoryId => {
      const categoryItems = this.allItems.get(categoryId);
      if (categoryItems) {
        items.push(...categoryItems);
      }
    });
    return items;
  }

  getCategoryName(item: GoalFrameworkItemDto): string {
    // Find category name by looking up items
    for (const category of this.categories) {
      const categoryItems = this.allItems.get(category.id);
      if (categoryItems && categoryItems.find(i => i.id === item.id)) {
        return category.name;
      }
    }
    return '';
  }

  toggleItemSelection(itemId: string): void {
    if (this.selectedItemIds.has(itemId)) {
      this.selectedItemIds.delete(itemId);
      // Remove goal items for this item
      const goalItems = this.allGoalItems.get(itemId);
      if (goalItems) {
        goalItems.forEach(gi => {
          this.selectedGoalItems.delete(gi.id);
          this.goalConfigurations.delete(gi.id);
        });
      }
    } else {
      this.selectedItemIds.add(itemId);
    }
    this.recalculateTotalScore();
  }

  isItemSelected(itemId: string): boolean {
    return this.selectedItemIds.has(itemId);
  }

  proceedToGoalItemSelection(): void {
    if (this.selectedItemIds.size === 0) {
      alert('Please select at least one item.');
      return;
    }
    // Load goal items for all selected items
    this.loadGoalItemsForSelectedItems();
    this.currentStep = 4;
  }

  // ===========================
  // Step 4: Select Goal Items
  // ===========================

  loadGoalItemsForSelectedItems(): void {
    this.loading = true;
    this.error = null;
    let loadedCount = 0;
    const totalToLoad = this.selectedItemIds.size;

    this.selectedItemIds.forEach(itemId => {
      this.goalsService.getGoalItemsByItem(itemId).subscribe({
        next: (goalItems) => {
          this.allGoalItems.set(itemId, goalItems);
          loadedCount++;
          if (loadedCount === totalToLoad) {
            this.loading = false;
          }
        },
        error: (err) => {
          this.error = 'Failed to load goal items. Please try again.';
          this.loading = false;
          console.error('Error loading goal items:', err);
        }
      });
    });
  }

  getGoalItemsForDisplay(): GoalFrameworkGoalItemDto[] {
    const goalItems: GoalFrameworkGoalItemDto[] = [];
    this.selectedItemIds.forEach(itemId => {
      const items = this.allGoalItems.get(itemId);
      if (items) {
        goalItems.push(...items);
      }
    });
    return goalItems;
  }

  toggleGoalItemSelection(goalItemId: string): void {
    const goalItem = this.findGoalItemById(goalItemId);
    if (!goalItem) return;

    if (this.selectedGoalItems.has(goalItemId)) {
      this.selectedGoalItems.delete(goalItemId);
      this.goalConfigurations.delete(goalItemId);
    } else {
      this.selectedGoalItems.set(goalItemId, goalItem);
      this.initializeGoalConfiguration(goalItem);
    }
    this.recalculateTotalScore();
  }

  isGoalItemSelected(goalItemId: string): boolean {
    return this.selectedGoalItems.has(goalItemId);
  }

  findGoalItemById(goalItemId: string): GoalFrameworkGoalItemDto | null {
    for (const goalItems of this.allGoalItems.values()) {
      const found = goalItems.find(gi => gi.id === goalItemId);
      if (found) return found;
    }
    return null;
  }

  recalculateTotalScore(): void {
    this.totalTargetScore = 0;
    this.selectedGoalItems.forEach(goalItem => {
      this.totalTargetScore += goalItem.targetScore;
    });
  }

  canProceedToConfiguration(): boolean {
    return this.totalTargetScore >= this.minRequiredScore;
  }

  proceedToConfiguration(): void {
    if (!this.canProceedToConfiguration()) {
      alert(`Please select goal items with a total score of at least ${this.minRequiredScore}. Current total: ${this.totalTargetScore}`);
      return;
    }
    this.currentConfigIndex = 0;
    this.currentStep = 5;
  }

  // ===========================
  // Step 5: Configure Details
  // ===========================

  initializeGoalConfiguration(goalItem: GoalFrameworkGoalItemDto): void {
    this.goalConfigurations.set(goalItem.id, {
      goalItem: goalItem,
      title: goalItem.name,
      description: goalItem.description || '',
      startDate: this.formatDateForInput(this.goalStartDate),
      dueDate: this.formatDateForInput(this.goalEndDate),
      selectedSuggestedActivityIds: [],
      customActivities: []
    });
  }

  getCurrentConfiguration(): GoalItemSelection | null {
    const goalItemIds = Array.from(this.selectedGoalItems.keys());
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
    
    // Generate a single GoalSetId for all goals created together
    const goalSetId = this.generateGuid();

    try {
      for (const config of configurations) {
        // Parse date strings to Date objects, then convert to ISO string
        const startDate = typeof config.startDate === 'string' 
          ? this.parseDateFromInput(config.startDate)
          : config.startDate;
        
        const dueDate = typeof config.dueDate === 'string'
          ? this.parseDateFromInput(config.dueDate)
          : config.dueDate;

        const dto: CreatePersonalGoalDto = {
          goalItemId: config.goalItem.id,
          goalSetId: goalSetId, // Same for all goals in this batch
          title: config.title,
          description: config.description,
          startDate: startDate.toISOString(),
          dueDate: dueDate.toISOString(),
          selectedSuggestedActivityIds: [],
          customActivities: config.customActivities
        };

        const result = await this.goalsService.createPersonalGoal(dto).toPromise();
        if (result && result.id) {
          createdGoals.push(result.id);
        }
      }

      // Automatically submit the goal set for RM evaluation
      try {
        const submitResult = await this.goalsService.submitGoalSetForEvaluation(goalSetId).toPromise();
        this.loading = false;
        alert(`Successfully created ${createdGoals.length} goal(s) and submitted for RM review!\n\n${submitResult?.message || 'Your Reporting Manager will be notified.'}`);
      } catch (submitErr: any) {
        // Goals were created but submission failed
        this.loading = false;
        const errorMessage = submitErr?.error?.error || submitErr?.error?.message || 'Failed to submit for evaluation.';
        alert(`Created ${createdGoals.length} goal(s) but failed to submit for RM review: ${errorMessage}\n\nYou can submit manually from the My Goals page.`);
      }

      this.router.navigate(['/employee/goals']);
    } catch (err) {
      this.loading = false;
      this.error = 'Failed to create goals. Please try again.';
      console.error('Error creating goals:', err);
    }
  }

  // Helper method to generate GUID
  private generateGuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
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
