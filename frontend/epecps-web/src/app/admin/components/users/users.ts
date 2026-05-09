import { Component, OnInit } from '@angular/core';
import { AdminUserService, UserAdminDto, CreateUserAdminDto, UpdateUserAdminDto } from '../../../services/admin-user.service';
import { AdminDepartmentService, DepartmentDto } from '../../../services/admin-department.service';
import { DatabaseAdminService, Role } from '../../../services/database-admin.service';

@Component({
  selector: 'app-admin-users',
  templateUrl: './users.html',
  styleUrls: ['./users.css'],
  standalone: false
})
export class Users implements OnInit {
  users: UserAdminDto[] = [];
  departments: DepartmentDto[] = [];
  availableRoles: Role[] = [];
  
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  showForm = false;
  editingUserId: number | null = null;
  
  // Form models
  formData: CreateUserAdminDto = { fullName: '', email: '', status: 'Active', deptId: 0, roles: [], reportingManagerId: undefined };

  statusOptions = ['Active', 'Inactive', 'Suspended'];

  constructor(
    private userService: AdminUserService,
    private deptService: AdminDepartmentService,
    private dbAdminService: DatabaseAdminService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadDepartments();
    this.loadRoles();
  }

  loadUsers(): void {
    this.loading = true;
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load users';
        this.loading = false;
      }
    });
  }

  loadDepartments(): void {
    this.deptService.getAll().subscribe({
      next: (data) => this.departments = data,
      error: () => console.error('Failed to load departments')
    });
  }

  loadRoles(): void {
    this.dbAdminService.getAllRoles().subscribe({
      next: (data) => this.availableRoles = data,
      error: () => console.error('Failed to load roles')
    });
  }

  openCreateForm(): void {
    this.showForm = true;
    this.editingUserId = null;
    this.formData = { fullName: '', email: '', status: 'Active', deptId: this.departments[0]?.deptId || 0, roles: ['Employee'], reportingManagerId: undefined };
  }

  openEditForm(user: UserAdminDto): void {
    this.showForm = true;
    this.editingUserId = user.userId;
    this.formData = { 
      fullName: user.fullName, 
      email: user.email, 
      status: user.status,
      deptId: user.deptId,
      roles: [...user.roles],
      reportingManagerId: user.reportingManagerId
    };
  }

  closeForm(): void {
    this.showForm = false;
  }

  toggleRole(roleName: string): void {
    const idx = this.formData.roles.indexOf(roleName);
    if (idx > -1) {
      this.formData.roles.splice(idx, 1);
    } else {
      this.formData.roles.push(roleName);
    }
  }

  saveUser(): void {
    if (this.editingUserId) {
      const updateDto: UpdateUserAdminDto = {
        ...this.formData,
        isActive: this.formData.status === 'Active'
      };
      
      this.userService.update(this.editingUserId, updateDto).subscribe({
        next: () => {
          this.successMessage = 'User updated successfully';
          this.closeForm();
          this.loadUsers();
        },
        error: (err) => this.error = err.error || 'Failed to update user'
      });
    } else {
      this.userService.create(this.formData).subscribe({
        next: () => {
          this.successMessage = 'User created successfully';
          this.closeForm();
          this.loadUsers();
        },
        error: (err) => this.error = err.error || 'Failed to create user'
      });
    }
  }

  deleteUser(id: number): void {
    if (confirm('Are you sure you want to deactivate this user?')) {
      this.userService.delete(id).subscribe({
        next: () => {
          this.successMessage = 'User deactivated successfully';
          this.loadUsers();
        },
        error: (err) => this.error = err.error || 'Failed to deactivate user'
      });
    }
  }

  clearMessages(): void {
    this.error = null;
    this.successMessage = null;
  }
}
