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
      title: 'Goal Library',
      description: 'Add, edit, and manage goals available for RM assignment',
      icon: 'goals',
      route: '/admin/templates',
      color: 'blue'
    },
    {
      title: 'User & Role Management',
      description: 'Manage users and assign roles',
      icon: 'users',
      route: '/admin/templates/users',
      color: 'green'
    },
    {
      title: 'Score Templates',
      description: 'Manage underlying score templates and categories',
      icon: 'list',
      route: '/admin/templates/templates',
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
}
