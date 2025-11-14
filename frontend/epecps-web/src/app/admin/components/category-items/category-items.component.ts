import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreItemDto, ScoreItemType } from '../../../models/score-template.models';

/**
 * Component for listing and managing items within a category
 */
@Component({
  selector: 'app-category-items',
  templateUrl: './category-items.component.html',
  styleUrls: ['./category-items.component.css'],
  standalone: false
})
export class CategoryItemsComponent {
  @Input() categoryId!: string;
  @Input() categoryName!: string;
  @Input() items: ScoreItemDto[] = [];
  @Input() isPublished = false;

  @Output() itemsChanged = new EventEmitter<void>();

  showCreateDialog = false;
  showEditDialog = false;
  itemToEdit: ScoreItemDto | null = null;

  // Reference to ScoreItemType enum for template
  ScoreItemType = ScoreItemType;

  constructor(private templateService: ScoreTemplateService) {}

  /**
   * Open create item dialog
   */
  onCreateItem(): void {
    this.showCreateDialog = true;
  }

  /**
   * Handle item created
   */
  onItemCreated(): void {
    this.showCreateDialog = false;
    this.itemsChanged.emit();
  }

  /**
   * Open edit item dialog
   */
  onEditItem(item: ScoreItemDto, event: Event): void {
    event.stopPropagation();
    this.itemToEdit = item;
    this.showEditDialog = true;
  }

  /**
   * Handle item updated
   */
  onItemUpdated(): void {
    this.showEditDialog = false;
    this.itemToEdit = null;
    this.itemsChanged.emit();
  }

  /**
   * Delete an item
   */
  onDeleteItem(item: ScoreItemDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Delete item "${item.name}"?`)) {
      return;
    }

    this.templateService.deleteItem(item.id).subscribe({
      next: () => {
        this.itemsChanged.emit();
      },
      error: (err) => {
        alert('Failed to delete item. ' + (err.error?.message || 'Please try again.'));
        console.error('Error deleting item:', err);
      }
    });
  }

  /**
   * Get active items count
   */
  getActiveItemsCount(): number {
    return this.items.filter(i => i.isActive).length;
  }

  /**
   * Get item type label
   */
  getItemTypeLabel(itemType: ScoreItemType): string {
    switch (itemType) {
      case ScoreItemType.Checklist:
        return 'Checklist';
      case ScoreItemType.Rating:
        return 'Rating';
      case ScoreItemType.Numeric:
        return 'Numeric';
      default:
        return 'Unknown';
    }
  }
}
