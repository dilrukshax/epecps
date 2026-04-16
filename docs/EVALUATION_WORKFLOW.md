# Evaluation Workflow (EmpoVate360 Aligned)

## Overview
This document describes the workflow-v2 process aligned to EmpoVate360:

1. Admin setup and configuration
2. RM goal assignment + employee activation + RM activation approval
3. Employee evidence submission and self-evaluation
4. Parallel TL/RM/Peer reviews
5. HOD threshold routing (>= 80 to GM, < 80 to HR/PIP)
6. GM vacancy decision and HR final action

## Roles and Responsibilities
- Admin: departments, users, roles, RM mappings, goal pool/templates, evaluation periods, projects.
- RM: assigns at least 5 goals to each employee.
- Employee: submits activation methods, works on goals, submits per-goal evidence + self-ratings.
- RM: approves activation plans.
- TL: assigns exactly 2 peers.
- Peers (2): submit parallel peer review scores.
- HOD: finalizes department decision based on computed threshold.
- GM: promotion decision based on vacancy availability.
- HR: promotion processing and low-performer PIP handling.

## Phase 01: Setup and Configuration (Admin)
- Configure departments and HOD mappings.
- Configure users, roles, RM mappings.
- Configure goal framework (templates/categories/items).
- Configure evaluation cycle/period.
- Configure projects and assignments.
- Optional: import users/projects via Excel template.

## Phase 02: Goal Assignment and Activation
### Step 1: RM Assigns Goals
- RM selects employee.
- RM assigns at least 5 goals from active goal pool.
- System creates workflow-v2 evaluation in `V2_PENDING_EMPLOYEE_ACTIVATION`.

### Step 2: Employee Submits Activation Plan
- Employee provides activation method for each assigned goal.
- System validates all assigned goals are covered.
- Status moves to `V2_PENDING_RM_ACTIVATION_REVIEW`.

### Step 3: RM Activation Decision
- RM reviews activation methods.
- Approve: status -> `V2_ACTIVE_GOALS`.
- Return: status -> `V2_RETURNED_FOR_ACTIVATION`.
- Compatibility: in-flight records with `V2_PENDING_TL_ACTIVATION_REVIEW` are still accepted and routed through RM decision logic.

### Step 4: TL Assigns Peers (2)
- While status is `V2_ACTIVE_GOALS`, TL assigns exactly two peers.
- Employee, TL, RM cannot be selected as peer reviewers.

## Phase 03: Evidence Submission and Self-Evaluation (Employee)
- Self-evaluation opens at or after cycle end date.
- Employee submits self-evaluation per goal:
  - Score (0-100)
  - Evidence URL/reference (required)
  - Summary (required)
  - Optional comment
- Overall self score is computed as the average of submitted goal scores.
- Submission requires TL to have already assigned exactly two peers.

On submission:
- TL, RM, and both peer reviewers are put into pending review state.
- Status moves to `V2_PENDING_PARALLEL_REVIEWS`.
- Notifications are sent to TL, RM, Peer1, Peer2.

## Phase 04: Parallel Review and Routing
### Parallel Review Stage
In `V2_PENDING_PARALLEL_REVIEWS`, these are completed in parallel:
- TL review
- RM review
- Peer review 1
- Peer review 2
- Each reviewer submits scores for all assigned goals.

When all 4 are completed:
- System creates/ensures HOD review
- Status moves to `V2_PENDING_HOD_REVIEW`

### HOD Finalization
- HOD finalizes once parallel reviews are complete.
- System computes weighted final score from Self + TL + RM + the two TL-assigned peers.
- Threshold routing:
  - Final score >= 80: `V2_PENDING_GM_DECISION`
  - Final score < 80: `V2_PENDING_HR_LOW_PERFORMER` and PIP case is created/updated.

### GM Decision
For `V2_PENDING_GM_DECISION`:
- Approve + Vacancy Available -> `V2_PENDING_HR_PROMOTION`
- Approve + No Vacancy -> `V2_PROMOTION_DEFERRED`
- Reject -> `V2_COMPLETED_NO_PROMOTION`

### HR Final Actions
- Promotion queue: HR processes promotion to complete cycle.
- Low performer queue: HR manages PIP cases and action items.

## Rules
- Minimum 5 goals are required per assignment cycle.
- Employee must submit activation method for every assigned goal.
- TL must assign exactly 2 peers before self-evaluation submission.
- Self-evaluation requires per-goal evidence + summary + score.
- Self-evaluation is allowed only on/after cycle end date.
- HOD routing threshold (80) is system-enforced.
- GM decision requires vacancy context.
- Low-performer path automatically creates/updates PIP case.

## Status Lifecycle (Workflow v2)
1. `V2_PENDING_EMPLOYEE_ACTIVATION`
2. `V2_PENDING_RM_ACTIVATION_REVIEW`
3. `V2_ACTIVE_GOALS`
4. `V2_PENDING_PARALLEL_REVIEWS`
5. `V2_PENDING_HOD_REVIEW`
6. `V2_PENDING_GM_DECISION` or `V2_PENDING_HR_LOW_PERFORMER`
7. `V2_PENDING_HR_PROMOTION` / `V2_PROMOTION_DEFERRED` / `V2_COMPLETED_NO_PROMOTION`

## Key Endpoints
- `POST /api/rm/goals/assign`
- `POST /api/v2/workflow/goal-sets/{goalSetId}/activation`
- `POST /api/v2/workflow/evaluations/{evaluationId}/activation/decision`
- `POST /api/evaluations/{evaluationId}/assign-peers`
- `POST /api/v2/workflow/evaluations/{evaluationId}/self-evaluation`
- `POST /api/v2/workflow/evaluations/{evaluationId}/hod/finalize`
- `POST /api/v2/workflow/evaluations/{evaluationId}/gm/decision`
- `GET /api/v2/workflow/pip-cases`
