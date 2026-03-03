using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service for RM to assign goals to employees from the system goal library.
/// Flow: RM selects goals ? assigns to employee ? PersonalGoals are created ? auto-submitted for evaluation.
/// </summary>
public class RmGoalAssignmentService : IRmGoalAssignmentService
{
    private readonly EpecpsDbContext _context;
    private readonly IEvaluationWorkflowService _evaluationWorkflowService;

    public RmGoalAssignmentService(EpecpsDbContext context, IEvaluationWorkflowService evaluationWorkflowService)
    {
        _context = context;
        _evaluationWorkflowService = evaluationWorkflowService;
    }

    public async Task<List<GoalLibraryItemDto>> GetGoalLibraryAsync(CancellationToken cancellationToken = default)
    {
        // Return all active goals from non-archived templates (both published and draft)
        // Admin adds goals via the Goal Library which may use draft templates
        var items = await _context.ScoreItems
            .Where(i => i.IsActive && i.Category.IsActive && !i.Category.Template.IsArchived)
            .Include(i => i.Category)
                .ThenInclude(c => c.Template)
            .OrderBy(i => i.Category.Template.Name)
            .ThenBy(i => i.Category.DisplayOrder)
            .ThenBy(i => i.DisplayOrder)
            .Select(i => new GoalLibraryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                CategoryName = i.Category.Name,
                TemplateName = i.Category.Template.Name,
                TargetScore = i.TargetScore,
                MaxScore = i.MaxScore,
                IsMandatory = i.IsMandatory
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<List<RmEmployeeDto>> GetMyEmployeesAsync(int rmUserId, CancellationToken cancellationToken = default)
    {
        // Return ALL users in the system (including the RM themselves)
        // so the RM can assign goals to anyone, even themselves for testing
        var allEmployees = await _context.Users
            .Include(u => u.Department)
            .OrderBy(u => u.FullName)
            .Select(u => new RmEmployeeDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Department = u.Department != null ? u.Department.Name : "Unassigned"
            })
            .ToListAsync(cancellationToken);

        return allEmployees;
    }

    public async Task<RmAssignGoalsResponseDto> AssignGoalsToEmployeeAsync(int rmUserId, RmAssignGoalsDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Goals == null || dto.Goals.Count == 0)
            throw new BusinessRuleException("At least one goal must be assigned.");

        // Validate employee exists
        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == dto.EmployeeUserId, cancellationToken);

        if (employee == null)
            throw new NotFoundException(nameof(User), dto.EmployeeUserId);

        // Validate RM exists
        var rmUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == rmUserId, cancellationToken);

        if (rmUser == null)
            throw new NotFoundException(nameof(User), rmUserId);

        // Use a transaction to ensure all-or-nothing
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var goalSetId = Guid.NewGuid();
            var createdGoals = new List<PersonalGoal>();

            foreach (var goalDto in dto.Goals)
            {
                // Validate goal item exists
                var goalItem = await _context.ScoreItems
                    .Include(i => i.Category)
                    .FirstOrDefaultAsync(i => i.Id == goalDto.GoalItemId && i.IsActive, cancellationToken);

                if (goalItem == null)
                    throw new NotFoundException(nameof(ScoreItem), goalDto.GoalItemId);

                var title = !string.IsNullOrWhiteSpace(goalDto.Title) ? goalDto.Title : goalItem.Name;
                var description = !string.IsNullOrWhiteSpace(goalDto.Description) ? goalDto.Description : goalItem.Description;

                // Create the corresponding PersonalGoal for the employee
                var personalGoal = new PersonalGoal
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.EmployeeUserId,
                    GoalItemId = goalDto.GoalItemId,
                    GoalSetId = goalSetId,
                    Title = title,
                    Description = description,
                    TargetScore = goalItem.TargetScore,
                    StartDate = dto.StartDate,
                    DueDate = dto.DueDate,
                    Status = PersonalGoalStatus.ApprovedByRM, // Already approved since RM created it
                    CurrentScore = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PersonalGoals.Add(personalGoal);
                createdGoals.Add(personalGoal);

                // Create the GoalAssignment record
                var assignment = new GoalAssignment
                {
                    Id = Guid.NewGuid(),
                    AssignedByUserId = rmUserId,
                    AssignedToUserId = dto.EmployeeUserId,
                    GoalItemId = goalDto.GoalItemId,
                    GoalSetId = goalSetId,
                    Title = title,
                    Description = description,
                    TargetScore = goalItem.TargetScore,
                    StartDate = dto.StartDate,
                    DueDate = dto.DueDate,
                    Status = AssignedGoalStatus.Accepted, // Auto-accepted since RM is assigning
                    PersonalGoalId = personalGoal.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GoalAssignments.Add(assignment);

                // Add custom activities if provided
                if (goalDto.CustomActivities != null)
                {
                    foreach (var activityDesc in goalDto.CustomActivities)
                    {
                        if (!string.IsNullOrWhiteSpace(activityDesc))
                        {
                            var activity = new PersonalGoalActivity
                            {
                                Id = Guid.NewGuid(),
                                PersonalGoalId = personalGoal.Id,
                                SuggestedActivityId = null,
                                Description = activityDesc.Trim(),
                                IsFromTemplate = false,
                                Status = ActivityStatus.NotStarted,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.PersonalGoalActivities.Add(activity);
                        }
                    }
                }
            }

            // Save goals and assignments first
            await _context.SaveChangesAsync(cancellationToken);

            // Create an evaluation in Approved_By_RM status so the employee can start working
            var activeCycle = await _context.Set<Cycle>()
                .Where(c => c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeCycle == null)
            {
                activeCycle = new Cycle
                {
                    Name = $"Cycle {DateTime.UtcNow.Year}",
                    StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
                    EndDate = new DateTime(DateTime.UtcNow.Year, 12, 31),
                    Status = "Active"
                };
                _context.Set<Cycle>().Add(activeCycle);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Determine TL
            var teamLeadId = await GetTeamLeadIdAsync(dto.EmployeeUserId, cancellationToken);

            // Create evaluation in Approved_By_RM status (RM already approved since they assigned)
            var evaluation = new Evaluation
            {
                CycleId = activeCycle.CycleId,
                EmployeeId = dto.EmployeeUserId,
                ReportingManagerId = rmUserId,
                TeamLeadId = teamLeadId,
                GoalSetId = goalSetId,
                Status = "Approved_By_RM",
                OverallScore = null
            };

            _context.Set<Evaluation>().Add(evaluation);
            await _context.SaveChangesAsync(cancellationToken);

            // Create employee goals linked to the evaluation
            foreach (var pg in createdGoals)
            {
                var employeeGoal = new EmployeeGoal
                {
                    EvaluationId = evaluation.EvaluationId,
                    PersonalGoalId = pg.Id,
                    Title = pg.Title,
                    Description = pg.Description ?? string.Empty,
                    WeightPct = 100m / createdGoals.Count
                };
                _context.EmployeeGoals.Add(employeeGoal);
            }

            // Create self-review record (completed, since RM is acting on behalf of employee)
            var selfReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = dto.EmployeeUserId,
                ReviewerRole = ReviewerRole.Self,
                Status = "Completed",
                OverallComment = "Goals assigned by Reporting Manager.",
                OverallScore = null,
                SubmittedAt = DateTime.UtcNow
            };
            _context.Set<Review>().Add(selfReview);

            // Create RM review record
            var rmReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = rmUserId,
                ReviewerRole = ReviewerRole.RM,
                Status = "Approved",
                OverallScore = null,
                SubmittedAt = DateTime.UtcNow
            };
            _context.Set<Review>().Add(rmReview);

            // Create approval history entries
            var submitHistory = new ApprovalHistory
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewId = null,
                ActorUserId = rmUserId,
                ActorRole = "RM",
                Action = "RMAssignedGoals",
                Comment = $"RM assigned {createdGoals.Count} goal(s) to {employee.FullName}",
                FromStatus = "New",
                ToStatus = "Approved_By_RM",
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<ApprovalHistory>().Add(submitHistory);

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorUserId = rmUserId,
                EntityType = "GoalAssignment",
                EntityId = 0,
                Action = "RM_ASSIGNED_GOALS",
                BeforeJson = null,
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    GoalSetId = goalSetId,
                    EmployeeId = dto.EmployeeUserId,
                    GoalCount = createdGoals.Count,
                    EvaluationId = evaluation.EvaluationId
                }),
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<AuditLog>().Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RmAssignGoalsResponseDto
            {
                GoalSetId = goalSetId,
                GoalCount = createdGoals.Count,
                EmployeeName = employee.FullName,
                Message = $"Successfully assigned {createdGoals.Count} goal(s) to {employee.FullName}. The employee can now start working on them."
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<GoalAssignmentListDto>> GetMyAssignmentsAsync(int rmUserId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.GoalAssignments
            .Where(ga => ga.AssignedByUserId == rmUserId)
            .Include(ga => ga.AssignedToUser)
            .Include(ga => ga.GoalItem)
                .ThenInclude(gi => gi.Category)
            .OrderByDescending(ga => ga.CreatedAt)
            .Select(ga => new GoalAssignmentListDto
            {
                Id = ga.Id,
                GoalSetId = ga.GoalSetId,
                EmployeeUserId = ga.AssignedToUserId,
                EmployeeName = ga.AssignedToUser.FullName,
                EmployeeEmail = ga.AssignedToUser.Email,
                GoalItemName = ga.GoalItem.Name,
                CategoryName = ga.GoalItem.Category.Name,
                Title = ga.Title,
                Description = ga.Description,
                TargetScore = ga.TargetScore,
                Status = ga.Status.ToString(),
                StartDate = ga.StartDate,
                DueDate = ga.DueDate,
                CreatedAt = ga.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return assignments;
    }

    public async Task<List<GoalAssignmentListDto>> GetAssignmentsForEmployeeAsync(int rmUserId, int employeeUserId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.GoalAssignments
            .Where(ga => ga.AssignedByUserId == rmUserId && ga.AssignedToUserId == employeeUserId)
            .Include(ga => ga.AssignedToUser)
            .Include(ga => ga.GoalItem)
                .ThenInclude(gi => gi.Category)
            .OrderByDescending(ga => ga.CreatedAt)
            .Select(ga => new GoalAssignmentListDto
            {
                Id = ga.Id,
                GoalSetId = ga.GoalSetId,
                EmployeeUserId = ga.AssignedToUserId,
                EmployeeName = ga.AssignedToUser.FullName,
                EmployeeEmail = ga.AssignedToUser.Email,
                GoalItemName = ga.GoalItem.Name,
                CategoryName = ga.GoalItem.Category.Name,
                Title = ga.Title,
                Description = ga.Description,
                TargetScore = ga.TargetScore,
                Status = ga.Status.ToString(),
                StartDate = ga.StartDate,
                DueDate = ga.DueDate,
                CreatedAt = ga.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return assignments;
    }

    private async Task<int> GetTeamLeadIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        // Look for a TL in the same department
        var employee = await _context.Users.FirstOrDefaultAsync(u => u.UserId == employeeId, cancellationToken);
        if (employee == null) return employeeId;

        var teamLead = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.DeptId == employee.DeptId && u.UserId != employeeId)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "TL"))
            .FirstOrDefaultAsync(cancellationToken);

        return teamLead?.UserId ?? employeeId;
    }

    /// <summary>
    /// Comparer for deduplicating employee DTOs
    /// </summary>
    private class RmEmployeeDtoComparer : IEqualityComparer<RmEmployeeDto>
    {
        public bool Equals(RmEmployeeDto? x, RmEmployeeDto? y) => x?.UserId == y?.UserId;
        public int GetHashCode(RmEmployeeDto obj) => obj.UserId.GetHashCode();
    }
}
