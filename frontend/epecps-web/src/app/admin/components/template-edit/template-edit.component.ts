import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreTemplateDetailDto, ScoreCategoryDto } from '../../../models/score-template.models';

/**
 * Component for editing a score template
 * Manages template information and categories
 */
@Component({
  selector: 'app-template-edit',
  templateUrl: './template-edit.component.html',
  styleUrls: ['./template-edit.component.css'],
  standalone: false
})
export class TemplateEditComponent implements OnInit {
  template: ScoreTemplateDetailDto | null = null;
  selectedCategory: ScoreCategoryDto | null = null;
  loading = false;
  error: string | null = null;
  showTemplateEditDialog = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private templateService: ScoreTemplateService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const templateId = params['id'];
      if (templateId) {
        this.loadTemplate(templateId);
      }
    });
  }

  /**
   * Load template details from the API
   */
  loadTemplate(id: string): void {
    this.loading = true;
    this.error = null;

    this.templateService.getTemplateById(id).subscribe({
      next: (template) => {
        this.template = template;
        this.loading = false;
        
        // Auto-select first category if available
        if (template.categories.length > 0 && !this.selectedCategory) {
          this.selectedCategory = template.categories[0];
        }
      },
      error: (err) => {
        this.error = 'Failed to load template. Please try again.';
        this.loading = false;
        console.error('Error loading template:', err);
      }
    });
  }

  /**
   * Calculate total weight percentage of all active categories
   */
  getTotalWeight(): number {
    if (!this.template) return 0;
    return this.template.categories
      .filter(c => c.isActive)
      .reduce((sum, cat) => sum + cat.weightPercent, 0);
  }

  /**
   * Check if total weight is valid (exactly 100%)
   */
  isWeightValid(): boolean {
    return Math.abs(this.getTotalWeight() - 100) < 0.01;
  }

  /**
   * Handle category selection
   */
  onCategorySelected(category: ScoreCategoryDto): void {
    this.selectedCategory = category;
  }

  /**
   * Handle category changes (create/update/delete)
   */
  onCategoriesChanged(): void {
    if (this.template) {
      this.loadTemplate(this.template.id);
    }
  }

  /**
   * Open template edit dialog
   */
  onEditTemplateInfo(): void {
    if (!this.template?.isPublished) {
      this.showTemplateEditDialog = true;
    }
  }

  /**
   * Handle template updated event
   */
  onTemplateUpdated(): void {
    this.showTemplateEditDialog = false;
    if (this.template) {
      this.loadTemplate(this.template.id);
    }
  }

  /**
   * Publish the template
   */
  onPublishTemplate(): void {
    if (!this.template) return;

    if (!this.isWeightValid()) {
      alert('Cannot publish: Total category weight must equal 100%. Current: ' + this.getTotalWeight().toFixed(2) + '%');
      return;
    }

    if (!confirm(`Publish template "${this.template.name}"? Published templates cannot be modified.`)) {
      return;
    }

    this.templateService.publishTemplate(this.template.id).subscribe({
      next: () => {
        this.loadTemplate(this.template!.id);
        alert('Template published successfully!');
      },
      error: (err) => {
        alert('Failed to publish template. ' + (err.error?.message || 'Please try again.'));
        console.error('Error publishing template:', err);
      }
    });
  }

  /**
   * Navigate back to template list
   */
  onBack(): void {
    this.router.navigate(['/admin/templates']);
  }
}
