# CLAUDE.md

## Purpose
This file is a code-verified project guide for contributors and AI agents working in this repository.

It explains:
- what the system is,
- how the repository is structured,
- how services start and communicate,
- how authentication/authorization works,
- how evaluation workflows move through statuses,
- where implementation and legacy documentation differ.

## Project Summary
EPECPS (Employee Performance Evaluation and Career Progression System) is a full-stack performance management platform with:
- a .NET 8 backend API,
- an Angular 20 frontend,
- SQL Server persistence,
- Docker-based local orchestration,
- role-based workflow for goals, reviews, approvals, and promotion/PIP routing.

## Top-Level Repository Structure

- `backend/`
  - `Epecps.Domain/`: domain entities and enums
  - `Epecps.Application/`: DTOs, interfaces, validators, app contracts
  - `Epecps.Infrastructure/`: EF Core persistence, services, migrations, seeding
  - `Epecps.Api/`: HTTP API entrypoint and controllers
  - `Epecps.Tests/`: xUnit + Moq + EF InMemory tests
- `frontend/epecps-web/`
  - Angular app, auth guards/interceptor, feature modules, services
- `scripts/`
  - `docker.sh`: main local Docker workflow script
  - `sql/`: idempotent SQL helpers (workflow-v2 schema alignment)
- `docs/`
  - architecture, workflow, API reference, run guides, test data credentials
- `docker-compose.yml`
  - db + backend + frontend stack for local development

## Architecture

### Backend (layered architecture)
- Domain layer:
  - core entities such as User, Evaluation, Review, PersonalGoal, PromotionCase, PipCase.
- Application layer:
  - contracts/interfaces for services and DTO boundaries.
- Infrastructure layer:
  - EF Core DbContext (`EpecpsDbContext`), service implementations, import/report/email logic.
- API layer:
  - controllers expose HTTP endpoints and enforce role-based access.

### Frontend (Angular)
- App module + feature modules (`employee`, `admin`).
- Auth stack:
  - `AuthService` with JWT access/refresh tokens in localStorage.
  - `AuthInterceptor` adds bearer token and attempts refresh on 401.
  - `AuthGuard`/`RoleGuard` enforce route access.
- Service-driven API access through typed Angular services.

### Database
- SQL Server with EF Core migrations + runtime schema safety checks.
- Core tables include users/roles/departments/cycles/evaluations/reviews/goals.
- Workflow-v2 additions include review weights, manager mappings, HOD mappings, PIP tables.

## Runtime and Environment Model

### Docker stack (recommended dev path)
Services in `docker-compose.yml`:
- `db` (SQL Server, exposed on 1433)
- `backend` (ASP.NET API, exposed on 8080)
- `frontend` (Nginx serving Angular, exposed on 4200)

### Docker startup flow (`./scripts/docker.sh up`)
1. Validates Docker + compose availability.
2. Ensures `.env` exists (copies `.env.docker.example` if needed).
3. Starts DB first.
4. Ensures `EpecpsDb` exists.
5. Starts backend + frontend.
6. Optionally imports Excel test data when enabled and not already present.

### Backend startup behavior
In `Program.cs`, startup can:
- run migrations with retries,
- fallback to idempotent schema sync SQL,
- optionally ensure core tables and recreate partial schema in relaxed modes,
- seed baseline data,
- ensure a SuperAdmin account exists.

### Frontend environment behavior
- Local Angular env: `environment.ts` -> `apiUrl: https://localhost:7275`.
- Docker build env: `environment.docker.ts` -> `apiUrl: window.location.origin`.
- In Docker, Nginx proxies `/api` and `/swagger` to backend service.

## Authentication and Authorization

### Implemented authentication model (current code)
JWT-based auth with API endpoints under `/api/v1/auth`:
- login
- setup-password
- register
- refresh
- logout
- me

### Token/session model
- Access token + refresh token persisted in localStorage.
- Interceptor attaches bearer token for API calls.
- On 401 (non-auth endpoints), frontend attempts refresh and retries.

### Role model
Common roles used by policies and guards:
- Employee
- RM
- TL
- Peer
- HOD
- GM
- HR
- Admin
- SuperAdmin

## API Surface Overview

### Core functional areas
- Auth: `/api/v1/auth/*`
- Dashboard: `/api/v1/dashboard/*`
- Employee goals: `/api/employee-goals/*`
- Evaluations + approvals: `/api/evaluations/*`
- Workflow-v2 routes: `/api/v2/workflow/*`
- Reports and downloads: `/api/reports/*`
- Score template admin: `/api/v1/admin/templates/*`
- Excel admin import: `/api/v1/admin/import/*`
- Database admin/debug routes: `/api/admin/database/*`

## End-to-End Workflow Flows

This section reflects current implementation and is aligned with the flow diagram in `docs/EmpoVate360 EhancedFlow.drawio (1).pdf`.

## 1) RM-first kickoff (goal assignment)
1. RM selects employee and assigns at least 5 goals.
2. System creates `PersonalGoals`, `GoalAssignments`, and `EmployeeGoals` records.
3. System creates evaluation with:
  - `WorkflowVersion = v2`
  - `Status = V2_PENDING_EMPLOYEE_ACTIVATION`
4. Employee receives goal-assignment notification.

## 2) Employee activation + RM activation review
1. Employee submits activation method for every assigned goal.
2. Status moves to `V2_PENDING_RM_ACTIVATION_REVIEW`.
3. RM reviews activation plan:
  - approve -> `V2_ACTIVE_GOALS`
  - return -> `V2_RETURNED_FOR_ACTIVATION` (employee resubmits corrections)

Legacy compatibility:
- Existing in-flight records with `V2_PENDING_TL_ACTIVATION_REVIEW` are still accepted and can be processed by RM.

## 3) Employee completes all assigned goals
1. Employee starts and works goals during the cycle period.
2. Employee completes each goal and adds evidence/summary/comments.
3. When all goals in the goal set are completed, workflow continuation is triggered.
4. Employee self-evaluation submission path also leads into the same RM post-completion stage.

## 4) Automatic RM post-completion stage (required behavior)
This is the core rule for the RM-first process.

After all goals are completed, evaluation automatically moves to:
- `Pending_RM_Review_PostCompletion`

What this stage does:
- Creates/ensures a pending RM review task.
- Notifies RM for evaluation.
- RM reviews each goal individually and submits per-goal scores.

Implementation paths:
- `EvaluationWorkflowService.ContinueWorkflowAfterEmployeeCompletionAsync` transitions to RM post-completion automatically when all goals are complete.
- `WorkflowV2Service.SubmitSelfEvaluationAsync` also sets `Pending_RM_Review_PostCompletion`.

## 5) RM approval auto-opens TL evaluation
1. RM submits all required per-goal scores.
2. RM approves post-completion review.
3. System transitions to `Pending_TL_Review`.
4. TL review task is created automatically.

## 6) TL overall score + assign two peer reviewers
1. TL submits overall score/comment for the evaluation.
2. TL assigns exactly two peer reviewers in the same action.
3. Status moves to `Pending_Peer_Reviews`.

## 7) Peer reviews then handoff to remaining evaluators
1. Peer 1 submits review.
2. Peer 2 submits review.
3. After both peer reviews are completed/approved, request is passed forward:
  - runtime hybrid path -> `Pending_HOD_Review`
  - v2 parallel target -> `V2_PENDING_HOD_REVIEW`

## 8) HOD routing and promotion/PIP
### Legacy/hybrid routing (`EvaluationWorkflowService`)
- HOD stage computes overall score.
- Current threshold constant: 85.
- If score >= 85 -> `Pending_GM_Decision`
- If score < 85 -> `Pending_HR_Processing`

### Workflow-v2 routing (`WorkflowV2Service`)
- HOD finalization on `V2_PENDING_HOD_REVIEW`:
  - score >= 85 -> `V2_PENDING_GM_DECISION`
  - score < 85 -> `V2_PENDING_HR_LOW_PERFORMER` and create/update PIP case
- GM decision on `V2_PENDING_GM_DECISION`:
  - approve + vacancy -> `V2_PENDING_HR_PROMOTION`
  - approve + no vacancy -> `V2_PROMOTION_DEFERRED`
  - reject -> `V2_COMPLETED_NO_PROMOTION`
- HR closes promotion or low-performer path to completion statuses.

## 9) Review weighting model (workflow-v2)
`WorkflowV2Service` stores configurable weights for:
- Self
- TL
- RM
- Peer1
- Peer2

Defaults are 20% each (sum must equal 100).
Final weighted score is rounded and used for HOD threshold routing.

## 10) PIP workflow
When routed to low-performer path:
- system creates or updates a `PipCase`,
- assigns HR owner,
- supports CRUD for action items,
- exposes queue endpoints for HR operations.

## 11) Status progression snapshot (current runtime)
`V2_PENDING_EMPLOYEE_ACTIVATION`
-> `V2_PENDING_RM_ACTIVATION_REVIEW`
-> `V2_ACTIVE_GOALS`
-> `Pending_RM_Review_PostCompletion` (automatic post-completion RM stage)
-> `Pending_TL_Review`
-> `Pending_Peer_Reviews`
-> `Pending_HOD_Review` (or `V2_PENDING_HOD_REVIEW` in full v2 routing)
-> GM/HR decision stages
-> completed status

## Data Bootstrap and Import Flows

### Baseline seed
`DatabaseSeeder` ensures:
- default department (`General`),
- system roles,
- current-year active cycle.

### SuperAdmin bootstrap
`SuperAdminBootstrapService` ensures configured super admin exists and has role.

### Excel import
`UserProjectImportService` supports:
- template generation,
- bulk user/project/assignment import,
- user-role replacement semantics per employee,
- manager mapping updates,
- validation/error reporting.

## Frontend Route Map (functional)
- `/login`, `/register`, `/setup-password`
- `/dashboard`
- `/admin/dashboard`
- `/admin/templates`
- `/employee/goals`
- `/employee/rm-assign-goals`
- `/employee/hr-reports`
- `/employee/hr-pip-cases`
- `/evaluations/my-approvals`
- `/evaluations/:id`

## Important Files to Know First
- Backend entrypoint: `backend/Epecps.Api/Program.cs`
- Compose stack: `docker-compose.yml`
- Dev orchestration script: `scripts/docker.sh`
- Workflow v2 service: `backend/Epecps.Infrastructure/Services/WorkflowV2Service.cs`
- Main evaluation workflow service: `backend/Epecps.Infrastructure/Services/EvaluationWorkflowService.cs`
- Review scoring service: `backend/Epecps.Infrastructure/Services/ReviewScoringService.cs`
- Frontend auth service: `frontend/epecps-web/src/app/core/auth/auth.service.ts`
- Frontend route config: `frontend/epecps-web/src/app/app-routing-module.ts`
- Employee routes: `frontend/epecps-web/src/app/employee/employee-routing.module.ts`

## Build and Run Quick Commands

### Docker
- `./scripts/docker.sh up`
- `./scripts/docker.sh up-fresh`
- `./scripts/docker.sh seed-test-data`
- `./scripts/docker.sh logs`
- `./scripts/docker.sh down`
- `./scripts/docker.sh reset`

### Backend local
- `cd backend/Epecps.Api`
- `dotnet run`

### Frontend local
- `cd frontend/epecps-web`
- `npm install`
- `npm start`

## Testing Snapshot
- Test project: `backend/Epecps.Tests`
- Framework: xUnit
- Support libs: Moq, EFCore.InMemory
- Significant workflow test coverage exists in `EvaluationWorkflowTests.cs`.
- `UnitTest1.cs` is a minimal placeholder.

## Known Alignment Notes
1. Authentication docs vs code:
   - Some docs still describe Azure AD/MSAL-based runtime.
   - Implemented runtime currently uses local JWT auth endpoints.
2. Workflow-v2 docs vs current transition path:
   - Docs describe direct transition into `V2_PENDING_PARALLEL_REVIEWS` after self-evaluation.
   - Current code transitions to `Pending_RM_Review_PostCompletion`, then legacy statuses continue.
3. Workflow threshold in diagram/docs vs code:
  - Flow diagram references cutoff logic around 80 in some places.
  - Current runtime code enforces 85 as the HOD routing threshold.
4. Frontend local dev port:
   - Angular serve default is configured as 64291, while some docs reference 4200.

## Maintenance Guidance
When changing workflows, keep these in sync together:
- API controller contracts,
- service-layer status transitions,
- frontend route/service assumptions,
- docs in `docs/` and this file,
- test cases under `backend/Epecps.Tests`.

If status names or thresholds change, update:
- constants,
- routing logic,
- dashboard queries,
- pending-approval filters,
- docs and test fixtures.
