/**
 * Models for Employee Goal-Setting module
 */

// Enums
export enum PersonalGoalStatus {
  Draft = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export enum ActivityStatus {
  NotStarted = 0,
  InProgress = 1,
  Done = 2
}

// Goal Framework DTOs (read-only for employees)
export interface GoalFrameworkTemplateDto {
  id: string;
  name: string;
  description?: string;
  version: number;
  categoryCount: number;
}

export interface GoalFrameworkCategoryDto {
  id: string;
  templateId: string;
  name: string;
  description?: string;
  itemCount: number;
}

export interface GoalFrameworkItemDto {
  id: string;
  name: string;
  description?: string;
  goalItemCount: number;
}

export interface SuggestedActivityDto {
  id: string;
  description: string;
}

export interface GoalFrameworkGoalItemDto {
  id: string;
  name: string;
  description?: string;
  targetScore: number;
  suggestedActivities: SuggestedActivityDto[];
}

// Personal Goal DTOs
export interface CreatePersonalGoalDto {
  goalItemId: string;
  goalSetId?: string; // Optional: groups goals created together
  title: string;
  description?: string;
  startDate: Date | string;
  dueDate: Date | string;
  selectedSuggestedActivityIds: string[];
  customActivities: string[];
}

export interface UpdatePersonalGoalDto {
  title: string;
  description?: string;
  startDate: Date | string;
  dueDate: Date | string;
  status: PersonalGoalStatus;
}

export interface UpdatePersonalGoalScoreDto {
  currentScore: number;
}

export interface PersonalGoalListDto {
  id: string;
  goalSetId?: string; // Optional: groups goals together
  title: string;
  categoryName: string;
  itemName: string;
  goalItemName: string;
  targetScore: number;
  currentScore: number;
  status: PersonalGoalStatus;
  dueDate: Date | string;
  createdAt: Date | string;
}

export interface PersonalGoalActivityDto {
  id: string;
  personalGoalId: string;
  suggestedActivityId?: string;
  description: string;
  isFromTemplate: boolean;
  status: ActivityStatus;
  dueDate?: Date | string;
  evidenceUrl?: string;
  evidenceNotes?: string;
  createdAt: Date | string;
  updatedAt?: Date | string;
}

export interface PersonalGoalDetailDto {
  id: string;
  userId: number;
  goalItemId: string;
  title: string;
  description?: string;
  targetScore: number;
  currentScore: number;
  startDate: Date | string;
  dueDate: Date | string;
  status: PersonalGoalStatus;
  createdAt: Date | string;
  updatedAt?: Date | string;
  categoryName: string;
  itemName: string;
  goalItemName: string;
  goalItemDescription?: string;
  activities: PersonalGoalActivityDto[];
}

export interface CreatePersonalGoalActivityDto {
  suggestedActivityId?: string;
  description: string;
  dueDate?: Date | string;
}

export interface UpdatePersonalGoalActivityDto {
  description: string;
  status: ActivityStatus;
  dueDate?: Date | string;
  evidenceUrl?: string;
  evidenceNotes?: string;
}

// Helper interfaces for UI
export interface GoalItemSelection {
  goalItem: GoalFrameworkGoalItemDto;
  title: string;
  description: string;
  startDate: Date | string;  // Can be Date object or string from date input
  dueDate: Date | string;    // Can be Date object or string from date input
  selectedSuggestedActivityIds: string[];
  customActivities: string[];
}

// Goal Set DTO (grouped view)
export interface PersonalGoalSetDto {
  goalSetId: string;
  templateName: string;
  goalCount: number;
  totalTargetScore: number;
  totalCurrentScore: number;
  startDate: Date | string;
  dueDate: Date | string;
  status: PersonalGoalStatus;
  createdAt: Date | string;
  goals: PersonalGoalListDto[];
  categories: string[];
}
