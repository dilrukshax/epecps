# Evaluation Workflow

## Overview
The evaluation workflow manages the complete employee performance review process from goal setting through final approval. The workflow involves multiple stakeholders and follows a structured approval chain.

## Workflow States

### Status Flow Diagram
```
Draft
  ?
AwaitingSelfReview
  ? (employee submits)
SelfReviewCompleted
  ?
AwaitingTLReview
  ? (team lead completes review)
TLReviewCompleted
  ?
AwaitingRMReview
  ? (reporting manager approves)
RMReviewCompleted
  ?
AwaitingHODApproval
  ? (HOD approves)
HODApproved
  ?
AwaitingGMApproval
  ? (GM approves)
Completed

Note: Can transition to Rejected at any approval stage
```

## Detailed Workflow Steps

### 1. Evaluation Creation (Draft)
**Actors**: HR, Admin

**Actions:**
- Create evaluation record
- Assign employee, team lead, reporting manager
- Set evaluation cycle
- Link to employee's goal set (if exists)

**Status**: Draft

**Data Created:**
- Evaluation record
- Initial approval history entry

### 2. Self Review (AwaitingSelfReview)
**Actors**: Employee

**Trigger**: Admin/HR initiates evaluation

**Actions:**
- Employee reviews their goals
- Employee provides self-assessment
- Employee adds evidence/comments
- Employee submits for TL review

**Status Transition**: AwaitingSelfReview ? SelfReviewCompleted

**Data Created:**
- Self review record (ReviewerRole: Self)
- Review items with self-ratings
- Approval history entry

**Notifications:**
- Email to employee: "Please complete your self-review"
- Email to TL after submission: "Self-review submitted by {employee}"

### 3. Team Lead Review (AwaitingTLReview)
**Actors**: Team Lead

**Trigger**: Employee completes self-review

**Actions:**
- TL reviews employee's self-assessment
- TL provides ratings for each goal/competency
- TL adds overall comments
- TL can recommend for promotion
- TL submits for RM review

**Status Transition**: AwaitingTLReview ? TLReviewCompleted

**Data Created:**
- Team lead review record (ReviewerRole: TeamLead)
- Review items with TL ratings
- Promotion case (if recommended)
- Approval history entry

**Scoring:**
- Calculate weighted average of goal scores
- Update evaluation overall score

**Notifications:**
- Email to TL: "Please review {employee}'s performance"
- Email to RM after submission: "TL review completed for {employee}"

### 4. Reporting Manager Review (AwaitingRMReview)
**Actors**: Reporting Manager

**Trigger**: Team Lead completes review

**Actions:**
- RM reviews TL assessment
- RM can adjust ratings (optional)
- RM adds management comments
- RM approves or requests changes
- RM submits for HOD approval

**Status Transition**: 
- Approve: AwaitingRMReview ? RMReviewCompleted
- Reject: AwaitingRMReview ? Rejected

**Data Created:**
- RM review record (ReviewerRole: ReportingManager)
- Review items (if adjustments made)
- Approval history entry

**Notifications:**
- Email to RM: "Please approve {employee}'s evaluation"
- Email to HOD after approval: "RM approved {employee}'s evaluation"
- Email to TL if rejected: "Evaluation rejected by RM - {reason}"

### 5. HOD Approval (AwaitingHODApproval)
**Actors**: Head of Department

**Trigger**: Reporting Manager approves

**Actions:**
- HOD reviews complete evaluation
- HOD can recommend for promotion
- HOD approves or rejects
- If promotion recommended, forward to GM

**Status Transition**:
- Approve: AwaitingHODApproval ? HODApproved
- Reject: AwaitingHODApproval ? Rejected

**Data Created:**
- Promotion case (if recommended)
- Approval history entry

**Notifications:**
- Email to HOD: "Please approve {employee}'s evaluation"
- Email to GM if approved: "HOD approved {employee}'s evaluation"
- Email to RM if rejected: "Evaluation rejected by HOD - {reason}"

### 6. GM Final Approval (AwaitingGMApproval)
**Actors**: General Manager

**Trigger**: HOD approves

**Actions:**
- GM reviews evaluation and promotion case
- GM makes final promotion decision
- GM provides final approval

**Status Transition**:
- Approve: AwaitingGMApproval ? Completed
- Reject: AwaitingGMApproval ? Rejected

**Data Created:**
- Promotion decision (Approved/Rejected)
- Approval history entry

**Notifications:**
- Email to GM: "Please make final approval for {employee}"
- Email to employee: "Your evaluation is complete"
- Email to all reviewers: "Evaluation completed for {employee}"
- Email to employee if promoted: "Congratulations on your promotion"

### 7. Completion (Completed)
**Status**: Completed

**Final Actions:**
- Lock evaluation (no further edits)
- Generate final reports
- Archive documents
- Update employee record

## Peer Review Process

### Peer Assignment
**Timing**: Parallel to TL review

**Actions:**
- TL assigns 2-3 peers
- Peers receive notification
- Peers submit reviews independently

**Data Created:**
- PeerAssignment records
- Peer review records (ReviewerRole: Peer)

**Integration:**
- Peer scores considered in final calculation
- Peer comments visible to TL and RM

## Rejection Workflow

### Rejection Points
- RM can reject after TL review
- HOD can reject after RM approval
- GM can reject at final stage

### Rejection Actions
**Data Created:**
- Approval history with rejection reason

**Status Transition**: Any ? Rejected

**Notifications:**
- Email to previous reviewer with rejection reason
- Email to employee (if final rejection)

**Recovery:**
- Previous reviewer can resubmit
- Status reverts to appropriate waiting state

## Goal Linking

### Linking Personal Goals to Evaluation
**Timing**: Before or during self-review

**Process:**
1. Employee creates goal set from template
2. Employee works on goals throughout cycle
3. During evaluation creation, goals linked via GoalSetId
4. Goals automatically imported to EmployeeGoals
5. Reviews assess goal achievement

**Data Flow:**
```
PersonalGoals (GoalSetId) ? Evaluation (GoalSetId) ? EmployeeGoals ? ReviewItems
```

## Scoring Calculation

### Review Item Scoring
Each review item has:
- Rating value (0-100)
- Weight percentage
- Comments

### Evaluation Overall Score
```
For each Review:
  Review Score = ? (ReviewItem.RatingValue × ReviewItem.Weight)

Evaluation Overall Score = Weighted average of all submitted reviews
  - Self review: 20% weight
  - Peer reviews: 15% weight each
  - TL review: 35% weight
  - RM review: 30% weight
```

### Score Storage
- Individual review scores stored in Review.OverallScore
- Final evaluation score in Evaluation.OverallScore
- Updated after each review submission

## Promotion Workflow

### Recommendation Process
1. TL recommends during review (optional)
2. HOD reviews and forwards to GM
3. GM makes final decision

### Promotion Data
**PromotionCase fields:**
- RecommendedByHodId
- RecommendedAt
- GmDecision (Pending/Approved/Rejected)
- GmDecidedById
- GmDecidedAt
- DecisionReason

### Decision Flow
```
TL Review ? Recommendation flag set
  ?
HOD Approval ? Creates PromotionCase
  ?
GM Review ? GmDecision = Approved/Rejected
  ?
Employee notified of decision
```

## Training Recommendations

### Automatic Recommendations
Based on:
- Low scores in specific areas
- Competency gaps
- Previous evaluation comparisons

### Manual Recommendations
- TL/RM can add specific training materials
- Linked to TrainingMaterials table
- Reason for recommendation captured

**Data Structure:**
```
TrainingRecommendation:
  - EvaluationId
  - TrainingMaterialId
  - Reason
```

## Document Management

### Document Types
- PerformanceEvidence
- AchievementProof
- SupportingDoc

### Upload Process
1. Employee uploads during self-review
2. File stored with checksum
3. URI stored in Documents table
4. Linked to evaluation

### Document Access
- Employee: own documents
- TL/RM: documents for assigned evaluations
- HOD/GM: department documents
- HR/Admin: all documents

## Approval History Tracking

### History Entry Components
- Timestamp
- Actor (user who performed action)
- Actor role
- Action performed
- From status
- To status
- Comment (optional)

### Tracked Actions
- Evaluation created
- Self-review submitted
- TL review completed
- RM approved/rejected
- HOD approved/rejected
- GM approved/rejected
- Promotion recommended
- Promotion decision made

### History Query
Available through:
- Evaluation detail view
- Audit reports
- Admin dashboard

## Notifications

### Email Triggers
- Evaluation assigned
- Review due reminder
- Review submitted
- Approval required
- Approval completed
- Rejection occurred
- Promotion decision made
- Evaluation completed

### Email Content
- Personalized recipient name
- Action required
- Direct link to evaluation
- Deadline (if applicable)
- Actor who triggered

### Email Configuration
Configured in appsettings.json:
- SMTP server
- Sender details
- Retry policy
- Background processing enabled

## Business Rules

### Validation Rules
- Only assigned employee can submit self-review
- Only assigned TL can complete TL review
- Only assigned RM can approve/reject
- HOD must be in same department
- GM must have GM role
- Cannot skip workflow steps
- Cannot edit after completion

### Permission Rules
- View: Employee, TL, RM, HOD (department), GM, HR, Admin
- Edit self-review: Employee only
- Edit TL review: Assigned TL only
- Approve: RM, HOD, GM (based on status)
- Reject: RM, HOD, GM
- Delete: Admin only (draft status)

### Data Integrity Rules
- Evaluation must have active cycle
- Employee must be active
- TL and RM must be different from employee
- Goal weights must sum to 100%
- Scores must be 0-100 range
- Cannot delete evaluation with reviews

## Error Handling

### Common Errors
- Invalid status transition
- Unauthorized action
- Missing required data
- Concurrent modification
- Database constraint violation

### Error Response
- HTTP status code
- Error message
- Validation errors (if applicable)
- Timestamp

### Recovery Actions
- Retry with correct data
- Contact administrator
- Check permissions
- Reload evaluation data
