import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface UserAdminDto {
  userId: number;
  fullName: string;
  email: string;
  status: string;
  isActive: boolean;
  deptId: number;
  departmentName: string;
  roles: string[];
  reportingManagerId?: number;
  reportingManagerName?: string;
}

export interface CreateUserAdminDto {
  fullName: string;
  email: string;
  status: string;
  deptId: number;
  roles: string[];
  reportingManagerId?: number;
}

export interface UpdateUserAdminDto {
  fullName: string;
  email: string;
  status: string;
  isActive: boolean;
  deptId: number;
  roles: string[];
  reportingManagerId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminUserService {
  private apiUrl = `${environment.apiUrl}/api/v1/admin/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<UserAdminDto[]> {
    return this.http.get<UserAdminDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<UserAdminDto> {
    return this.http.get<UserAdminDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateUserAdminDto): Observable<UserAdminDto> {
    return this.http.post<UserAdminDto>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateUserAdminDto): Observable<UserAdminDto> {
    return this.http.put<UserAdminDto>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
