import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardDataDto } from '../../models/dashboard.models';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  standalone: false
})
export class DashboardComponent implements OnInit {
  userName = '';
  userEmail = '';
  userRoles: string[] = [];

  // Dashboard data
  dashboardData: DashboardDataDto | null = null;
  dashboardLoading = false;
  dashboardError: string | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.userName = user.fullName || '';
      this.userEmail = user.email || '';
      this.userRoles = user.roles || [];
    } else if (this.authService.isAuthenticated()) {
      this.authService.getMe().subscribe({
        next: (me) => {
          this.userName = me.fullName || '';
          this.userEmail = me.email || '';
          this.userRoles = me.roles || [];
        }
      });
    }

    // Load dashboard data for all users
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.dashboardLoading = true;
    this.dashboardError = null;

    this.dashboardService.getDashboardData().subscribe({
      next: (data) => {
        this.dashboardData = data;
        this.dashboardLoading = false;
      },
      error: (err) => {
        this.dashboardError = 'Failed to load dashboard data. Please try again.';
        this.dashboardLoading = false;
        console.error('Dashboard error:', err);
      }
    });
  }

  getStatusColor(status: string): string {
    const normalized = status.toLowerCase();
    if (normalized.includes('completed')) return 'text-green-600';
    if (normalized.includes('rejected')) return 'text-red-600';
    if (normalized.includes('pending')) return 'text-yellow-600';
    if (normalized.includes('approved')) return 'text-blue-600';
    if (normalized.includes('deferred')) return 'text-indigo-600';
    return 'text-gray-600';
  }

  getScoreColorClass(score: number): string {
    if (score >= 80) return 'text-green-600';
    if (score >= 70) return 'text-blue-600';
    if (score >= 50) return 'text-yellow-600';
    return 'text-red-600';
  }

  getTimeSince(date: Date | string | null | undefined): string {
    if (!date) return 'N/A';
    const now = new Date();
    const then = new Date(date);
    const diffMs = now.getTime() - then.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 60) return `${diffMins} min ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays}d ago`;
    const diffWeeks = Math.floor(diffDays / 7);
    if (diffWeeks < 4) return `${diffWeeks}w ago`;
    return then.toLocaleDateString();
  }

  getCurrentTime(): string {
    return new Date().toLocaleTimeString();
  }

  getCompletionPercentage(): number {
    if (!this.dashboardData) return 0;
    const total = this.dashboardData.stats.completedThisMonth + 
                  this.dashboardData.stats.totalEvaluationsUnderReview;
    if (total === 0) return 0;
    return (this.dashboardData.stats.completedThisMonth / total) * 100;
  }

  getExcellenceRate(): number {
    if (!this.dashboardData) return 0;
    const totalEvaluated = this.dashboardData.stats.completedThisMonth;
    if (totalEvaluated === 0) return 0;
    return Math.round((this.dashboardData.stats.highPerformers / totalEvaluated) * 100);
  }

  viewEvaluation(evaluationId: number): void {
    this.router.navigate(['/evaluations', evaluationId]);
  }

  navigateToGoals(): void {
    this.router.navigate(['/employee/goals']);
  }

  navigateToMyApprovals(): void {
    this.router.navigate(['/evaluations/my-approvals']);
  }

  navigateToHrReports(): void {
    this.router.navigate(['/employee/hr-reports']);
  }

  navigateToHrPipCases(): void {
    this.router.navigate(['/employee/hr-pip-cases']);
  }

  navigateToAdminDashboard(): void {
    this.router.navigate(['/admin/dashboard']);
  }

  hasHrRole(): boolean {
    return this.userRoles.includes('HR') || this.userRoles.includes('SuperAdmin');
  }

  hasAdminRole(): boolean {
    return this.userRoles.includes('Admin') || 
           this.userRoles.includes('SuperAdmin') ||
           this.userRoles.includes('HOD') || 
           this.userRoles.includes('GM');
  }
}
