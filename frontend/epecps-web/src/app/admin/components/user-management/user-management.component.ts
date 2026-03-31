import { Component, OnInit } from '@angular/core';
import {
  DatabaseAdminService,
  UserWithRoles,
  Role,
  UsersProjectsImportResult
} from '../../../services/database-admin.service';
import { EvaluationService } from '../../../services/evaluation.service';
import { WorkflowReviewWeightDto } from '../../../models/evaluation.models';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.css'],
  standalone: false
})
export class UserManagementComponent implements OnInit {
  users: UserWithRoles[] = [];
  availableRoles: Role[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  selectedUser: UserWithRoles | null = null;
  showRoleDialog = false;
  selectedRoleIds: number[] = [];

  // Database status
  databaseStats: any = null;
  loadingStats = false;
  seeding = false;
  assigningAllRoles = false;
  importFile: File | null = null;
  importing = false;
  importResult: UsersProjectsImportResult | null = null;

  reviewWeights: WorkflowReviewWeightDto[] = [];
  loadingReviewWeights = false;
  savingReviewWeights = false;
  reviewWeightTotal = 0;

  constructor(
    private dbAdminService: DatabaseAdminService,
    private evaluationService: EvaluationService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
    this.loadDatabaseStatus();
    this.loadReviewWeights();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    this.dbAdminService.getAllUsers().subscribe({
      next: (users: UserWithRoles[]) => {
        this.users = users;
        this.loading = false;
      },
      error: (err: any) => {
        this.error = 'Failed to load users';
        this.loading = false;
        console.error('Error loading users:', err);
      }
    });
  }

  loadRoles(): void {
    this.dbAdminService.getAllRoles().subscribe({
      next: (roles: Role[]) => {
        this.availableRoles = roles;
      },
      error: (err: any) => {
        console.error('Error loading roles:', err);
      }
    });
  }

  loadDatabaseStatus(): void {
    this.loadingStats = true;
    this.dbAdminService.getDatabaseStatus().subscribe({
      next: (status: any) => {
        this.databaseStats = status.statistics;
        this.loadingStats = false;
      },
      error: (err: any) => {
        console.error('Error loading database status:', err);
        this.loadingStats = false;
      }
    });
  }

  seedDatabase(): void {
    if (!confirm('Are you sure you want to seed the database? This will add default roles and cycles.')) {
      return;
    }

    this.seeding = true;
    this.error = null;
    this.successMessage = null;

    this.dbAdminService.seedDatabase().subscribe({
      next: (response: any) => {
        this.successMessage = response.message;
        this.seeding = false;
        this.loadDatabaseStatus();
        this.loadRoles();
      },
      error: (err: any) => {
        this.error = err.error?.error || 'Failed to seed database';
        this.seeding = false;
        console.error('Error seeding database:', err);
      }
    });
  }

  assignAllRolesToMe(): void {
    if (!confirm('Are you sure you want to assign all roles to yourself?')) {
      return;
    }

    this.assigningAllRoles = true;
    this.error = null;
    this.successMessage = null;

    this.dbAdminService.assignAllRolesToMe().subscribe({
      next: (response: any) => {
        this.successMessage = response.message;
        this.assigningAllRoles = false;
        this.loadUsers();
      },
      error: (err: any) => {
        this.error = err.error?.error || 'Failed to assign roles';
        this.assigningAllRoles = false;
        console.error('Error assigning roles:', err);
      }
    });
  }

  onImportFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.importFile = target.files && target.files.length > 0 ? target.files[0] : null;
  }

  downloadImportTemplate(): void {
    this.dbAdminService.downloadUsersProjectsTemplate().subscribe({
      next: (blob: Blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `users-projects-import-template-${new Date().toISOString().slice(0, 10)}.xlsx`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.error = 'Failed to download import template.';
      }
    });
  }

  importUsersProjects(): void {
    if (!this.importFile) {
      this.error = 'Please select an Excel file first.';
      return;
    }

    this.importing = true;
    this.error = null;
    this.successMessage = null;
    this.importResult = null;

    this.dbAdminService.importUsersProjects(this.importFile).subscribe({
      next: (result) => {
        this.importResult = result;
        this.importing = false;
        this.successMessage = 'Users/projects import completed.';
        this.loadUsers();
        this.loadDatabaseStatus();
      },
      error: (err) => {
        this.importing = false;
        this.error = err.error?.message || 'Users/projects import failed.';
      }
    });
  }

  loadReviewWeights(): void {
    this.loadingReviewWeights = true;

    this.evaluationService.getReviewWeights().subscribe({
      next: (weights) => {
        const sortOrder: { [key: string]: number } = {
          Self: 1,
          TL: 2,
          RM: 3,
          Peer1: 4,
          Peer2: 5
        };

        this.reviewWeights = [...weights].sort((a, b) =>
          (sortOrder[a.reviewerKey] || 99) - (sortOrder[b.reviewerKey] || 99));
        this.recalculateReviewWeightTotal();
        this.loadingReviewWeights = false;
      },
      error: (err) => {
        this.loadingReviewWeights = false;
        console.error('Error loading workflow review weights:', err);
      }
    });
  }

  recalculateReviewWeightTotal(): void {
    this.reviewWeightTotal = this.reviewWeights
      .reduce((sum, weight) => sum + Number(weight.weightPercent || 0), 0);
  }

  isReviewWeightTotalValid(): boolean {
    return Math.abs(this.reviewWeightTotal - 100) <= 0.001;
  }

  saveReviewWeights(): void {
    this.recalculateReviewWeightTotal();

    if (!this.isReviewWeightTotalValid()) {
      this.error = 'Review weights must total exactly 100.';
      return;
    }

    this.savingReviewWeights = true;
    this.error = null;
    this.successMessage = null;

    const payload = {
      weights: this.reviewWeights.map(w => ({
        reviewerKey: w.reviewerKey,
        weightPercent: Number(w.weightPercent || 0)
      }))
    };

    this.evaluationService.updateReviewWeights(payload).subscribe({
      next: (updated) => {
        this.reviewWeights = updated;
        this.recalculateReviewWeightTotal();
        this.savingReviewWeights = false;
        this.successMessage = 'Workflow review weights updated successfully.';
      },
      error: (err) => {
        this.savingReviewWeights = false;
        this.error = err.error?.error || err.error?.message || 'Failed to update workflow review weights.';
      }
    });
  }

  openRoleDialog(user: UserWithRoles): void {
    this.selectedUser = user;
    this.selectedRoleIds = user.roles.map((r: any) => r.roleId);
    this.showRoleDialog = true;
    this.error = null;
    this.successMessage = null;
  }

  closeRoleDialog(): void {
    this.showRoleDialog = false;
    this.selectedUser = null;
    this.selectedRoleIds = [];
  }

  toggleRole(roleId: number): void {
    const index = this.selectedRoleIds.indexOf(roleId);
    if (index > -1) {
      this.selectedRoleIds.splice(index, 1);
    } else {
      this.selectedRoleIds.push(roleId);
    }
  }

  isRoleSelected(roleId: number): boolean {
    return this.selectedRoleIds.includes(roleId);
  }

  saveRoles(): void {
    if (!this.selectedUser) return;

    const userId = this.selectedUser.userId;
    const currentRoleIds = this.selectedUser.roles.map((r: any) => r.roleId);
    const rolesToAdd = this.selectedRoleIds.filter((id: number) => !currentRoleIds.includes(id));
    const rolesToRemove = currentRoleIds.filter((id: number) => !this.selectedRoleIds.includes(id));

    this.loading = true;
    this.error = null;
    this.successMessage = null;

    // Remove roles first
    const removePromises = rolesToRemove.map((roleId: number) =>
      this.dbAdminService.removeRoleFromUser(userId, roleId).toPromise()
    );

    // Then add roles
    const addPromises = rolesToAdd.map((roleId: number) =>
      this.dbAdminService.assignRoleToUser(userId, roleId).toPromise()
    );

    Promise.all([...removePromises, ...addPromises])
      .then(() => {
        this.successMessage = 'Roles updated successfully';
        this.loading = false;
        this.closeRoleDialog();
        this.loadUsers();
      })
      .catch((err: any) => {
        this.error = err.error?.error || 'Failed to update roles';
        this.loading = false;
        console.error('Error updating roles:', err);
      });
  }

  getRoleBadgeColor(roleName: string): string {
    const colors: { [key: string]: string } = {
      'Admin': 'bg-red-100 text-red-800',
      'GM': 'bg-purple-100 text-purple-800',
      'HOD': 'bg-indigo-100 text-indigo-800',
      'HR': 'bg-pink-100 text-pink-800',
      'TL': 'bg-blue-100 text-blue-800',
      'RM': 'bg-green-100 text-green-800',
      'Peer': 'bg-yellow-100 text-yellow-800',
      'Employee': 'bg-gray-100 text-gray-800'
    };
    return colors[roleName] || 'bg-gray-100 text-gray-800';
  }
}
