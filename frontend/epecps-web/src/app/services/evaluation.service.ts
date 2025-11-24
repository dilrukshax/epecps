import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  SubmitGoalSetResponseDto,
  PendingApprovalDto,
  EvaluationDetailDto,
  ApprovalActionDto,
  AssignPeersDto,
  PromotionDecisionDto
} from '../models/evaluation.models';

@Injectable({
  providedIn: 'root'
})
export class EvaluationService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /**
   * Submit a goal set for evaluation
   */
  submitGoalSetForEvaluation(goalSetId: string): Observable<SubmitGoalSetResponseDto> {
    return this.http.post<SubmitGoalSetResponseDto>(
      `${this.apiUrl}/api/employee-goals/sets/${goalSetId}/submit-for-evaluation`,
      {}
    );
  }

  /**
   * Get all pending approvals for the current user
   */
  getPendingApprovals(): Observable<PendingApprovalDto[]> {
    return this.http.get<PendingApprovalDto[]>(
      `${this.apiUrl}/api/evaluations/pending-approvals`
    );
  }

  /**
   * Get detailed evaluation information
   */
  getEvaluationDetail(evaluationId: number): Observable<EvaluationDetailDto> {
    return this.http.get<EvaluationDetailDto>(
      `${this.apiUrl}/api/evaluations/${evaluationId}`
    );
  }

  /**
   * Approve an evaluation at the current stage
   */
  approveEvaluation(evaluationId: number, comment?: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/approve`,
      body
    );
  }

  /**
   * Reject an evaluation and return it to the employee
   */
  rejectEvaluation(evaluationId: number, comment: string): Observable<any> {
    const body: ApprovalActionDto = { comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/reject`,
      body
    );
  }

  /**
   * Assign peer reviewers (Team Lead only)
   */
  assignPeerReviewers(evaluationId: number, peerUserId1: number, peerUserId2: number): Observable<any> {
    const body: AssignPeersDto = { peerUserId1, peerUserId2 };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/assign-peers`,
      body
    );
  }

  /**
   * Process promotion decision (GM only)
   */
  processPromotionDecision(evaluationId: number, approve: boolean, comment?: string): Observable<any> {
    const body: PromotionDecisionDto = { approve, comment };
    return this.http.post(
      `${this.apiUrl}/api/evaluations/${evaluationId}/promotion-decision`,
      body
    );
  }
}
