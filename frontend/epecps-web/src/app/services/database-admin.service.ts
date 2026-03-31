import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserWithRoles {
  userId: number;
  fullName: string;
  email: string;
  status: string;
  department: string;
  departmentId: number;
  roles: RoleInfo[];
}

export interface RoleInfo {
  roleId: number;
  roleName: string;
}

export interface Role {
  roleId: number;
  name: string;
}

export interface DatabaseStatus {
  message: string;
  statistics: {
    users: number;
    roles: number;
    cycles: number;
    userRoleAssignments: number;
  };
  endpoints: any;
}

export interface ImportRowError {
  rowNumber: number;
  message: string;
}

export interface UsersProjectsImportResult {
  totalRows: number;
  createdUsers: number;
  updatedUsers: number;
  createdRoleAssignments: number;
  removedRoleAssignments: number;
  createdProjects: number;
  updatedProjects: number;
  createdAssignments: number;
  updatedAssignments: number;
  createdManagerMappings: number;
  updatedManagerMappings: number;
  createdDepartmentHodMappings: number;
  skippedRows: number;
  errors: ImportRowError[];
}

@Injectable({
  providedIn: 'root'
})
export class DatabaseAdminService {
  private readonly baseUrl = `${environment.apiUrl}/api/admin/database`;
  private readonly importBaseUrl = `${environment.apiUrl}/api/v1/admin/import`;

  constructor(private http: HttpClient) {}

  /**
   * Seed the database with initial data (roles, cycles)
   */
  seedDatabase(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/seed`, {});
  }

  /**
   * Assign all roles to the current authenticated user
   */
  assignAllRolesToMe(): Observable<{ message: string; email: string; roles: string[] }> {
    return this.http.post<{ message: string; email: string; roles: string[] }>(
      `${this.baseUrl}/assign-all-roles-to-me`,
      {}
    );
  }

  /**
   * Get database status and statistics
   */
  getDatabaseStatus(): Observable<DatabaseStatus> {
    return this.http.get<DatabaseStatus>(`${this.baseUrl}/status`);
  }

  /**
   * Get all users with their roles
   */
  getAllUsers(): Observable<UserWithRoles[]> {
    return this.http.get<UserWithRoles[]>(`${this.baseUrl}/users`);
  }

  /**
   * Get all available roles
   */
  getAllRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(`${this.baseUrl}/roles`);
  }

  /**
   * Get roles for a specific user
   */
  getUserRoles(userId: number): Observable<{ userId: number; fullName: string; email: string; roles: RoleInfo[] }> {
    return this.http.get<{ userId: number; fullName: string; email: string; roles: RoleInfo[] }>(
      `${this.baseUrl}/users/${userId}/roles`
    );
  }

  /**
   * Assign a role to a user
   */
  assignRoleToUser(userId: number, roleId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/users/${userId}/roles`,
      { roleId }
    );
  }

  /**
   * Remove a role from a user
   */
  removeRoleFromUser(userId: number, roleId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.baseUrl}/users/${userId}/roles/${roleId}`
    );
  }

  /**
   * Assign multiple roles to a user at once
   */
  assignMultipleRoles(userId: number, roleIds: number[]): Observable<{ message: string; addedRoles: string[]; skippedRoles: string[] }> {
    return this.http.post<{ message: string; addedRoles: string[]; skippedRoles: string[] }>(
      `${this.baseUrl}/users/${userId}/roles/bulk`,
      { roleIds }
    );
  }

  downloadUsersProjectsTemplate(): Observable<Blob> {
    return this.http.get(`${this.importBaseUrl}/template`, { responseType: 'blob' });
  }

  importUsersProjects(file: File): Observable<UsersProjectsImportResult> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<UsersProjectsImportResult>(`${this.importBaseUrl}/users-projects`, form);
  }
}
