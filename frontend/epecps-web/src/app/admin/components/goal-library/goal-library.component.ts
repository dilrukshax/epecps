import { Component, OnInit } from '@angular/core';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { forkJoin } from 'rxjs';
import {
  ScoreTemplateListDto,
  ScoreTemplateDetailDto,
  ScoreCategoryDto,
  ScoreItemDto,
  CreateScoreItemDto,
  UpdateScoreItemDto,
  ScoreItemType,
  CreateScoreCategoryDto
} from '../../../models/score-template.models';

interface GoalLibraryRow {
  item: ScoreItemDto;
  categoryName: string;
  categoryId: string;
  templateName: string;
  templateId: string;
}

@Component({
  selector: 'app-goal-library',
  templateUrl: './goal-library.component.html',
  styleUrls: ['./goal-library.component.css'],
  standalone: false
})
export class GoalLibraryComponent implements OnInit {
  // All goals (flat view)
  goals: GoalLibraryRow[] = [];
  filteredGoals: GoalLibraryRow[] = [];
  searchQuery = '';
  selectedCategoryFilter = 'all';
  categories: string[] = [];

  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Templates and categories for the add-goal form
  templates: ScoreTemplateListDto[] = [];
  templateDetails: Map<string, ScoreTemplateDetailDto> = new Map();

  // Add Goal dialog
  showAddGoalDialog = false;
  addGoalForm = {
    templateId: '',
    categoryId: '',
    name: '',
    description: '',
    weight: 100,
    maxScore: 100,
    isMandatory: false
  };
  availableCategories: ScoreCategoryDto[] = [];
  addGoalLoading = false;
  addGoalError: string | null = null;

  // Edit Goal dialog
  showEditGoalDialog = false;
  editGoalForm = {
    itemId: '',
    name: '',
    description: '',
    weight: 0,
    maxScore: 0,
    isMandatory: false,
    isActive: true
  };
  editGoalLoading = false;
  editGoalError: string | null = null;

  // Add Category dialog
  showAddCategoryDialog = false;
  addCategoryForm = {
    templateId: '',
    name: '',
    description: '',
    weightPercent: 100
  };
  addCategoryLoading = false;
  addCategoryError: string | null = null;

  // Hardcoded defaults to simplify first-time admin setup
  seedingDefaults = false;
  private defaultSeedAttempted = false;
  private readonly defaultTemplateSeed = {
    name: 'EmpoVate Default Template',
    description: 'Auto-created default template for quick goal/category setup.'
  };
  private readonly defaultCategorySeeds: Array<{ name: string; description: string; weightPercent: number }> = [
    { name: 'Technical Skills', description: 'Core technical delivery and engineering quality.', weightPercent: 25 },
    { name: 'Quality & Testing', description: 'Code quality, stability, and defect prevention.', weightPercent: 20 },
    { name: 'Collaboration', description: 'Team contribution, support, and cross-functional work.', weightPercent: 20 },
    { name: 'Communication', description: 'Clear updates, documentation, and stakeholder communication.', weightPercent: 15 },
    { name: 'Ownership & Growth', description: 'Initiative, accountability, and continuous improvement.', weightPercent: 20 }
  ];

  constructor(private templateService: ScoreTemplateService) {}

  ngOnInit(): void {
    this.loadGoals();
  }

  // ===========================
  // Load all goals
  // ===========================

  loadGoals(): void {
    this.loading = true;
    this.error = null;

    this.templateService.getTemplates(false).subscribe({
      next: (templates) => {
        this.templates = templates;
        this.goals = [];
        this.templateDetails.clear();

        if (templates.length === 0) {
          this.seedDefaultTemplateData();
          return;
        }

        let loaded = 0;
        for (const tpl of templates) {
          this.templateService.getTemplateById(tpl.id).subscribe({
            next: (detail) => {
              this.templateDetails.set(tpl.id, detail);
              for (const cat of detail.categories) {
                if (!cat.isActive) continue;
                for (const item of cat.items) {
                  if (!item.isActive) continue;
                  this.goals.push({
                    item,
                    categoryName: cat.name,
                    categoryId: cat.id,
                    templateName: detail.name,
                    templateId: detail.id
                  });
                }
              }
              loaded++;
              if (loaded === templates.length) {
                this.categories = [...new Set(this.goals.map(g => g.categoryName))].sort();
                this.applyFilters();
                this.loading = false;
              }
            },
            error: () => {
              loaded++;
              if (loaded === templates.length) {
                this.categories = [...new Set(this.goals.map(g => g.categoryName))].sort();
                this.applyFilters();
                this.loading = false;
              }
            }
          });
        }
      },
      error: (err) => {
        this.error = 'Failed to load goals. Please try again.';
        this.loading = false;
        console.error('Error loading goals:', err);
      }
    });
  }

  private seedDefaultTemplateData(): void {
    if (this.seedingDefaults) {
      return;
    }
    if (this.defaultSeedAttempted) {
      this.loading = false;
      this.error = 'No templates available. Please create a template first.';
      return;
    }

    this.defaultSeedAttempted = true;
    this.seedingDefaults = true;
    this.error = null;

    this.templateService.createTemplate(this.defaultTemplateSeed).subscribe({
      next: (templateId) => {
        const categoryRequests = this.defaultCategorySeeds.map((category, index) =>
          this.templateService.createCategory(templateId, {
            name: category.name,
            description: category.description,
            weightPercent: category.weightPercent,
            maxScore: 100,
            displayOrder: index
          })
        );

        forkJoin(categoryRequests).subscribe({
          next: () => {
            this.seedingDefaults = false;
            this.successMessage = 'Default template and categories created. You can now add goals.';
            setTimeout(() => (this.successMessage = null), 4000);
            this.loadGoals();
          },
          error: (err) => {
            this.seedingDefaults = false;
            console.error('Error creating default categories:', err);
            this.loadGoals();
          }
        });
      },
      error: (err) => {
        this.seedingDefaults = false;
        console.error('Error creating default template:', err);
        // Reload in case another user/process created it concurrently.
        this.loadGoals();
      }
    });
  }

  applyFilters(): void {
    const q = this.searchQuery.toLowerCase();
    this.filteredGoals = this.goals.filter(g => {
      const matchesSearch = !q ||
        g.item.name.toLowerCase().includes(q) ||
        (g.item.description || '').toLowerCase().includes(q) ||
        g.categoryName.toLowerCase().includes(q);
      const matchesCat = this.selectedCategoryFilter === 'all' || g.categoryName === this.selectedCategoryFilter;
      return matchesSearch && matchesCat;
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onCategoryFilterChange(): void {
    this.applyFilters();
  }

  // ===========================
  // Add Goal
  // ===========================

  openAddGoalDialog(): void {
    this.showAddGoalDialog = true;
    this.addGoalError = null;
    this.addGoalForm = {
      templateId: this.templates.length > 0 ? this.templates[0].id : '',
      categoryId: '',
      name: '',
      description: '',
      weight: 100,
      maxScore: 100,
      isMandatory: false
    };
    this.onAddGoalTemplateChange();
  }

  onAddGoalTemplateChange(): void {
    const detail = this.templateDetails.get(this.addGoalForm.templateId);
    if (detail) {
      this.availableCategories = detail.categories.filter(c => c.isActive);
      if (this.availableCategories.length > 0) {
        this.addGoalForm.categoryId = this.availableCategories[0].id;
      } else {
        this.addGoalForm.categoryId = '';
      }
    } else {
      this.availableCategories = [];
      this.addGoalForm.categoryId = '';
      // Load the template detail if not cached
      if (this.addGoalForm.templateId) {
        this.templateService.getTemplateById(this.addGoalForm.templateId).subscribe({
          next: (detail) => {
            this.templateDetails.set(this.addGoalForm.templateId, detail);
            this.availableCategories = detail.categories.filter(c => c.isActive);
            if (this.availableCategories.length > 0) {
              this.addGoalForm.categoryId = this.availableCategories[0].id;
            }
          }
        });
      }
    }
  }

  submitAddGoal(): void {
    if (!this.addGoalForm.name.trim()) {
      this.addGoalError = 'Goal name is required.';
      return;
    }
    if (!this.addGoalForm.categoryId) {
      this.addGoalError = 'Please select a category.';
      return;
    }

    this.addGoalLoading = true;
    this.addGoalError = null;

    const dto: CreateScoreItemDto = {
      name: this.addGoalForm.name.trim(),
      description: this.addGoalForm.description.trim() || undefined,
      itemType: ScoreItemType.Rating,
      maxScore: this.addGoalForm.maxScore,
      weightWithinCategory: this.addGoalForm.weight,
      isMandatory: this.addGoalForm.isMandatory,
      evidenceRequired: false,
      evidenceHint: undefined,
      displayOrder: this.goals.length
    };

    this.templateService.createItem(this.addGoalForm.categoryId, dto).subscribe({
      next: () => {
        this.addGoalLoading = false;
        this.showAddGoalDialog = false;
        this.successMessage = `Goal "${this.addGoalForm.name}" added successfully!`;
        setTimeout(() => this.successMessage = null, 4000);
        this.loadGoals();
      },
      error: (err) => {
        this.addGoalLoading = false;
        this.addGoalError = err.error?.message || 'Failed to add goal. Please try again.';
        console.error('Error adding goal:', err);
      }
    });
  }

  // ===========================
  // Edit Goal
  // ===========================

  openEditGoalDialog(goal: GoalLibraryRow): void {
    this.showEditGoalDialog = true;
    this.editGoalError = null;
    this.editGoalForm = {
      itemId: goal.item.id,
      name: goal.item.name,
      description: goal.item.description || '',
      weight: goal.item.weightWithinCategory || 0,
      maxScore: goal.item.maxScore,
      isMandatory: goal.item.isMandatory,
      isActive: goal.item.isActive
    };
  }

  submitEditGoal(): void {
    if (!this.editGoalForm.name.trim()) {
      this.editGoalError = 'Goal name is required.';
      return;
    }

    this.editGoalLoading = true;
    this.editGoalError = null;

    const dto: UpdateScoreItemDto = {
      name: this.editGoalForm.name.trim(),
      description: this.editGoalForm.description.trim() || undefined,
      itemType: ScoreItemType.Rating,
      maxScore: this.editGoalForm.maxScore,
      weightWithinCategory: this.editGoalForm.weight,
      isMandatory: this.editGoalForm.isMandatory,
      evidenceRequired: false,
      evidenceHint: undefined,
      displayOrder: 0,
      isActive: this.editGoalForm.isActive
    };

    this.templateService.updateItem(this.editGoalForm.itemId, dto).subscribe({
      next: () => {
        this.editGoalLoading = false;
        this.showEditGoalDialog = false;
        this.successMessage = `Goal "${this.editGoalForm.name}" updated successfully!`;
        setTimeout(() => this.successMessage = null, 4000);
        this.loadGoals();
      },
      error: (err) => {
        this.editGoalLoading = false;
        this.editGoalError = err.error?.message || 'Failed to update goal. Please try again.';
        console.error('Error updating goal:', err);
      }
    });
  }

  // ===========================
  // Delete Goal
  // ===========================

  deleteGoal(goal: GoalLibraryRow): void {
    if (!confirm(`Are you sure you want to remove "${goal.item.name}"?`)) return;

    this.templateService.deleteItem(goal.item.id).subscribe({
      next: () => {
        this.successMessage = `Goal "${goal.item.name}" removed.`;
        setTimeout(() => this.successMessage = null, 4000);
        this.loadGoals();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to remove goal.');
        console.error('Error deleting goal:', err);
      }
    });
  }

  // ===========================
  // Add Category
  // ===========================

  openAddCategoryDialog(): void {
    this.showAddCategoryDialog = true;
    this.addCategoryError = null;
    this.addCategoryForm = {
      templateId: this.templates.length > 0 ? this.templates[0].id : '',
      name: '',
      description: '',
      weightPercent: 100
    };
  }

  submitAddCategory(): void {
    if (!this.addCategoryForm.name.trim()) {
      this.addCategoryError = 'Category name is required.';
      return;
    }
    if (!this.addCategoryForm.templateId) {
      this.addCategoryError = 'Please select a template.';
      return;
    }

    this.addCategoryLoading = true;
    this.addCategoryError = null;

    const dto: CreateScoreCategoryDto = {
      name: this.addCategoryForm.name.trim(),
      description: this.addCategoryForm.description.trim() || undefined,
      weightPercent: this.addCategoryForm.weightPercent,
      displayOrder: 0
    };

    this.templateService.createCategory(this.addCategoryForm.templateId, dto).subscribe({
      next: () => {
        this.addCategoryLoading = false;
        this.showAddCategoryDialog = false;
        this.successMessage = `Category "${this.addCategoryForm.name}" created!`;
        setTimeout(() => this.successMessage = null, 4000);
        this.loadGoals();
      },
      error: (err) => {
        this.addCategoryLoading = false;
        this.addCategoryError = err.error?.message || 'Failed to create category.';
        console.error('Error creating category:', err);
      }
    });
  }

  closeDialogs(): void {
    this.showAddGoalDialog = false;
    this.showEditGoalDialog = false;
    this.showAddCategoryDialog = false;
  }
}
