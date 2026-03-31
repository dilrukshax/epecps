import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { EvaluationService } from '../../../services/evaluation.service';
import { PipActionItemDto, PipCaseDto } from '../../../models/evaluation.models';

@Component({
  selector: 'app-hr-pip-cases',
  templateUrl: './hr-pip-cases.component.html',
  styleUrls: ['./hr-pip-cases.component.css'],
  standalone: false
})
export class HrPipCasesComponent implements OnInit {
  loading = false;
  saving = false;
  error: string | null = null;
  successMessage: string | null = null;

  statusFilter = '';
  assignedToMeOnly = true;
  highlightedEvaluationId: number | null = null;

  currentUserId: number | null = null;
  pipCases: PipCaseDto[] = [];
  expandedCaseId: number | null = null;

  caseForms: { [pipCaseId: number]: { status: string; dueDate: string; reason: string } } = {};
  actionItemForms: { [pipCaseId: number]: { title: string; description: string; externalTrainingLink: string; dueDate: string } } = {};

  constructor(
    private evaluationService: EvaluationService,
    private authService: AuthService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUser()?.userId ?? null;

    this.route.queryParamMap.subscribe(params => {
      const evaluationIdRaw = params.get('evaluationId');
      this.highlightedEvaluationId = evaluationIdRaw ? Number(evaluationIdRaw) : null;
      this.loadPipCases();
    });
  }

  loadPipCases(): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    const assignedHrUserId = this.assignedToMeOnly ? (this.currentUserId ?? undefined) : undefined;
    const status = this.statusFilter.trim() ? this.statusFilter.trim() : undefined;

    this.evaluationService.getPipCases(assignedHrUserId, status).subscribe({
      next: (cases) => {
        this.pipCases = cases;
        this.initializeForms(cases);
        this.loading = false;

        if (this.highlightedEvaluationId) {
          const match = this.pipCases.find(c => c.evaluationId === this.highlightedEvaluationId);
          if (match) {
            this.expandedCaseId = match.pipCaseId;
          }
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.error || err.error?.message || 'Failed to load PIP cases.';
      }
    });
  }

  private initializeForms(cases: PipCaseDto[]): void {
    const caseForms: { [pipCaseId: number]: { status: string; dueDate: string; reason: string } } = {};
    const actionForms: { [pipCaseId: number]: { title: string; description: string; externalTrainingLink: string; dueDate: string } } = {};

    cases.forEach(pipCase => {
      caseForms[pipCase.pipCaseId] = {
        status: pipCase.status || 'Open',
        dueDate: pipCase.dueDate ? this.toDateInputValue(pipCase.dueDate) : '',
        reason: pipCase.reason || ''
      };

      actionForms[pipCase.pipCaseId] = {
        title: '',
        description: '',
        externalTrainingLink: '',
        dueDate: ''
      };
    });

    this.caseForms = caseForms;
    this.actionItemForms = actionForms;
  }

  toggleCaseExpansion(pipCaseId: number): void {
    this.expandedCaseId = this.expandedCaseId === pipCaseId ? null : pipCaseId;
  }

  saveCase(pipCase: PipCaseDto): void {
    const form = this.caseForms[pipCase.pipCaseId];
    if (!form) return;

    this.saving = true;
    this.error = null;
    this.successMessage = null;

    this.evaluationService.updatePipCase(pipCase.pipCaseId, {
      status: form.status || undefined,
      dueDate: form.dueDate || undefined,
      reason: form.reason || undefined
    }).subscribe({
      next: (updated) => {
        this.replaceCase(updated);
        this.saving = false;
        this.successMessage = 'PIP case updated successfully.';
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || err.error?.message || 'Failed to update PIP case.';
      }
    });
  }

  addActionItem(pipCase: PipCaseDto): void {
    const form = this.actionItemForms[pipCase.pipCaseId];
    if (!form) return;

    if (!form.title.trim()) {
      this.error = 'Action item title is required.';
      return;
    }

    this.saving = true;
    this.error = null;
    this.successMessage = null;

    this.evaluationService.addPipActionItem(pipCase.pipCaseId, {
      title: form.title.trim(),
      description: form.description.trim() || undefined,
      externalTrainingLink: form.externalTrainingLink.trim() || undefined,
      dueDate: form.dueDate || undefined
    }).subscribe({
      next: (updated) => {
        this.replaceCase(updated);
        this.actionItemForms[pipCase.pipCaseId] = {
          title: '',
          description: '',
          externalTrainingLink: '',
          dueDate: ''
        };
        this.saving = false;
        this.successMessage = 'PIP action item created successfully.';
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || err.error?.message || 'Failed to add PIP action item.';
      }
    });
  }

  setActionItemStatus(actionItem: PipActionItemDto, status: string): void {
    this.saving = true;
    this.error = null;
    this.successMessage = null;

    this.evaluationService.updatePipActionItem(actionItem.pipActionItemId, {
      status
    }).subscribe({
      next: (updated) => {
        this.replaceCase(updated);
        this.saving = false;
        this.successMessage = 'Action item updated successfully.';
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || err.error?.message || 'Failed to update action item.';
      }
    });
  }

  getCaseStatusBadgeClass(status: string): string {
    const normalized = (status || '').toLowerCase();
    if (normalized.includes('open')) return 'bg-amber-100 text-amber-800';
    if (normalized.includes('inprogress') || normalized.includes('in_progress')) return 'bg-blue-100 text-blue-800';
    if (normalized.includes('closed')) return 'bg-green-100 text-green-800';
    return 'bg-gray-100 text-gray-800';
  }

  getActionStatusBadgeClass(status: string): string {
    const normalized = (status || '').toLowerCase();
    if (normalized.includes('pending')) return 'bg-amber-100 text-amber-800';
    if (normalized.includes('inprogress') || normalized.includes('in_progress')) return 'bg-blue-100 text-blue-800';
    if (normalized.includes('done') || normalized.includes('completed')) return 'bg-green-100 text-green-800';
    return 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  private replaceCase(updated: PipCaseDto): void {
    this.pipCases = this.pipCases.map(c => c.pipCaseId === updated.pipCaseId ? updated : c);

    this.caseForms[updated.pipCaseId] = {
      status: updated.status || 'Open',
      dueDate: updated.dueDate ? this.toDateInputValue(updated.dueDate) : '',
      reason: updated.reason || ''
    };
  }

  private toDateInputValue(date: Date | string): string {
    const parsed = new Date(date);
    if (Number.isNaN(parsed.getTime())) return '';
    return parsed.toISOString().slice(0, 10);
  }
}
