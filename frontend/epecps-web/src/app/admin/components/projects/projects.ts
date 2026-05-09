import { Component, OnInit } from '@angular/core';
import { AdminProjectService, ProjectDto, CreateProjectDto, UpdateProjectDto } from '../../../services/admin-project.service';
import { AdminUserService, UserAdminDto } from '../../../services/admin-user.service';

@Component({
  selector: 'app-admin-projects',
  templateUrl: './projects.html',
  styleUrls: ['./projects.css'],
  standalone: false
})
export class Projects implements OnInit {
  projects: ProjectDto[] = [];
  users: UserAdminDto[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  showForm = false;
  showTlForm = false;
  editingProjectId: number | null = null;
  
  // Form models
  formData: CreateProjectDto = { projectCode: '', projectName: '', status: 'Active', projectManagerUserId: undefined, supervisorUserId: undefined };
  tlFormData = { projectId: 0, userId: 0 };

  statusOptions = ['Active', 'Completed', 'On Hold', 'Cancelled'];

  constructor(
    private projectService: AdminProjectService,
    private userService: AdminUserService
  ) {}

  ngOnInit(): void {
    this.loadProjects();
    this.loadUsers();
  }

  loadProjects(): void {
    this.loading = true;
    this.projectService.getAll().subscribe({
      next: (data) => {
        this.projects = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load projects';
        this.loading = false;
      }
    });
  }

  loadUsers(): void {
    this.userService.getAll().subscribe({
      next: (data) => this.users = data,
      error: () => console.error('Failed to load users')
    });
  }

  openCreateForm(): void {
    this.showForm = true;
    this.editingProjectId = null;
    this.formData = { projectCode: '', projectName: '', status: 'Active', projectManagerUserId: undefined, supervisorUserId: undefined };
  }

  openEditForm(proj: ProjectDto): void {
    this.showForm = true;
    this.editingProjectId = proj.projectId;
    this.formData = { 
      projectCode: proj.projectCode, 
      projectName: proj.projectName, 
      status: proj.status,
      projectManagerUserId: proj.projectManagerUserId,
      supervisorUserId: proj.supervisorUserId
    };
  }

  closeForm(): void {
    this.showForm = false;
  }

  saveProject(): void {
    if (this.editingProjectId) {
      this.projectService.update(this.editingProjectId, this.formData).subscribe({
        next: () => {
          this.successMessage = 'Project updated successfully';
          this.closeForm();
          this.loadProjects();
        },
        error: (err) => this.error = err.error || 'Failed to update project'
      });
    } else {
      this.projectService.create(this.formData).subscribe({
        next: () => {
          this.successMessage = 'Project created successfully';
          this.closeForm();
          this.loadProjects();
        },
        error: (err) => this.error = err.error || 'Failed to create project'
      });
    }
  }

  deleteProject(id: number): void {
    if (confirm('Are you sure you want to delete this project?')) {
      this.projectService.delete(id).subscribe({
        next: () => {
          this.successMessage = 'Project deleted successfully';
          this.loadProjects();
        },
        error: (err) => this.error = err.error || 'Failed to delete project'
      });
    }
  }

  openAssignTlForm(projId: number): void {
    this.showTlForm = true;
    this.tlFormData = { projectId: projId, userId: 0 };
  }

  closeTlForm(): void {
    this.showTlForm = false;
  }

  assignTl(): void {
    if (!this.tlFormData.userId) return;
    this.projectService.assignTechLead(this.tlFormData.projectId, this.tlFormData.userId).subscribe({
      next: () => {
        this.successMessage = 'Tech Lead assigned successfully';
        this.closeTlForm();
        this.loadProjects();
      },
      error: (err) => this.error = err.error || 'Failed to assign Tech Lead'
    });
  }

  removeTl(projId: number, userId: number): void {
    if (confirm('Are you sure you want to remove this Tech Lead?')) {
      this.projectService.removeTechLead(projId, userId).subscribe({
        next: () => {
          this.successMessage = 'Tech Lead removed successfully';
          this.loadProjects();
        },
        error: (err) => this.error = err.error || 'Failed to remove Tech Lead'
      });
    }
  }

  clearMessages(): void {
    this.error = null;
    this.successMessage = null;
  }
}
