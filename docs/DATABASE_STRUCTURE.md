# Database Structure

## Overview
EPECPS uses SQL Server with Entity Framework Core for data persistence. The database follows a normalized relational structure with clear separation of concerns across multiple modules.

## Database Schema

### Core Entities

#### Users
Stores employee and user information.
```
Table: Users
- UserId (PK, int, Identity)
- FullName (nvarchar(200), required)
- Email (nvarchar(256), required, unique)
- Status (nvarchar(50), required)
- DeptId (FK to Departments, required)
```

**Indexes:**
- Unique index on Email
- Index on DeptId

**Relationships:**
- Belongs to one Department
- Has many UserRoles
- Has many PersonalGoals
- Referenced by Evaluations (as Employee, TeamLead, ReportingManager)

#### Roles
Defines system roles for authorization.
```
Table: Roles
- RoleId (PK, int, Identity)
- Name (nvarchar(50), required, unique)
```

**Available Roles:**
- Employee
- TL (Team Lead)
- RM (Reporting Manager)
- HOD (Head of Department)
- GM (General Manager)
- HR (Human Resources)
- Admin

#### UserRoles
Many-to-many relationship between Users and Roles.
```
Table: UserRoles
- UserId (PK, FK to Users)
- RoleId (PK, FK to Roles)
```

#### Departments
Hierarchical department structure.
```
Table: Departments
- DeptId (PK, int, Identity)
- Name (nvarchar(200), required)
- ParentDeptId (FK to Departments, nullable)
```

**Relationships:**
- Self-referencing (parent-child departments)
- Has many Users

#### Cycles
Performance evaluation cycles.
```
Table: Cycles
- CycleId (PK, int, Identity)
- Name (nvarchar(200), required)
- StartDate (datetime2, required)
- EndDate (datetime2, required)
- Status (nvarchar(50), required)
```

**Statuses:**
- Draft
- Active
- Closed

### Scoring Framework

#### ScoreTemplates
Defines evaluation scoring templates.
```
Table: ScoreTemplates
- Id (PK, uniqueidentifier)
- Name (nvarchar(200), required)
- Description (nvarchar(1000), nullable)
- Version (int, default 1)
- IsPublished (bit, default false)
- IsArchived (bit, default false)
- CreatedAt (datetime2, required)
- CreatedByUserId (int, required)
- UpdatedAt (datetime2, nullable)
- UpdatedByUserId (int, nullable)
```

**Indexes:**
- Index on Name
- Index on IsPublished
- Index on IsArchived

#### ScoreCategories
Categories within scoring templates.
```
Table: ScoreCategories
- Id (PK, uniqueidentifier)
- ScoreTemplateId (FK to ScoreTemplates, required)
- Name (nvarchar(200), required)
- Description (nvarchar(1000), nullable)
- WeightPercent (decimal(5,2), required)
- MaxScore (decimal(10,2), nullable)
- DisplayOrder (int, default 0)
- IsActive (bit, default true)
```

**Indexes:**
- Index on ScoreTemplateId
- Index on IsActive
- Composite index on (ScoreTemplateId, DisplayOrder)

**Relationships:**
- Belongs to one ScoreTemplate
- Has many ScoreItems

#### ScoreItems
Individual scoring criteria within categories.
```
Table: ScoreItems
- Id (PK, uniqueidentifier)
- ScoreCategoryId (FK to ScoreCategories, required)
- Name (nvarchar(200), required)
- Description (nvarchar(1000), nullable)
- ItemType (int, default 1) [Rating/Goal/Competency]
- MaxScore (decimal(10,2), required)
- TargetScore (decimal(10,2), default 100)
- WeightWithinCategory (decimal(10,2), nullable)
- IsMandatory (bit, default false)
- EvidenceRequired (bit, default false)
- EvidenceHint (nvarchar(500), nullable)
- DisplayOrder (int, default 0)
- IsActive (bit, default true)
```

**Indexes:**
- Index on ScoreCategoryId
- Index on IsActive
- Composite index on (ScoreCategoryId, DisplayOrder)

**Relationships:**
- Belongs to one ScoreCategory
- Has many PersonalGoals

### Personal Goals Module

#### PersonalGoals
Employee personal goals linked to score items.
```
Table: PersonalGoals
- Id (PK, uniqueidentifier)
- UserId (FK to Users, required)
- GoalItemId (FK to ScoreItems, required)
- GoalSetId (uniqueidentifier, nullable)
- Title (nvarchar(200), required)
- Description (nvarchar(2000), nullable)
- StartDate (datetime2, required)
- DueDate (datetime2, required)
- TargetScore (decimal(10,2), default 100)
- CurrentScore (decimal(10,2), default 0)
- Status (int, default 0) [NotStarted/InProgress/Completed/Cancelled]
- CreatedAt (datetime2, default GETUTCDATE())
- StartedAt (datetime2, nullable)
- CompletedAt (datetime2, nullable)
- UpdatedAt (datetime2, nullable)
```

**Indexes:**
- Index on UserId
- Index on GoalItemId
- Index on GoalSetId
- Index on Status
- Composite index on (UserId, DueDate)
- Composite index on (UserId, GoalSetId)
- Composite index on (UserId, Status)

**Relationships:**
- Belongs to one User
- Belongs to one ScoreItem
- Has many PersonalGoalActivities

#### PersonalGoalActivities
Activities and milestones for personal goals.
```
Table: PersonalGoalActivities
- Id (PK, uniqueidentifier)
- PersonalGoalId (FK to PersonalGoals, required)
- Description (nvarchar(1000), required)
- DueDate (datetime2, nullable)
- Status (int, default 0) [NotStarted/InProgress/Completed/Cancelled]
- EvidenceUrl (nvarchar(2000), nullable)
- EvidenceNotes (nvarchar(2000), nullable)
- IsFromTemplate (bit, default false)
- SuggestedActivityId (uniqueidentifier, nullable)
- CreatedAt (datetime2, default GETUTCDATE())
- UpdatedAt (datetime2, nullable)
```

**Indexes:**
- Index on PersonalGoalId
- Index on Status

**Relationships:**
- Belongs to one PersonalGoal

### Evaluation Module

#### Evaluations
Core evaluation records.
```
Table: Evaluations
- EvaluationId (PK, int, Identity)
- EmployeeId (FK to Users, required)
- CycleId (FK to Cycles, required)
- TeamLeadId (FK to Users, required)
- ReportingManagerId (FK to Users, required)
- GoalSetId (uniqueidentifier, nullable)
- Status (nvarchar(50), required)
- OverallScore (decimal(10,2), nullable)
- PreviousEvaluationId (FK to Evaluations, nullable)
```

**Statuses:**
- Draft
- AwaitingSelfReview
- SelfReviewCompleted
- AwaitingTLReview
- TLReviewCompleted
- AwaitingRMReview
- RMReviewCompleted
- AwaitingHODApproval
- HODApproved
- AwaitingGMApproval
- Completed
- Rejected

**Indexes:**
- Index on EmployeeId
- Index on CycleId
- Index on TeamLeadId
- Index on ReportingManagerId
- Index on GoalSetId
- Index on Status
- Index on PreviousEvaluationId

**Relationships:**
- Belongs to one Employee (User)
- Belongs to one Cycle
- Belongs to one TeamLead (User)
- Belongs to one ReportingManager (User)
- Has many Reviews
- Has many EmployeeGoals
- Has many PeerAssignments
- Has many Documents
- Has many PromotionCases
- Has many TrainingRecommendations
- Self-referencing (previous evaluation)

#### Reviews
Reviews submitted by different roles.
```
Table: Reviews
- ReviewId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- ReviewerUserId (FK to Users, required)
- ReviewerRole (int, required) [Self/Peer/TeamLead/ReportingManager]
- Status (nvarchar(50), required)
- OverallComment (nvarchar(2000), nullable)
- SubmittedAt (datetime2, nullable)
```

**Indexes:**
- Index on EvaluationId
- Index on ReviewerUserId
- Index on Status

**Relationships:**
- Belongs to one Evaluation
- Belongs to one Reviewer (User)
- Has many ReviewItems

#### ReviewItems
Individual ratings within a review.
```
Table: ReviewItems
- ItemId (PK, int, Identity)
- ReviewId (FK to Reviews, required)
- GoalId (FK to EmployeeGoals, nullable)
- CompetencyId (FK to Competencies, nullable)
- RatingValue (decimal(10,2), required)
- Comment (nvarchar(2000), nullable)
```

**Indexes:**
- Index on ReviewId
- Index on GoalId
- Index on CompetencyId

**Relationships:**
- Belongs to one Review
- Optionally references one EmployeeGoal
- Optionally references one Competency

#### EmployeeGoals
Goals set for evaluation.
```
Table: EmployeeGoals
- GoalId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- Title (nvarchar(200), required)
- Description (nvarchar(2000), required)
- WeightPct (decimal(5,2), required)
- EvidenceUri (nvarchar(2000), nullable)
```

**Indexes:**
- Index on EvaluationId

**Relationships:**
- Belongs to one Evaluation
- Has many ReviewItems

#### PeerAssignments
Peer reviewers assigned to evaluations.
```
Table: PeerAssignments
- PeerAssignmentId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- PeerUserId (FK to Users, required)
```

**Indexes:**
- Index on EvaluationId
- Index on PeerUserId
- Unique composite index on (EvaluationId, PeerUserId)

**Relationships:**
- Belongs to one Evaluation
- References one PeerUser (User)

#### Competencies
Skills and competencies to evaluate.
```
Table: Competencies
- CompetencyId (PK, int, Identity)
- Name (nvarchar(200), required)
- Description (nvarchar(1000), required)
- TargetLevel (nvarchar(100), required)
```

**Relationships:**
- Has many ReviewItems

#### PromotionCases
Promotion recommendations and decisions.
```
Table: PromotionCases
- PromotionCaseId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- RecommendedByHodId (FK to Users, nullable)
- RecommendedAt (datetime2, nullable)
- GmDecision (int, required) [Pending/Approved/Rejected]
- GmDecidedById (FK to Users, nullable)
- GmDecidedAt (datetime2, nullable)
- DecisionReason (nvarchar(2000), nullable)
```

**Indexes:**
- Index on EvaluationId
- Index on RecommendedByHodId
- Index on GmDecidedById
- Index on GmDecision

**Relationships:**
- Belongs to one Evaluation
- References one RecommendedByHod (User)
- References one GmDecidedBy (User)

#### TrainingRecommendations
Training recommendations based on evaluation.
```
Table: TrainingRecommendations
- TrainingRecId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- TrainingMaterialId (FK to TrainingMaterials, required)
- Reason (nvarchar(2000), nullable)
```

**Indexes:**
- Index on EvaluationId
- Index on TrainingMaterialId

**Relationships:**
- Belongs to one Evaluation
- References one TrainingMaterial

#### TrainingMaterials
Available training resources.
```
Table: TrainingMaterials
- TrainingMaterialId (PK, int, Identity)
- Title (nvarchar(500), required)
- Link (nvarchar(2000), required)
- Tags (nvarchar(1000), nullable)
```

**Relationships:**
- Has many TrainingRecommendations

#### Documents
Uploaded documents for evaluations.
```
Table: Documents
- DocumentId (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- Type (int, required) [PerformanceEvidence/AchievementProof/SupportingDoc]
- Uri (nvarchar(2000), required)
- Checksum (nvarchar(200), required)
- CreatedAt (datetime2, required)
```

**Indexes:**
- Index on EvaluationId

**Relationships:**
- Belongs to one Evaluation

### Audit & History

#### ApprovalHistories
Tracks approval workflow history.
```
Table: ApprovalHistories
- Id (PK, int, Identity)
- EvaluationId (FK to Evaluations, required)
- ReviewId (FK to Reviews, nullable)
- ActorUserId (FK to Users, required)
- ActorRole (nvarchar(50), required)
- Action (nvarchar(100), required)
- FromStatus (nvarchar(50), required)
- ToStatus (nvarchar(50), required)
- Comment (nvarchar(2000), nullable)
- CreatedAt (datetime2, required)
```

**Indexes:**
- Index on EvaluationId
- Index on ReviewId
- Index on ActorUserId
- Index on CreatedAt

**Relationships:**
- Belongs to one Evaluation
- Optionally references one Review
- References one ActorUser (User)

#### AuditLogs
System-wide audit logging.
```
Table: AuditLogs
- AuditId (PK, int, Identity)
- EntityType (nvarchar(100), required)
- EntityId (int, required)
- Action (nvarchar(50), required)
- ActorUserId (FK to Users, required)
- BeforeJson (nvarchar(max), nullable)
- AfterJson (nvarchar(max), nullable)
- CreatedAt (datetime2, required)
```

**Indexes:**
- Index on EntityType
- Index on ActorUserId
- Index on CreatedAt
- Composite index on (EntityType, EntityId)

**Relationships:**
- References one ActorUser (User)

#### Notifications
Email and system notifications.
```
Table: Notifications
- NotificationId (PK, int, Identity)
- UserId (FK to Users, required)
- Subject (nvarchar(500), required)
- Channel (nvarchar(50), required)
- SentAt (datetime2, required)
```

**Indexes:**
- Index on UserId
- Index on SentAt

**Relationships:**
- Belongs to one User

## Entity Relationships Diagram

```
Users ????? UserRoles ?? Roles
        ??? PersonalGoals ?? PersonalGoalActivities
        ??? Evaluations (as Employee)
        ??? Evaluations (as TeamLead)
        ??? Evaluations (as ReportingManager)
        ??? Reviews (as Reviewer)
        ??? PeerAssignments (as PeerUser)
        ??? PromotionCases (as RecommendedByHod)
        ??? PromotionCases (as GmDecidedBy)
        ??? ApprovalHistories (as ActorUser)
        ??? AuditLogs (as ActorUser)
        ??? Notifications

Departments ????? Users
              ??? Departments (sub-departments)

Cycles ?? Evaluations

ScoreTemplates ?? ScoreCategories ?? ScoreItems ?? PersonalGoals

Evaluations ????? Reviews ?? ReviewItems ????? EmployeeGoals
              ??? EmployeeGoals                ??? Competencies
              ??? PeerAssignments
              ??? Documents
              ??? PromotionCases
              ??? TrainingRecommendations ?? TrainingMaterials
              ??? ApprovalHistories
              ??? Evaluations (previous evaluation)
```

## Connection String Configuration

LocalDB (Development):
```
Server=(localdb)\\mssqllocaldb;Database=EpecpsDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

SQL Server Express:
```
Server=localhost\\SQLEXPRESS;Database=EpecpsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Azure SQL Database:
```
Server=tcp:server.database.windows.net,1433;Database=EpecpsDb;User Id=username;Password=password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Migration Commands

Create migration:
```bash
dotnet ef migrations add MigrationName -s Epecps.Api -p ../Epecps.Infrastructure
```

Apply migrations:
```bash
dotnet ef database update -s Epecps.Api -p ../Epecps.Infrastructure
```

Generate SQL script:
```bash
dotnet ef migrations script -s Epecps.Api -p ../Epecps.Infrastructure -o migration.sql
```
