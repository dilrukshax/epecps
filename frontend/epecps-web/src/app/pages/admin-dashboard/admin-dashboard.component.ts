import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ScoreTemplateService } from '../../services/score-template.service';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css'],
  standalone: false
})
export class AdminDashboardComponent implements OnInit {
  stats = {
    totalTemplates: 0,
    publishedTemplates: 0,
    draftTemplates: 0,
    archivedTemplates: 0
  };

  loading = false;
  error: string | null = null;

  adminActions = [
    {
      title: 'Manage Templates',
      description: 'Create, edit, and publish score templates',
      icon: 'template',
      route: '/admin/templates',
      color: 'blue'
    },
    {
      title: 'View All Templates',
      description: 'Browse all templates including archived ones',
      icon: 'list',
      route: '/admin/templates',
      color: 'green'
    },
    {
      title: 'API Documentation',
      description: 'View API endpoints and documentation',
      icon: 'doc',
      route: '#',
      color: 'purple'
    },
    {
      title: 'System Settings',
      description: 'Configure system-wide settings',
      icon: 'settings',
      route: '#',
      color: 'gray'
    }
  ];

  apiEndpoints = [
    { method: 'GET', path: '/api/v1/admin/templates', description: 'Get all templates' },
    { method: 'GET', path: '/api/v1/admin/templates/{id}', description: 'Get template details' },
    { method: 'POST', path: '/api/v1/admin/templates', description: 'Create new template' },
    { method: 'PUT', path: '/api/v1/admin/templates/{id}', description: 'Update template' },
    { method: 'POST', path: '/api/v1/admin/templates/{id}/publish', description: 'Publish template' },
    { method: 'POST', path: '/api/v1/admin/templates/{id}/clone', description: 'Clone template' },
    { method: 'POST', path: '/api/v1/admin/templates/{id}/archive', description: 'Archive template' },
    { method: 'POST', path: '/api/v1/admin/templates/{templateId}/categories', description: 'Create category' },
    { method: 'PUT', path: '/api/v1/admin/categories/{categoryId}', description: 'Update category' },
    { method: 'DELETE', path: '/api/v1/admin/categories/{categoryId}', description: 'Delete category' },
    { method: 'POST', path: '/api/v1/admin/categories/{categoryId}/items', description: 'Create item' },
    { method: 'PUT', path: '/api/v1/admin/items/{itemId}', description: 'Update item' },
    { method: 'DELETE', path: '/api/v1/admin/items/{itemId}', description: 'Delete item' }
  ];

  constructor(
    private templateService: ScoreTemplateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;
    this.error = null;

    this.templateService.getTemplates(true).subscribe({
      next: (templates) => {
        this.stats.totalTemplates = templates.length;
        this.stats.publishedTemplates = templates.filter(t => t.isPublished && !t.isArchived).length;
        this.stats.draftTemplates = templates.filter(t => !t.isPublished && !t.isArchived).length;
        this.stats.archivedTemplates = templates.filter(t => t.isArchived).length;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load statistics';
        this.loading = false;
        console.error('Error loading stats:', err);
      }
    });
  }

  navigateTo(route: string): void {
    if (route !== '#') {
      this.router.navigate([route]);
    }
  }

  getMethodColor(method: string): string {
    switch (method) {
      case 'GET': return 'text-green-600 bg-green-50';
      case 'POST': return 'text-blue-600 bg-blue-50';
      case 'PUT': return 'text-yellow-600 bg-yellow-50';
      case 'DELETE': return 'text-red-600 bg-red-50';
      default: return 'text-gray-600 bg-gray-50';
    }
  }
}
