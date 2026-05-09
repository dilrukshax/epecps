import { Component, OnInit } from '@angular/core';
import { AdminDepartmentService, DepartmentDto, CreateDepartmentDto, UpdateDepartmentDto } from '../../../services/admin-department.service';
import { AdminUserService, UserAdminDto } from '../../../services/admin-user.service';

@Component({
  selector: 'app-admin-departments',
  templateUrl: './departments.html',
  styleUrls: ['./departments.css'],
  standalone: false
})
export class Departments implements OnInit {
  departments: DepartmentDto[] = [];
  users: UserAdminDto[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  showForm = false;
  showHodForm = false;
  editingDepartmentId: number | null = null;
  
  // Form models
  formData: CreateDepartmentDto = { name: '', parentDeptId: undefined };
  hodFormData = { departmentId: 0, userId: 0 };

  constructor(
    private deptService: AdminDepartmentService,
    private userService: AdminUserService
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
    this.loadUsers();
  }

  loadDepartments(): void {
    this.loading = true;
    this.deptService.getAll().subscribe({
      next: (data) => {
        this.departments = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load departments';
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
    this.editingDepartmentId = null;
    this.formData = { name: '', parentDeptId: undefined };
  }

  openEditForm(dept: DepartmentDto): void {
    this.showForm = true;
    this.editingDepartmentId = dept.deptId;
    this.formData = { name: dept.name, parentDeptId: dept.parentDeptId };
  }

  closeForm(): void {
    this.showForm = false;
  }

  saveDepartment(): void {
    if (this.editingDepartmentId) {
      this.deptService.update(this.editingDepartmentId, this.formData).subscribe({
        next: () => {
          this.successMessage = 'Department updated successfully';
          this.closeForm();
          this.loadDepartments();
        },
        error: (err) => this.error = err.error || 'Failed to update department'
      });
    } else {
      this.deptService.create(this.formData).subscribe({
        next: () => {
          this.successMessage = 'Department created successfully';
          this.closeForm();
          this.loadDepartments();
        },
        error: (err) => this.error = err.error || 'Failed to create department'
      });
    }
  }

  deleteDepartment(id: number): void {
    if (confirm('Are you sure you want to delete this department?')) {
      this.deptService.delete(id).subscribe({
        next: () => {
          this.successMessage = 'Department deleted successfully';
          this.loadDepartments();
        },
        error: (err) => this.error = err.error || 'Failed to delete department'
      });
    }
  }

  openAssignHodForm(deptId: number): void {
    this.showHodForm = true;
    this.hodFormData = { departmentId: deptId, userId: 0 };
  }

  closeHodForm(): void {
    this.showHodForm = false;
  }

  assignHod(): void {
    if (!this.hodFormData.userId) return;
    this.deptService.assignHod(this.hodFormData.departmentId, this.hodFormData.userId).subscribe({
      next: () => {
        this.successMessage = 'HOD assigned successfully';
        this.closeHodForm();
        this.loadDepartments();
      },
      error: (err) => this.error = err.error || 'Failed to assign HOD'
    });
  }

  removeHod(deptId: number, userId: number): void {
    if (confirm('Are you sure you want to remove this HOD?')) {
      this.deptService.removeHod(deptId, userId).subscribe({
        next: () => {
          this.successMessage = 'HOD removed successfully';
          this.loadDepartments();
        },
        error: (err) => this.error = err.error || 'Failed to remove HOD'
      });
    }
  }

  clearMessages(): void {
    this.error = null;
    this.successMessage = null;
  }
}
