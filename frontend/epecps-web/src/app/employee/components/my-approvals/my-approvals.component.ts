import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { EvaluationService } from '../../../services/evaluation.service';
import { 
  PendingApprovalDto, 
  MyEvaluationDto, 
  BulkApprovalStatsDto, 
  BulkApprovalCandidateDto,
  BulkApprovalResponseDto
} from '../../../models/evaluation.models';

@Component({
  selector: 'app-my-approvals',
  templateUrl: './my-approvals.component.html',
  styleUrls: ['./my-approvals.component.css'],
  standalone: false
})
export class MyApprovalsComponent implements OnInit {
  // Active tab: 'pending', 'all', or 'bulk'
  activeTab: 'pending' | 'all' | 'bulk' = 'pending';
  
  pendingApprovals: PendingApprovalDto[] = [];
  allEvaluations: MyEvaluationDto[] = [];
  
  // Bulk approval data
  bulkApprovalStats: BulkApprovalStatsDto | null = null;
  pendingGmApprovals: BulkApprovalCandidateDto[] = [];
  pendingHrProcessing: BulkApprovalCandidateDto[] = [];
  selectedEvaluationIds: Set<number> = new Set();
  bulkComment: string = '';
  
  // User roles
  userRoles: string[] = [];
  hasGmRole = false;
  hasHrRole = false;
  
  loading = false;
  bulkLoading = false;
  error: string | null = null;
  successMessage: string | null = null;
  bulkResult: BulkApprovalResponseDto | null = null;

  constructor(
    private evaluationService: EvaluationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPendingApprovals();
    this.checkUserRoles();
  }

  checkUserRoles(): void {
    // Load bulk approval stats to check if user has GM/HR role
    this.evaluationService.getBulkApprovalStats().subscribe({
      next: (stats) => {
        this.bulkApprovalStats = stats;
        this.hasGmRole = true;
        this.hasHrRole = true;
      },
      error: () => {
        // User doesn't have GM or HR role, hide bulk tab
        this.hasGmRole = false;
        this.hasHrRole = false;
      }
    });
  }

  switchTab(tab: 'pending' | 'all' | 'bulk'): void {
    this.activeTab = tab;
    this.error = null;
    this.successMessage = null;
    this.bulkResult = null;
    
    if (tab === 'all' && this.allEvaluations.length === 0) {
      this.loadAllEvaluations();
    }
    if (tab === 'bulk') {
      this.loadBulkApprovalData();
    }
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

  loadAllEvaluations(): void {
    this.loading = true;
    this.error = null;

    this.evaluationService.getMyEvaluations().subscribe({
      next: (evaluations) => {
        this.allEvaluations = evaluations;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load evaluations. Please try again.';
        this.loading = false;
        console.error('Error loading evaluations:', err);
      }
    });
  }

  loadBulkApprovalData(): void {
    this.bulkLoading = true;
    this.error = null;
    this.selectedEvaluationIds.clear();

    // Load stats
    this.evaluationService.getBulkApprovalStats().subscribe({
      next: (stats) => {
        this.bulkApprovalStats = stats;
      },
      error: (err) => {
        console.error('Error loading bulk approval stats:', err);
      }
    });

    // Load GM pending approvals
    this.evaluationService.getPendingGmBulkApprovals().subscribe({
      next: (approvals) => {
        this.pendingGmApprovals = approvals;
        this.hasGmRole = true;
      },
      error: (err) => {
        if (err.status === 403) {
          this.hasGmRole = false;
        }
        console.error('Error loading GM pending approvals:', err);
      }
    });

    // Load HR pending processing
    this.evaluationService.getPendingHrBulkProcessing().subscribe({
      next: (processing) => {
        this.pendingHrProcessing = processing;
        this.hasHrRole = true;
        this.bulkLoading = false;
      },
      error: (err) => {
        if (err.status === 403) {
          this.hasHrRole = false;
        }
        this.bulkLoading = false;
        console.error('Error loading HR pending processing:', err);
      }
    });
  }

  // Bulk selection methods
  toggleSelection(evaluationId: number): void {
    if (this.selectedEvaluationIds.has(evaluationId)) {
      this.selectedEvaluationIds.delete(evaluationId);
    } else {
      this.selectedEvaluationIds.add(evaluationId);
    }
  }

  isSelected(evaluationId: number): boolean {
    return this.selectedEvaluationIds.has(evaluationId);
  }

  selectAllGm(): void {
    this.pendingGmApprovals.forEach(e => this.selectedEvaluationIds.add(e.evaluationId));
  }

  selectAllHr(): void {
    this.pendingHrProcessing.forEach(e => this.selectedEvaluationIds.add(e.evaluationId));
  }

  clearSelection(): void {
    this.selectedEvaluationIds.clear();
  }

  getSelectedCount(): number {
    return this.selectedEvaluationIds.size;
  }

  // Get count of selected GM pending items
  getSelectedGmCount(): number {
    return Array.from(this.selectedEvaluationIds).filter(id => 
      this.pendingGmApprovals.some(e => e.evaluationId === id)
    ).length;
  }

  // Get count of selected HR pending items
  getSelectedHrCount(): number {
    return Array.from(this.selectedEvaluationIds).filter(id => 
      this.pendingHrProcessing.some(e => e.evaluationId === id)
    ).length;
  }

  // Bulk approval actions
  gmBulkApprove(): void {
    if (this.selectedEvaluationIds.size === 0) {
      this.error = 'Please select at least one evaluation to approve.';
      return;
    }

    const gmIds = Array.from(this.selectedEvaluationIds).filter(id => 
      this.pendingGmApprovals.some(e => e.evaluationId === id)
    );

    if (gmIds.length === 0) {
      this.error = 'No GM pending evaluations selected.';
      return;
    }

    this.bulkLoading = true;
    this.error = null;
    this.successMessage = null;

    this.evaluationService.gmBulkApprove({
      evaluationIds: gmIds,
      comment: this.bulkComment || 'Bulk approved by GM'
    }).subscribe({
      next: (result) => {
        this.bulkResult = result;
        this.successMessage = result.message;
        this.bulkLoading = false;
        this.loadBulkApprovalData();
        this.clearSelection();
        this.bulkComment = '';
      },
      error: (err) => {
        this.error = err.error?.error || 'Failed to process bulk approval.';
        this.bulkLoading = false;
        console.error('Error during bulk approval:', err);
      }
    });
  }

  hrBulkProcess(): void {
    if (this.selectedEvaluationIds.size === 0) {
      this.error = 'Please select at least one evaluation to process.';
      return;
    }

    const hrIds = Array.from(this.selectedEvaluationIds).filter(id => 
      this.pendingHrProcessing.some(e => e.evaluationId === id)
    );

    if (hrIds.length === 0) {
      this.error = 'No HR pending evaluations selected.';
      return;
    }

    this.bulkLoading = true;
    this.error = null;
    this.successMessage = null;

    this.evaluationService.hrBulkProcess({
      evaluationIds: hrIds,
      comment: this.bulkComment || 'Bulk processed by HR'
    }).subscribe({
      next: (result) => {
        this.bulkResult = result;
        this.successMessage = result.message;
        this.bulkLoading = false;
        this.loadBulkApprovalData();
        this.clearSelection();
        this.bulkComment = '';
      },
      error: (err) => {
        this.error = err.error?.error || 'Failed to process bulk HR processing.';
        this.bulkLoading = false;
        console.error('Error during bulk HR processing:', err);
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
    if (statusLower.includes('pending_hr')) return 'bg-pink-100 text-pink-800';
    if (statusLower.includes('v2_active_goals')) return 'bg-teal-100 text-teal-800';
    if (statusLower.includes('returned_for_activation')) return 'bg-orange-100 text-orange-800';
    if (statusLower.includes('deferred')) return 'bg-indigo-100 text-indigo-800';
    if (statusLower.includes('completed')) return 'bg-green-100 text-green-800';
    if (statusLower.includes('rejected')) return 'bg-red-100 text-red-800';
    
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
      case 'EMPLOYEE': return 'bg-indigo-100 text-indigo-800';
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
      'HR': 'Human Resources',
      'EMPLOYEE': 'My Evaluation'
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

  getCompletedCount(): number {
    return this.allEvaluations.filter(e => e.status.toLowerCase().includes('completed')).length;
  }

  getPendingCountForAll(): number {
    return this.allEvaluations.filter(e => e.status.toLowerCase().includes('pending')).length;
  }

  getScoreColor(score: number | undefined): string {
    if (!score) return 'text-gray-500';
    if (score >= 80) return 'text-green-600';
    if (score >= 60) return 'text-yellow-600';
    return 'text-red-600';
  }

  canShowBulkTab(): boolean {
    return this.hasGmRole || this.hasHrRole;
  }
}
