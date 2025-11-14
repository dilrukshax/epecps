import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ScoreTemplateListDto,
  ScoreTemplateDetailDto,
  CreateScoreTemplateDto,
  UpdateScoreTemplateDto,
  CreateScoreCategoryDto,
  UpdateScoreCategoryDto
} from '../models/score-template.models';
import { environment } from '../../environments/environment';

/**
 * Service for managing score templates and categories
 * Communicates with the backend API
 */
@Injectable({
  providedIn: 'root'
})
export class ScoreTemplateService {
  private readonly apiUrl = `${environment.apiUrl}/api/v1/admin/templates`;

  constructor(private http: HttpClient) {}

  // ===========================
  // Template Management
  // ===========================

  /**
   * Get all score templates
   * @param includeArchived Whether to include archived templates
   */
  getTemplates(includeArchived: boolean = false): Observable<ScoreTemplateListDto[]> {
    const params = new HttpParams().set('includeArchived', includeArchived.toString());
    return this.http.get<ScoreTemplateListDto[]>(this.apiUrl, { params });
  }

  /**
   * Get a specific template with all categories and items
   * @param id Template ID
   */
  getTemplateById(id: string): Observable<ScoreTemplateDetailDto> {
    return this.http.get<ScoreTemplateDetailDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new score template
   * @param dto Template data
   */
  createTemplate(dto: CreateScoreTemplateDto): Observable<string> {
    return this.http.post<string>(this.apiUrl, dto);
  }

  /**
   * Update a template's basic information
   * @param id Template ID
   * @param dto Updated template data
   */
  updateTemplate(id: string, dto: UpdateScoreTemplateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Publish a template (makes it immutable and available for use)
   * @param id Template ID
   */
  publishTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/publish`, {});
  }

  /**
   * Clone a template to create a new draft version
   * @param id Template ID to clone
   */
  cloneTemplate(id: string): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/${id}/clone`, {});
  }

  /**
   * Archive a template (soft delete)
   * @param id Template ID
   */
  archiveTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/archive`, {});
  }

  // ===========================
  // Category Management
  // ===========================

  /**
   * Create a new category within a template
   * @param templateId Template ID
   * @param dto Category data
   */
  createCategory(templateId: string, dto: CreateScoreCategoryDto): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/${templateId}/categories`, dto);
  }

  /**
   * Update an existing category
   * @param categoryId Category ID
   * @param dto Updated category data
   */
  updateCategory(categoryId: string, dto: UpdateScoreCategoryDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/categories/${categoryId}`, dto);
  }

  /**
   * Delete a category (soft delete if template is published)
   * @param categoryId Category ID
   */
  deleteCategory(categoryId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/categories/${categoryId}`);
  }
}
