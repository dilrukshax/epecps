import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface MeResponse {
  userId: number;
  fullName: string;
  email: string;
  status: string;
  isActive: boolean;
  departmentId: number;
  departmentName: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = `${environment.apiUrl.replace(/\/+$/, '')}/api/v1`;

  constructor(private http: HttpClient) {}

  getMe() {
    return this.http.get<MeResponse>(`${this.baseUrl}/auth/me`);
  }
}
