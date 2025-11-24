import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EvaluationService } from '../../../services/evaluation.service';
import { PendingApprovalDto } from '../../../models/evaluation.models';

@Component({
  selector: 'app-my-approvals',
  templateUrl: './my-approvals.component.html',
  styleUrls: ['./my-approvals.component.css'],
  standalone: false
})
export class MyApprovalsComponent implements OnInit {
  pendingApprovals: PendingApprovalDto[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private evaluationService: EvaluationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPendingApprovals();
  }

  loadPendingApprovals(): void {
    this.loading = true;
    this.error = null;

    this.evaluationService.getPendingApprovals().subscribe({
      next: (approvals) => {
        this.pendingApprovals = approvals;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load pending approvals. Please try again.';
        this.loading = false;
        console.error('Error loading pending approvals:', err);
      }
    });
  }

  viewEvaluation(evaluationId: number): void {
    this.router.navigate(['/evaluations', evaluationId]);
  }

  getStatusBadgeClass(status: string): string {
    const statusLower = status.toLowerCase();
    
    if (statusLower.includes('pending_rm')) return 'bg-blue-100 text-blue-800';
    if (statusLower.includes('pending_tl')) return 'bg-purple-100 text-purple-800';
    if (statusLower.includes('pending_peer')) return 'bg-yellow-100 text-yellow-800';
    if (statusLower.includes('pending_hod')) return 'bg-orange-100 text-orange-800';
    if (statusLower.includes('pending_gm')) return 'bg-red-100 text-red-800';
    if (statusLower.includes('completed')) return 'bg-green-100 text-green-800';
    
    return 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  getRoleBadgeClass(role: string): string {
    switch (role.toUpperCase()) {
      case 'RM': return 'bg-blue-100 text-blue-800';
      case 'TL': return 'bg-purple-100 text-purple-800';
      case 'PEER': return 'bg-yellow-100 text-yellow-800';
      case 'HOD': return 'bg-orange-100 text-orange-800';
      case 'GM': return 'bg-red-100 text-red-800';
      case 'HR': return 'bg-green-100 text-green-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getRoleLabel(role: string): string {
    const labels: { [key: string]: string } = {
      'RM': 'Reporting Manager',
      'TL': 'Team Lead',
      'PEER': 'Peer Reviewer',
      'HOD': 'Head of Department',
      'GM': 'General Manager',
      'HR': 'Human Resources'
    };
    return labels[role.toUpperCase()] || role;
  }

  getTimeSince(date: Date | undefined): string {
    if (!date) return 'N/A';
    
    const now = new Date();
    const then = new Date(date);
    const diffMs = now.getTime() - then.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  }

  getEmployeeInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }
}
