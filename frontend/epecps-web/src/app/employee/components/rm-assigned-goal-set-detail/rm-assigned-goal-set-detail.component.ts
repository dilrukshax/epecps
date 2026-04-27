import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GoalAssignmentListDto, RmGoalAssignmentService } from '../../../services/rm-goal-assignment.service';

@Component({
  selector: 'app-rm-assigned-goal-set-detail',
  templateUrl: './rm-assigned-goal-set-detail.component.html',
  styleUrls: ['./rm-assigned-goal-set-detail.component.css'],
  standalone: false
})
export class RmAssignedGoalSetDetailComponent implements OnInit {
  loading = false;
  error: string | null = null;
  assignments: GoalAssignmentListDto[] = [];
  employeeUserId: number | null = null;
  goalSetId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly rmService: RmGoalAssignmentService
  ) {}

  ngOnInit(): void {
    const employeeUserId = Number(this.route.snapshot.paramMap.get('employeeUserId'));
    const goalSetId = this.route.snapshot.paramMap.get('goalSetId');

    if (!employeeUserId || !goalSetId) {
      this.error = 'Invalid assignment details.';
      return;
    }

    this.employeeUserId = employeeUserId;
    this.goalSetId = goalSetId;
    this.loadAssignments();
  }

  get pageTitle(): string {
    if (this.assignments.length === 0) {
      return 'Assigned Goals';
    }

    const first = this.assignments[0];
    return `${first.employeeName} - ${this.cycleLabel}`;
  }

  get cycleLabel(): string {
    if (this.assignments.length === 0) {
      return '';
    }

    return `${new Date(this.assignments[0].startDate).getFullYear()} Evaluation Cycle`;
  }

  get assignmentPeriodLabel(): string {
    if (this.assignments.length === 0) {
      return '';
    }

    const first = this.assignments[0];
    return `${new Date(first.startDate).toLocaleDateString()} - ${new Date(first.dueDate).toLocaleDateString()}`;
  }

  get assignedOnLabel(): string {
    if (this.assignments.length === 0) {
      return '';
    }

    return new Date(this.assignments[0].createdAt).toLocaleDateString();
  }

  loadAssignments(): void {
    if (!this.employeeUserId || !this.goalSetId) {
      return;
    }

    this.loading = true;
    this.error = null;

    this.rmService.getAssignmentsForEmployee(this.employeeUserId).subscribe({
      next: (assignments) => {
        this.assignments = assignments
          .filter(assignment => assignment.goalSetId === this.goalSetId)
          .sort((a, b) => a.title.localeCompare(b.title));
        this.loading = false;

        if (this.assignments.length === 0) {
          this.error = 'No assigned goals were found for this employee and evaluation cycle.';
        }
      },
      error: (err) => {
        this.error = 'Failed to load assigned goals.';
        this.loading = false;
        console.error('Error loading assigned goal set:', err);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/employee/rm-assign-goals'], {
      queryParams: { tab: 'history' }
    });
  }
}
