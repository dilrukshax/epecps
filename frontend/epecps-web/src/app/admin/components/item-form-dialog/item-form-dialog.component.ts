import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreItemDto, ScoreItemType } from '../../../models/score-template.models';

/**
 * Dialog component for creating/editing score items
 */
@Component({
  selector: 'app-item-form-dialog',
  templateUrl: './item-form-dialog.component.html',
  styleUrls: ['./item-form-dialog.component.css'],
  standalone: false
})
export class ItemFormDialogComponent implements OnInit {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() categoryId!: string;
  @Input() item?: ScoreItemDto;

  @Output() itemCreated = new EventEmitter<void>();
  @Output() itemUpdated = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  form!: FormGroup;
  loading = false;
  error: string | null = null;

  // Item types for dropdown
  itemTypes = [
    { value: ScoreItemType.Checklist, label: 'Checklist' },
    { value: ScoreItemType.Rating, label: 'Rating' },
    { value: ScoreItemType.Numeric, label: 'Numeric' }
  ];

  constructor(
    private fb: FormBuilder,
    private templateService: ScoreTemplateService
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  /**
   * Initialize the form
   */
  private initForm(): void {
    this.form = this.fb.group({
      name: [this.item?.name || '', [Validators.required, Validators.maxLength(200)]],
      description: [this.item?.description || '', Validators.maxLength(1000)],
      itemType: [this.item?.itemType ?? ScoreItemType.Rating, Validators.required],
      maxScore: [this.item?.maxScore || 0, [Validators.required, Validators.min(0)]],
      weightWithinCategory: [this.item?.weightWithinCategory || null, Validators.min(0)],
      isMandatory: [this.item?.isMandatory ?? false],
      evidenceRequired: [this.item?.evidenceRequired ?? false],
      evidenceHint: [this.item?.evidenceHint || '', Validators.maxLength(500)],
      displayOrder: [this.item?.displayOrder || 0, [Validators.required, Validators.min(0)]],
      isActive: [this.item?.isActive ?? true]
    });
  }

  /**
   * Get form title based on mode
   */
  getTitle(): string {
    return this.mode === 'create' ? 'Create Score Item' : 'Edit Score Item';
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    if (this.mode === 'create') {
      this.createItem();
    } else {
      this.updateItem();
    }
  }

  /**
   * Create new item
   */
  private createItem(): void {
    this.templateService.createItem(this.categoryId, this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.itemCreated.emit();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to create item. Please try again.';
        console.error('Error creating item:', err);
      }
    });
  }

  /**
   * Update existing item
   */
  private updateItem(): void {
    if (!this.item) return;

    this.templateService.updateItem(this.item.id, this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.itemUpdated.emit();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to update item. Please try again.';
        console.error('Error updating item:', err);
      }
    });
  }

  /**
   * Handle cancel
   */
  onCancel(): void {
    this.cancelled.emit();
  }

  /**
   * Get field error message
   */
  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (field?.invalid && (field.dirty || field.touched)) {
      if (field.errors?.['required']) {
        return 'This field is required';
      }
      if (field.errors?.['maxlength']) {
        return `Maximum length is ${field.errors['maxlength'].requiredLength} characters`;
      }
      if (field.errors?.['min']) {
        return `Minimum value is ${field.errors['min'].min}`;
      }
      if (field.errors?.['max']) {
        return `Maximum value is ${field.errors['max'].max}`;
      }
    }
    return null;
  }
}
