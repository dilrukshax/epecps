import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardDataDto } from '../../models/dashboard.models';

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
    private authService: MsalService,
    private router: Router,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    const account = this.authService.instance.getActiveAccount();
    if (account) {
      this.userName = account.name || '';
      this.userEmail = account.username || '';
      this.userRoles = account.idTokenClaims?.['roles'] as string[] || [];
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
    if (status.includes('Completed')) return 'text-green-600';
    if (status.includes('Rejected')) return 'text-red-600';
    if (status.includes('Pending')) return 'text-yellow-600';
    if (status.includes('Approved')) return 'text-blue-600';
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

  navigateToAdminDashboard(): void {
    this.router.navigate(['/admin/dashboard']);
  }

  hasHrRole(): boolean {
    return this.userRoles.includes('HR');
  }

  hasAdminRole(): boolean {
    return this.userRoles.includes('Admin') || 
           this.userRoles.includes('HOD') || 
           this.userRoles.includes('GM');
  }
}
