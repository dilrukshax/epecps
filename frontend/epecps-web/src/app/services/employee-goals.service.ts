import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  GoalFrameworkTemplateDto,
  GoalFrameworkCategoryDto,
  GoalFrameworkItemDto,
  GoalFrameworkGoalItemDto,
  CreatePersonalGoalDto,
  PersonalGoalListDto,
  PersonalGoalSetDto,
  PersonalGoalDetailDto,
  UpdatePersonalGoalDto,
  UpdatePersonalGoalScoreDto,
  CreatePersonalGoalActivityDto,
  UpdatePersonalGoalActivityDto
} from '../models/employee-goals.models';

/**
 * Service for Employee Goal-Setting
 * Handles both goal framework browsing and personal goal management
 */
@Injectable({
  providedIn: 'root'
})
export class EmployeeGoalsService {
  private readonly frameworkUrl = `${environment.apiUrl}/api/goal-framework`;
  private readonly goalsUrl = `${environment.apiUrl}/api/employee-goals`;

  constructor(private http: HttpClient) {}

  // ===========================
  // Goal Framework (Browse)
  // ===========================

  /**
   * Get all published, non-archived templates available for goal-setting
   */
  getTemplates(): Observable<GoalFrameworkTemplateDto[]> {
    return this.http.get<GoalFrameworkTemplateDto[]>(`${this.frameworkUrl}/templates`);
  }

  /**
   * Get all active categories for a specific template
   */
  getCategoriesByTemplate(templateId: string): Observable<GoalFrameworkCategoryDto[]> {
    return this.http.get<GoalFrameworkCategoryDto[]>(`${this.frameworkUrl}/templates/${templateId}/categories`);
  }

  /**
   * Get all active categories available for goal-setting
   */
  getCategories(): Observable<GoalFrameworkCategoryDto[]> {
    return this.http.get<GoalFrameworkCategoryDto[]>(`${this.frameworkUrl}/categories`);
  }

  /**
   * Get all active items for a given category
   */
  getItemsByCategory(categoryId: string): Observable<GoalFrameworkItemDto[]> {
    return this.http.get<GoalFrameworkItemDto[]>(`${this.frameworkUrl}/categories/${categoryId}/items`);
  }

  /**
   * Get all active goal items for a given item, including suggested activities
   */
  getGoalItemsByItem(itemId: string): Observable<GoalFrameworkGoalItemDto[]> {
    return this.http.get<GoalFrameworkGoalItemDto[]>(`${this.frameworkUrl}/items/${itemId}/goal-items`);
  }

  // ===========================
  // Personal Goals Management
  // ===========================

  /**
   * Create a new personal goal for the authenticated user
   */
  createPersonalGoal(dto: CreatePersonalGoalDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.goalsUrl, dto);
  }

  /**
   * Get all personal goals for the current user
   */
  getMyGoals(): Observable<PersonalGoalListDto[]> {
    return this.http.get<PersonalGoalListDto[]>(`${this.goalsUrl}/my`);
  }

  /**
   * Get personal goals grouped by goal sets
   */
  getMyGoalSets(): Observable<PersonalGoalSetDto[]> {
    return this.http.get<PersonalGoalSetDto[]>(`${this.goalsUrl}/my/sets`);
  }

  /**
   * Get detailed information about a specific personal goal
   */
  getGoalDetails(goalId: string): Observable<PersonalGoalDetailDto> {
    return this.http.get<PersonalGoalDetailDto>(`${this.goalsUrl}/${goalId}`);
  }

  /**
   * Update a personal goal
   */
  updatePersonalGoal(goalId: string, dto: UpdatePersonalGoalDto): Observable<void> {
    return this.http.put<void>(`${this.goalsUrl}/${goalId}`, dto);
  }

  /**
   * Update the score/progress of a personal goal
   */
  updateGoalScore(goalId: string, dto: UpdatePersonalGoalScoreDto): Observable<void> {
    return this.http.put<void>(`${this.goalsUrl}/${goalId}/score`, dto);
  }

  /**
   * Add a new activity to a personal goal
   */
  addActivity(goalId: string, dto: CreatePersonalGoalActivityDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.goalsUrl}/${goalId}/activities`, dto);
  }

  /**
   * Update an existing activity
   */
  updateActivity(goalId: string, activityId: string, dto: UpdatePersonalGoalActivityDto): Observable<void> {
    return this.http.put<void>(`${this.goalsUrl}/${goalId}/activities/${activityId}`, dto);
  }

  /**
   * Recalculate goal score based on completed activities
   */
  recalculateGoalScore(goalId: string): Observable<void> {
    return this.http.post<void>(`${this.goalsUrl}/${goalId}/recalculate-score`, {});
  }

  /**
   * Delete a personal goal (only if not submitted for evaluation)
   */
  deletePersonalGoal(goalId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.goalsUrl}/${goalId}`);
  }

  /**
   * Delete an activity from a personal goal
   */
  deleteActivity(goalId: string, activityId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.goalsUrl}/${goalId}/activities/${activityId}`);
  }

  /**
   * Delete an entire goal set (all goals in the set)
   */
  deleteGoalSet(goalSetId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.goalsUrl}/sets/${goalSetId}`);
  }

  /**
   * Submit a goal set for evaluation
   */
  submitGoalSetForEvaluation(goalSetId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.goalsUrl}/sets/${goalSetId}/submit-for-evaluation`, {});
  }
}
