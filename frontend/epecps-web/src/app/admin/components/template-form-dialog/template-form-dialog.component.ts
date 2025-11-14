import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreTemplateDetailDto } from '../../../models/score-template.models';

/**
 * Dialog component for creating/editing templates
 */
@Component({
  selector: 'app-template-form-dialog',
  templateUrl: './template-form-dialog.component.html',
  styleUrls: ['./template-form-dialog.component.css'],
  standalone: false
})
export class TemplateFormDialogComponent implements OnInit {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() template?: ScoreTemplateDetailDto;

  @Output() templateCreated = new EventEmitter<string>();
  @Output() templateUpdated = new EventEmitter<void>();
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
      name: [this.template?.name || '', [Validators.required, Validators.maxLength(200)]],
      description: [this.template?.description || '', Validators.maxLength(1000)]
    });
  }

  /**
   * Get form title based on mode
   */
  getTitle(): string {
    return this.mode === 'create' ? 'Create Template' : 'Edit Template';
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
      this.createTemplate();
    } else {
      this.updateTemplate();
    }
  }

  /**
   * Create new template
   */
  private createTemplate(): void {
    this.templateService.createTemplate(this.form.value).subscribe({
      next: (templateId) => {
        this.loading = false;
        this.templateCreated.emit(templateId);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to create template. Please try again.';
        console.error('Error creating template:', err);
      }
    });
  }

  /**
   * Update existing template
   */
  private updateTemplate(): void {
    if (!this.template) return;

    this.templateService.updateTemplate(this.template.id, this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.templateUpdated.emit();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to update template. Please try again.';
        console.error('Error updating template:', err);
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
    }
    return null;
  }
}
