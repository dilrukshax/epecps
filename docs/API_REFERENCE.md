# API Reference

## Base Configuration

**Base URL**: `https://localhost:7275`

**Authentication**: Bearer token (Azure AD)

**Content-Type**: `application/json`

**Authorization Header**:
```
Authorization: Bearer {access_token}
```

## API Endpoints

### Authentication

All endpoints require valid Azure AD authentication token with appropriate scopes.

**Required Scope**: `api://{client-id}/Epecps.ReadWrite`

## Workflow V2 (EmpoVate360-Aligned)

The endpoints below are the preferred integration path for the RM-first + activation + parallel review flow.

### RM Assignment
```
POST /api/rm/goals/assign
```
Assigns 5+ goals and creates a workflow-v2 evaluation.

### Employee Activation Submission
```
POST /api/v2/workflow/goal-sets/{goalSetId}/activation
```

### RM Activation Decision
```
POST /api/v2/workflow/evaluations/{evaluationId}/activation/decision
```

Notes:
- Current primary status before this decision: `V2_PENDING_RM_ACTIVATION_REVIEW`.
- Legacy in-flight status `V2_PENDING_TL_ACTIVATION_REVIEW` is still accepted for compatibility.
- Authorized roles: `RM`, `SuperAdmin`.

### TL Peer Assignment (2 peers)
```
POST /api/evaluations/{evaluationId}/assign-peers
```

### Employee Self-Evaluation (Per Goal)
```
POST /api/v2/workflow/evaluations/{evaluationId}/self-evaluation
```

Request body:
```json
{
  "overallComment": "Overall self-evaluation summary",
  "goals": [
    {
      "personalGoalId": "guid",
      "score": 82,
      "summary": "Delivered target features and reduced defects.",
      "evidenceUrl": "https://example.com/evidence",
      "comment": "Optional per-goal comment"
    }
  ]
}
```

Rules:
- All assigned goals must be included.
- Score range is 0-100.
- `summary` and `evidenceUrl` are required per goal.
- TL must have assigned exactly 2 peers before submission.
- Submission is allowed on/after the evaluation cycle end date.

### HOD Finalization (Threshold Routing)
```
POST /api/v2/workflow/evaluations/{evaluationId}/hod/finalize
```

### GM Decision (Vacancy Aware)
```
POST /api/v2/workflow/evaluations/{evaluationId}/gm/decision
```

### HR PIP Queue
```
GET /api/v2/workflow/pip-cases
```

### Evaluations API

#### Get All Evaluations
```
GET /api/evaluations
```

**Authorization**: Any authenticated user

**Response**: 200 OK
```json
[
  {
    "evaluationId": 1,
    "employeeId": 123,
    "employeeName": "John Doe",
    "cycleId": 1,
    "cycleName": "Annual Review 2024",
    "status": "AwaitingTLReview",
    "overallScore": 85.5,
    "teamLeadName": "Jane Smith",
    "reportingManagerName": "Bob Johnson"
  }
]
```

#### Get Evaluation Details
```
GET /api/evaluations/{id}
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: Employee, assigned reviewers, HOD (department), HR, Admin

**Response**: 200 OK
```json
{
  "evaluationId": 1,
  "employeeId": 123,
  "employeeName": "John Doe",
  "cycleId": 1,
  "cycleName": "Annual Review 2024",
  "status": "AwaitingTLReview",
  "overallScore": 85.5,
  "goalSetId": "guid-here",
  "reviews": [...],
  "goals": [...],
  "approvalHistory": [...]
}
```

#### Create Evaluation
```
POST /api/evaluations
```

**Authorization**: HR, Admin

**Request Body**:
```json
{
  "employeeId": 123,
  "cycleId": 1,
  "teamLeadId": 456,
  "reportingManagerId": 789,
  "goalSetId": "optional-guid"
}
```

**Response**: 201 Created
```json
{
  "evaluationId": 1,
  "status": "Draft"
}
```

#### Submit Self Review
```
POST /api/evaluations/{id}/submit-self-review
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: Employee (self)

**Request Body**:
```json
{
  "overallComment": "My self-assessment comments",
  "reviewItems": [
    {
      "goalId": 1,
      "ratingValue": 85,
      "comment": "Goal achieved with good results"
    }
  ]
}
```

**Response**: 200 OK

#### Complete Team Lead Review
```
POST /api/evaluations/{id}/tl-complete-review
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: Assigned Team Lead

**Request Body**:
```json
{
  "overallComment": "Performance review comments",
  "reviewItems": [
    {
      "goalId": 1,
      "ratingValue": 88,
      "comment": "Strong performance"
    }
  ],
  "recommendForPromotion": true
}
```

**Response**: 200 OK

#### RM Approve Evaluation
```
POST /api/evaluations/{id}/rm-approve
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: Assigned Reporting Manager

**Request Body**:
```json
{
  "approved": true,
  "comment": "Approval comments"
}
```

**Response**: 200 OK

#### HOD Approve Evaluation
```
POST /api/evaluations/{id}/hod-approve
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: HOD (same department)

**Request Body**:
```json
{
  "approved": true,
  "comment": "Department head approval",
  "recommendForPromotion": false
}
```

**Response**: 200 OK

#### GM Final Approval
```
POST /api/evaluations/{id}/gm-approve
```

**Parameters:**
- `id` (path, required): Evaluation ID

**Authorization**: GM

**Request Body**:
```json
{
  "approved": true,
  "comment": "Final approval comments",
  "promotionDecision": "Approved",
  "promotionReason": "Exceptional performance"
}
```

**Response**: 200 OK

### Personal Goals API

#### Get My Personal Goals
```
GET /api/personal-goals
```

**Query Parameters:**
- `status` (optional): Filter by status (NotStarted, InProgress, Completed, Cancelled)
- `goalSetId` (optional): Filter by goal set ID

**Authorization**: Employee (own goals), TL, RM, HR, Admin

**Response**: 200 OK
```json
[
  {
    "id": "guid",
    "title": "Improve coding skills",
    "description": "Learn advanced patterns",
    "status": "InProgress",
    "startDate": "2024-01-01",
    "dueDate": "2024-12-31",
    "targetScore": 100,
    "currentScore": 65,
    "activities": [...]
  }
]
```

#### Get Personal Goal Details
```
GET /api/personal-goals/{id}
```

**Parameters:**
- `id` (path, required): Goal ID (GUID)

**Authorization**: Employee (own goal), TL, RM, HR, Admin

**Response**: 200 OK

#### Create Personal Goal
```
POST /api/personal-goals
```

**Authorization**: Employee

**Request Body**:
```json
{
  "goalItemId": "score-item-guid",
  "title": "Improve coding skills",
  "description": "Learn advanced patterns",
  "startDate": "2024-01-01",
  "dueDate": "2024-12-31",
  "targetScore": 100,
  "activities": [
    {
      "description": "Complete online course",
      "dueDate": "2024-06-30"
    }
  ]
}
```

**Response**: 201 Created

#### Update Personal Goal
```
PUT /api/personal-goals/{id}
```

**Parameters:**
- `id` (path, required): Goal ID (GUID)

**Authorization**: Employee (own goal)

**Request Body**:
```json
{
  "description": "Updated description",
  "currentScore": 75,
  "activities": [...]
}
```

**Response**: 200 OK

#### Start Personal Goal
```
POST /api/personal-goals/{id}/start
```

**Parameters:**
- `id` (path, required): Goal ID (GUID)

**Authorization**: Employee (own goal)

**Response**: 200 OK

#### Complete Personal Goal
```
POST /api/personal-goals/{id}/complete
```

**Parameters:**
- `id` (path, required): Goal ID (GUID)

**Authorization**: Employee (own goal)

**Request Body**:
```json
{
  "finalScore": 90,
  "completionNotes": "Successfully completed all activities"
}
```

**Response**: 200 OK

### Dashboard API

#### Get Dashboard Statistics
```
GET /api/dashboard/stats
```

**Authorization**: Any authenticated user

**Response**: 200 OK
```json
{
  "totalEvaluations": 150,
  "completedEvaluations": 45,
  "pendingReviews": 23,
  "myPendingActions": 5,
  "teamPendingActions": 8,
  "averageScore": 82.5,
  "promotionRecommendations": 12,
  "goalsInProgress": 87,
  "goalsCompleted": 34
}
```

### Reports API

#### Get Evaluation Report Data
```
GET /api/reports/evaluations
```

**Query Parameters:**
- `cycleId` (optional): Filter by cycle
- `departmentId` (optional): Filter by department
- `status` (optional): Filter by status
- `startDate` (optional): Filter by date range start
- `endDate` (optional): Filter by date range end

**Authorization**: HR, Admin, HOD (department), GM

**Response**: 200 OK
```json
[
  {
    "evaluationId": 1,
    "employeeName": "John Doe",
    "department": "Engineering",
    "status": "Completed",
    "overallScore": 85.5,
    "completedDate": "2024-03-15",
    "promotionRecommended": true
  }
]
```

#### Export Evaluation Report
```
POST /api/reports/evaluations/export
```

**Authorization**: HR, Admin, HOD (department), GM

**Request Body**:
```json
{
  "cycleId": 1,
  "departmentId": 5,
  "status": "Completed",
  "format": "Excel"
}
```

**Response**: 200 OK (Excel file download)
```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename=EvaluationReport_2024.xlsx
```

### Score Templates API

#### Get All Templates
```
GET /api/score-templates
```

**Query Parameters:**
- `includeArchived` (optional): Include archived templates

**Authorization**: Any authenticated user

**Response**: 200 OK
```json
[
  {
    "id": "guid",
    "name": "Software Engineer Template",
    "version": 1,
    "isPublished": true,
    "isArchived": false,
    "categories": [...]
  }
]
```

#### Get Template Details
```
GET /api/score-templates/{id}
```

**Parameters:**
- `id` (path, required): Template ID (GUID)

**Authorization**: Any authenticated user

**Response**: 200 OK

#### Create Template
```
POST /api/score-templates
```

**Authorization**: HR, Admin

**Request Body**:
```json
{
  "name": "New Template",
  "description": "Template description",
  "categories": [
    {
      "name": "Technical Skills",
      "weightPercent": 60,
      "items": [
        {
          "name": "Coding Quality",
          "maxScore": 100,
          "isMandatory": true
        }
      ]
    }
  ]
}
```

**Response**: 201 Created

#### Publish Template
```
POST /api/score-templates/{id}/publish
```

**Parameters:**
- `id` (path, required): Template ID (GUID)

**Authorization**: HR, Admin

**Response**: 200 OK

### Users API

#### Get Current User
```
GET /api/users/me
```

**Authorization**: Any authenticated user

**Response**: 200 OK
```json
{
  "userId": 123,
  "fullName": "John Doe",
  "email": "john.doe@company.com",
  "department": "Engineering",
  "roles": ["Employee", "TL"]
}
```

#### Sync Users from Azure AD
```
POST /api/users/sync
```

**Authorization**: HR, Admin

**Response**: 200 OK
```json
{
  "syncedUsers": 45,
  "newUsers": 5,
  "updatedUsers": 40
}
```

### Cycles API

#### Get All Cycles
```
GET /api/cycles
```

**Authorization**: Any authenticated user

**Response**: 200 OK
```json
[
  {
    "cycleId": 1,
    "name": "Annual Review 2024",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "status": "Active"
  }
]
```

#### Create Cycle
```
POST /api/cycles
```

**Authorization**: HR, Admin

**Request Body**:
```json
{
  "name": "Q1 Review 2024",
  "startDate": "2024-01-01",
  "endDate": "2024-03-31"
}
```

**Response**: 201 Created

#### Activate Cycle
```
POST /api/cycles/{id}/activate
```

**Parameters:**
- `id` (path, required): Cycle ID

**Authorization**: HR, Admin

**Response**: 200 OK

## Error Responses

### Standard Error Format
```json
{
  "error": "Error message",
  "details": "Detailed error information",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### HTTP Status Codes

**200 OK**: Successful request
**201 Created**: Resource created successfully
**400 Bad Request**: Invalid request data
**401 Unauthorized**: Missing or invalid authentication token
**403 Forbidden**: Insufficient permissions
**404 Not Found**: Resource not found
**409 Conflict**: Data conflict (e.g., duplicate)
**500 Internal Server Error**: Server error

### Common Errors

#### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "details": "Valid authentication token required"
}
```

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "details": "You do not have permission to perform this action"
}
```

#### 400 Bad Request
```json
{
  "error": "Validation failed",
  "details": {
    "employeeId": ["Employee ID is required"],
    "cycleId": ["Cycle must be active"]
  }
}
```

#### 404 Not Found
```json
{
  "error": "Not Found",
  "details": "Evaluation with ID 999 not found"
}
```

## Rate Limiting

Currently no rate limiting implemented. Consider implementing in production:
- 100 requests per minute per user
- 1000 requests per hour per user

## Versioning

Current API version: v1

API versioning not yet implemented. Future versions may use URL versioning:
```
/api/v2/evaluations
```

## Pagination

For endpoints returning large datasets, pagination parameters:
```
GET /api/evaluations?page=1&pageSize=20
```

Response includes pagination metadata:
```json
{
  "data": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8
}
```

## Filtering & Sorting

Common query parameters:
- `sortBy`: Field to sort by
- `sortOrder`: asc or desc
- `filter`: Filter expression

Example:
```
GET /api/evaluations?sortBy=employeeName&sortOrder=asc&filter=status:Completed
```
