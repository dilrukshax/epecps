/**
 * Data Transfer Objects for Score Template Module
 * These interfaces match the backend DTOs
 */

// Enums
export enum ScoreItemType {
  Checklist = 0,
  Rating = 1,
  Numeric = 2
}

// Item DTOs
export interface ScoreItemDto {
  id: string;
  scoreCategoryId: string;
  name: string;
  description?: string;
  itemType: ScoreItemType;
  maxScore: number;
  weightWithinCategory?: number | null;
  isMandatory: boolean;
  evidenceRequired: boolean;
  evidenceHint?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateScoreItemDto {
  name: string;
  description?: string;
  itemType: ScoreItemType;
  maxScore: number;
  weightWithinCategory?: number | null;
  isMandatory: boolean;
  evidenceRequired: boolean;
  evidenceHint?: string;
  displayOrder: number;
}

export interface UpdateScoreItemDto {
  name: string;
  description?: string;
  itemType: ScoreItemType;
  maxScore: number;
  weightWithinCategory?: number | null;
  isMandatory: boolean;
  evidenceRequired: boolean;
  evidenceHint?: string;
  displayOrder: number;
  isActive: boolean;
}

// Category DTOs
export interface ScoreCategoryDto {
  id: string;
  scoreTemplateId: string;
  name: string;
  description?: string;
  weightPercent: number;
  maxScore?: number | null;
  displayOrder: number;
  isActive: boolean;
  items: ScoreItemDto[];
}

export interface CreateScoreCategoryDto {
  name: string;
  description?: string;
  weightPercent: number;
  maxScore?: number | null;
  displayOrder: number;
}

export interface UpdateScoreCategoryDto {
  name: string;
  description?: string;
  weightPercent: number;
  maxScore?: number | null;
  displayOrder: number;
  isActive: boolean;
}

// Template DTOs
export interface ScoreTemplateListDto {
  id: string;
  name: string;
  description?: string;
  version: number;
  isPublished: boolean;
  isArchived: boolean;
  categoryCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface ScoreTemplateDetailDto {
  id: string;
  name: string;
  description?: string;
  version: number;
  isPublished: boolean;
  isArchived: boolean;
  createdAt: string;
  createdByUserId: number;
  updatedAt?: string;
  updatedByUserId?: number | null;
  categories: ScoreCategoryDto[];
}

export interface CreateScoreTemplateDto {
  name: string;
  description?: string;
}

export interface UpdateScoreTemplateDto {
  name: string;
  description?: string;
}

// Reorder DTOs
export interface ReorderItemDto {
  id: string;
  displayOrder: number;
}

export interface ReorderRequestDto {
  items: ReorderItemDto[];
}

// API Error Response
export interface ApiErrorResponse {
  message: string;
  errors?: { [key: string]: string[] };
}
