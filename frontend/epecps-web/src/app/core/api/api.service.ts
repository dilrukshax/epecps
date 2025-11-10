import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface MeResponse {
  name?: string;
  preferred_username?: string;
  roles?: string[];
  scopes?: string[];
  claims?: Record<string, any>;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = 'https://localhost:7275/api/v1';

  constructor(private http: HttpClient) {}

  getMe() {
    return this.http.get<MeResponse>(`${this.baseUrl}/auth/me`);
  }
}
