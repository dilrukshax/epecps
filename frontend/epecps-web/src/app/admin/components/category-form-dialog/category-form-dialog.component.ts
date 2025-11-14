import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreCategoryDto } from '../../../models/score-template.models';

/**
 * Dialog component for creating/editing categories
 */
@Component({
  selector: 'app-category-form-dialog',
  templateUrl: './category-form-dialog.component.html',
  styleUrls: ['./category-form-dialog.component.css'],
  standalone: false
})
export class CategoryFormDialogComponent implements OnInit {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() templateId!: string;
  @Input() category?: ScoreCategoryDto;

  @Output() categoryCreated = new EventEmitter<void>();
  @Output() categoryUpdated = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  form!: FormGroup;
  loading = false;
  error: string | null = null;

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
      name: [this.category?.name || '', [Validators.required, Validators.maxLength(200)]],
      description: [this.category?.description || '', Validators.maxLength(1000)],
      weightPercent: [this.category?.weightPercent || 0, [Validators.required, Validators.min(0), Validators.max(100)]],
      maxScore: [this.category?.maxScore || null, Validators.min(0)],
      displayOrder: [this.category?.displayOrder || 0, [Validators.required, Validators.min(0)]],
      isActive: [this.category?.isActive ?? true]
    });
  }

  /**
   * Get form title based on mode
   */
  getTitle(): string {
    return this.mode === 'create' ? 'Create Category' : 'Edit Category';
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
      this.createCategory();
    } else {
      this.updateCategory();
    }
  }

  /**
   * Create new category
   */
  private createCategory(): void {
    this.templateService.createCategory(this.templateId, this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.categoryCreated.emit();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to create category. Please try again.';
        console.error('Error creating category:', err);
      }
    });
  }

  /**
   * Update existing category
   */
  private updateCategory(): void {
    if (!this.category) return;

    this.templateService.updateCategory(this.category.id, this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.categoryUpdated.emit();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to update category. Please try again.';
        console.error('Error updating category:', err);
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
