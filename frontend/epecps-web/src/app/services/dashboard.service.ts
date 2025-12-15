import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardDataDto, DashboardStatsDto, LatestActivityDto } from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/Dashboard`;

  constructor(private http: HttpClient) {}

  /**
   * Get comprehensive dashboard data
   */
  getDashboardData(): Observable<DashboardDataDto> {
    return this.http.get<DashboardDataDto>(this.baseUrl);
  }

  /**
   * Get dashboard statistics only
   */
  getDashboardStats(): Observable<DashboardStatsDto> {
    return this.http.get<DashboardStatsDto>(`${this.baseUrl}/stats`);
  }

  /**
   * Get latest activities
   */
  getLatestActivities(count: number = 10): Observable<LatestActivityDto[]> {
    return this.http.get<LatestActivityDto[]>(`${this.baseUrl}/activities?count=${count}`);
  }
}
