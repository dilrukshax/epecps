import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface DepartmentHodDto {
  userId: number;
  fullName: string;
  email: string;
}

export interface DepartmentDto {
  deptId: number;
  name: string;
  parentDeptId?: number;
  parentDeptName?: string;
  hods: DepartmentHodDto[];
}

export interface CreateDepartmentDto {
  name: string;
  parentDeptId?: number;
}

export interface UpdateDepartmentDto {
  name: string;
  parentDeptId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminDepartmentService {
  private apiUrl = `${environment.apiUrl}/api/v1/admin/departments`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DepartmentDto[]> {
    return this.http.get<DepartmentDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<DepartmentDto> {
    return this.http.get<DepartmentDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateDepartmentDto): Observable<DepartmentDto> {
    return this.http.post<DepartmentDto>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateDepartmentDto): Observable<DepartmentDto> {
    return this.http.put<DepartmentDto>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  assignHod(departmentId: number, userId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${departmentId}/hod`, { userId });
  }

  removeHod(departmentId: number, userId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${departmentId}/hod/${userId}`);
  }
}
