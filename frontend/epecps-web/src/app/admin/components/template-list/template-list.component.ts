import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ScoreTemplateService } from '../../../services/score-template.service';
import { ScoreTemplateListDto } from '../../../models/score-template.models';

/**
 * Component for listing all score templates
 * Provides actions to create, edit, publish, clone, and archive templates
 */
@Component({
  selector: 'app-template-list',
  templateUrl: './template-list.component.html',
  styleUrls: ['./template-list.component.css'],
  standalone: false
})
export class TemplateListComponent implements OnInit {
  templates: ScoreTemplateListDto[] = [];
  loading = false;
  error: string | null = null;
  includeArchived = false;
  showCreateDialog = false;

  constructor(
    private templateService: ScoreTemplateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadTemplates();
  }

  /**
   * Load templates from the API
   */
  loadTemplates(): void {
    this.loading = true;
    this.error = null;

    this.templateService.getTemplates(this.includeArchived).subscribe({
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

  /**
   * Toggle archived templates visibility
   */
  onToggleArchived(): void {
    this.includeArchived = !this.includeArchived;
    this.loadTemplates();
  }

  /**
   * Open create template dialog
   */
  onCreateTemplate(): void {
    this.showCreateDialog = true;
  }

  /**
   * Handle template created event
   */
  onTemplateCreated(templateId: string): void {
    this.showCreateDialog = false;
    this.router.navigate(['/admin/templates', templateId]);
  }

  /**
   * Navigate to template edit page
   */
  onEditTemplate(template: ScoreTemplateListDto): void {
    this.router.navigate(['/admin/templates', template.id]);
  }

  /**
   * Publish a template
   */
  onPublishTemplate(template: ScoreTemplateListDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Are you sure you want to publish "${template.name}"? Published templates cannot be modified.`)) {
      return;
    }

    this.templateService.publishTemplate(template.id).subscribe({
      next: () => {
        this.loadTemplates();
      },
      error: (err) => {
        alert('Failed to publish template. ' + (err.error?.message || 'Please try again.'));
        console.error('Error publishing template:', err);
      }
    });
  }

  /**
   * Clone a template
   */
  onCloneTemplate(template: ScoreTemplateListDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Clone template "${template.name}"?`)) {
      return;
    }

    this.templateService.cloneTemplate(template.id).subscribe({
      next: (newTemplateId) => {
        this.router.navigate(['/admin/templates', newTemplateId]);
      },
      error: (err) => {
        alert('Failed to clone template. Please try again.');
        console.error('Error cloning template:', err);
      }
    });
  }

  /**
   * Archive a template
   */
  onArchiveTemplate(template: ScoreTemplateListDto, event: Event): void {
    event.stopPropagation();

    if (!confirm(`Are you sure you want to archive "${template.name}"?`)) {
      return;
    }

    this.templateService.archiveTemplate(template.id).subscribe({
      next: () => {
        this.loadTemplates();
      },
      error: (err) => {
        alert('Failed to archive template. ' + (err.error?.message || 'Please try again.'));
        console.error('Error archiving template:', err);
      }
    });
  }

  /**
   * Get status badge class
   */
  getStatusClass(template: ScoreTemplateListDto): string {
    if (template.isArchived) return 'bg-gray-100 text-gray-800';
    if (template.isPublished) return 'bg-green-100 text-green-800';
    return 'bg-yellow-100 text-yellow-800';
  }

  /**
   * Get status text
   */
  getStatusText(template: ScoreTemplateListDto): string {
    if (template.isArchived) return 'Archived';
    if (template.isPublished) return 'Published';
    return 'Draft';
  }
}
