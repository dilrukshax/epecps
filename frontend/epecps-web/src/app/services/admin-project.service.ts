import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface ProjectTlDto {
  userId: number;
  fullName: string;
  email: string;
}

export interface ProjectDto {
  projectId: number;
  projectCode: string;
  projectName: string;
  status: string;
  projectManagerUserId?: number;
  projectManagerName?: string;
  supervisorUserId?: number;
  supervisorName?: string;
  createdAt: string;
  updatedAt?: string;
  techLeads: ProjectTlDto[];
}

export interface CreateProjectDto {
  projectCode: string;
  projectName: string;
  status: string;
  projectManagerUserId?: number;
  supervisorUserId?: number;
}

export interface UpdateProjectDto {
  projectCode: string;
  projectName: string;
  status: string;
  projectManagerUserId?: number;
  supervisorUserId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminProjectService {
  private apiUrl = `${environment.apiUrl}/api/v1/admin/projects`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ProjectDto[]> {
    return this.http.get<ProjectDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<ProjectDto> {
    return this.http.get<ProjectDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateProjectDto): Observable<ProjectDto> {
    return this.http.post<ProjectDto>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateProjectDto): Observable<ProjectDto> {
    return this.http.put<ProjectDto>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  assignTechLead(projectId: number, userId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${projectId}/tl`, { userId });
  }

  removeTechLead(projectId: number, userId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/tl/${userId}`);
  }
}
