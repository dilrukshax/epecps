import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreCategoryDto } from '../../../models/score-template.models';

/**
 * Component for listing and managing categories within a template
 */
@Component({
  selector: 'app-category-list',
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.css'],
  standalone: false
})
export class CategoryListComponent {
  @Input() templateId!: string;
  @Input() categories: ScoreCategoryDto[] = [];
  @Input() selectedCategory: ScoreCategoryDto | null = null;
  @Input() isPublished = false;

  @Output() categorySelected = new EventEmitter<ScoreCategoryDto>();
  @Output() categoriesChanged = new EventEmitter<void>();

  showCreateDialog = false;
  showEditDialog = false;
  categoryToEdit: ScoreCategoryDto | null = null;

  constructor(private templateService: ScoreTemplateService) {}

  /**
   * Handle category selection
   */
  onSelectCategory(category: ScoreCategoryDto): void {
    this.categorySelected.emit(category);
  }

  /**
   * Open create category dialog
   */
  onCreateCategory(): void {
    this.showCreateDialog = true;
  }

  /**
   * Handle category created
   */
  onCategoryCreated(): void {
    this.showCreateDialog = false;
    this.categoriesChanged.emit();
  }

  /**
   * Open edit category dialog
   */
  onEditCategory(category: ScoreCategoryDto, event: Event): void {
    event.stopPropagation();
    this.categoryToEdit = category;
    this.showEditDialog = true;
  }

  /**
   * Handle category updated
   */
  onCategoryUpdated(): void {
    this.showEditDialog = false;
    this.categoryToEdit = null;
    this.categoriesChanged.emit();
  }

  /**
   * Delete a category
   */
  onDeleteCategory(category: ScoreCategoryDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Delete category "${category.name}"? This will also delete all items in this category.`)) {
      return;
    }

    this.templateService.deleteCategory(category.id).subscribe({
      next: () => {
        this.categoriesChanged.emit();
      },
      error: (err) => {
        alert('Failed to delete category. ' + (err.error?.message || 'Please try again.'));
        console.error('Error deleting category:', err);
      }
    });
  }

  /**
   * Get active categories count
   */
  getActiveCategoriesCount(): number {
    return this.categories.filter(c => c.isActive).length;
  }

  /**
   * Check if category is selected
   */
  isCategorySelected(category: ScoreCategoryDto): boolean {
    return this.selectedCategory?.id === category.id;
  }
}
