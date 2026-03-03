import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ===========================
// RM Goal Assignment Models
// ===========================

export interface GoalLibraryItemDto {
  id: string;
  name: string;
  description?: string;
  categoryName: string;
  templateName: string;
  targetScore: number;
  maxScore: number;
  isMandatory: boolean;
}

export interface RmEmployeeDto {
  userId: number;
  fullName: string;
  email: string;
  department: string;
}

export interface AssignGoalItemDto {
  goalItemId: string;
  title?: string;
  description?: string;
  customActivities: string[];
}

export interface RmAssignGoalsDto {
  employeeUserId: number;
  goalPeriodMonths: number;
  startDate: string;
  dueDate: string;
  goals: AssignGoalItemDto[];
}

export interface RmAssignGoalsResponseDto {
  goalSetId: string;
  goalCount: number;
  employeeName: string;
  message: string;
}

export interface GoalAssignmentListDto {
  id: string;
  goalSetId: string;
  employeeUserId: number;
  employeeName: string;
  employeeEmail: string;
  goalItemName: string;
  categoryName: string;
  title: string;
  description?: string;
  targetScore: number;
  status: string;
  startDate: string;
  dueDate: string;
  createdAt: string;
}

/**
 * Service for RM goal assignment operations
 */
@Injectable({
  providedIn: 'root'
})
export class RmGoalAssignmentService {
  private readonly baseUrl = `${environment.apiUrl}/api/rm/goals`;

  constructor(private http: HttpClient) {}

  /**
   * Get all goals from the system goal library
   */
  getGoalLibrary(): Observable<GoalLibraryItemDto[]> {
    return this.http.get<GoalLibraryItemDto[]>(`${this.baseUrl}/library`);
  }

  /**
   * Get employees managed by this RM
   */
  getMyEmployees(): Observable<RmEmployeeDto[]> {
    return this.http.get<RmEmployeeDto[]>(`${this.baseUrl}/employees`);
  }

  /**
   * Assign goals to an employee
   */
  assignGoals(dto: RmAssignGoalsDto): Observable<RmAssignGoalsResponseDto> {
    return this.http.post<RmAssignGoalsResponseDto>(`${this.baseUrl}/assign`, dto);
  }

  /**
   * Get all assignments made by this RM
   */
  getMyAssignments(): Observable<GoalAssignmentListDto[]> {
    return this.http.get<GoalAssignmentListDto[]>(`${this.baseUrl}/assignments`);
  }

  /**
   * Get assignments for a specific employee
   */
  getAssignmentsForEmployee(employeeUserId: number): Observable<GoalAssignmentListDto[]> {
    return this.http.get<GoalAssignmentListDto[]>(`${this.baseUrl}/assignments/employee/${employeeUserId}`);
  }
}
