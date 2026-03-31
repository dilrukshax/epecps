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
/// Workflow v2:
/// RM assigns >= 5 goals -> employee submits activation plan -> TL reviews activation.
/// </summary>
public class RmGoalAssignmentService : IRmGoalAssignmentService
{
    private readonly EpecpsDbContext _context;

    public RmGoalAssignmentService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<GoalLibraryItemDto>> GetGoalLibraryAsync(CancellationToken cancellationToken = default)
    {
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
        var managedEmployeeIds = await _context.UserManagerMappings
            .Where(m => m.ManagerUserId == rmUserId)
            .Select(m => m.EmployeeUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (managedEmployeeIds.Count == 0)
        {
            return new List<RmEmployeeDto>();
        }

        return await _context.Users
            .Include(u => u.Department)
            .Where(u => managedEmployeeIds.Contains(u.UserId))
            .OrderBy(u => u.FullName)
            .Select(u => new RmEmployeeDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Department = u.Department != null ? u.Department.Name : "Unassigned"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RmAssignGoalsResponseDto> AssignGoalsToEmployeeAsync(int rmUserId, RmAssignGoalsDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Goals == null || dto.Goals.Count < 5)
        {
            throw new BusinessRuleException("At least 5 goals must be assigned.");
        }

        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == dto.EmployeeUserId, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException(nameof(User), dto.EmployeeUserId);
        }

        var rmUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == rmUserId, cancellationToken);

        if (rmUser == null)
        {
            throw new NotFoundException(nameof(User), rmUserId);
        }

        await EnsureCanManageEmployeeAsync(rmUserId, dto.EmployeeUserId, cancellationToken);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var goalSetId = Guid.NewGuid();
            var createdGoals = new List<PersonalGoal>();

            foreach (var goalDto in dto.Goals)
            {
                var goalItem = await _context.ScoreItems
                    .FirstOrDefaultAsync(i => i.Id == goalDto.GoalItemId && i.IsActive, cancellationToken);

                if (goalItem == null)
                {
                    throw new NotFoundException(nameof(ScoreItem), goalDto.GoalItemId);
                }

                var title = !string.IsNullOrWhiteSpace(goalDto.Title) ? goalDto.Title : goalItem.Name;
                var description = !string.IsNullOrWhiteSpace(goalDto.Description) ? goalDto.Description : goalItem.Description;

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
                    Status = PersonalGoalStatus.ApprovedByRM,
                    CurrentScore = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PersonalGoals.Add(personalGoal);
                createdGoals.Add(personalGoal);

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
                    Status = AssignedGoalStatus.Accepted,
                    PersonalGoalId = personalGoal.Id,
                    CreatedAt = DateTime.UtcNow,
                    ActivationStatus = "PendingEmployee"
                };

                _context.GoalAssignments.Add(assignment);

                if (goalDto.CustomActivities == null)
                {
                    continue;
                }

                foreach (var activityDesc in goalDto.CustomActivities)
                {
                    if (string.IsNullOrWhiteSpace(activityDesc))
                    {
                        continue;
                    }

                    _context.PersonalGoalActivities.Add(new PersonalGoalActivity
                    {
                        Id = Guid.NewGuid(),
                        PersonalGoalId = personalGoal.Id,
                        SuggestedActivityId = null,
                        Description = activityDesc.Trim(),
                        IsFromTemplate = false,
                        Status = ActivityStatus.NotStarted,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var activeCycle = await _context.Cycles
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
                _context.Cycles.Add(activeCycle);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var teamLeadId = await GetTeamLeadIdAsync(dto.EmployeeUserId, cancellationToken);

            var evaluation = new Evaluation
            {
                CycleId = activeCycle.CycleId,
                EmployeeId = dto.EmployeeUserId,
                ReportingManagerId = rmUserId,
                TeamLeadId = teamLeadId,
                GoalSetId = goalSetId,
                WorkflowVersion = "v2",
                Status = "V2_PENDING_EMPLOYEE_ACTIVATION"
            };

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var pg in createdGoals)
            {
                _context.EmployeeGoals.Add(new EmployeeGoal
                {
                    EvaluationId = evaluation.EvaluationId,
                    PersonalGoalId = pg.Id,
                    Title = pg.Title,
                    Description = pg.Description ?? string.Empty,
                    WeightPct = 100m / createdGoals.Count
                });
            }

            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                EvaluationId = evaluation.EvaluationId,
                ActorUserId = rmUserId,
                ActorRole = "RM",
                Action = "RMAssignedGoalsV2",
                Comment = $"RM assigned {createdGoals.Count} goal(s) and initiated activation workflow.",
                FromStatus = "New",
                ToStatus = evaluation.Status,
                CreatedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                UserId = dto.EmployeeUserId,
                Subject = "Goals assigned - activation plan submission required",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = rmUserId,
                EntityType = "GoalAssignment",
                EntityId = 0,
                Action = "RM_ASSIGNED_GOALS_V2",
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    GoalSetId = goalSetId,
                    EmployeeId = dto.EmployeeUserId,
                    GoalCount = createdGoals.Count,
                    EvaluationId = evaluation.EvaluationId,
                    WorkflowVersion = "v2",
                    Status = evaluation.Status
                }),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RmAssignGoalsResponseDto
            {
                GoalSetId = goalSetId,
                GoalCount = createdGoals.Count,
                EmployeeName = employee.FullName,
                Message = $"Successfully assigned {createdGoals.Count} goals to {employee.FullName}. Employee activation plan is now pending."
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
        var managedEmployeeIds = await _context.UserManagerMappings
            .Where(m => m.ManagerUserId == rmUserId)
            .Select(m => m.EmployeeUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var assignments = await _context.GoalAssignments
            .Where(ga => ga.AssignedByUserId == rmUserId || managedEmployeeIds.Contains(ga.AssignedToUserId))
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
        await EnsureCanManageEmployeeAsync(rmUserId, employeeUserId, cancellationToken);

        var assignments = await _context.GoalAssignments
            .Where(ga => ga.AssignedToUserId == employeeUserId)
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

    private async Task EnsureCanManageEmployeeAsync(int managerUserId, int employeeUserId, CancellationToken cancellationToken)
    {
        var isSuperAdmin = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(
                ur => ur.UserId == managerUserId &&
                      (ur.Role.Name == "SuperAdmin" || ur.Role.Name == "Admin"),
                cancellationToken);

        if (isSuperAdmin)
        {
            return;
        }

        var isMapped = await _context.UserManagerMappings
            .AnyAsync(
                m => m.ManagerUserId == managerUserId && m.EmployeeUserId == employeeUserId,
                cancellationToken);

        if (!isMapped)
        {
            throw new BusinessRuleException("You are not mapped as a reporting manager for this employee.");
        }
    }

    private async Task<int> GetTeamLeadIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == employeeId, cancellationToken);

        if (employee == null)
        {
            return employeeId;
        }

        var teamLead = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.DeptId == employee.DeptId && u.UserId != employeeId)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "TL"))
            .FirstOrDefaultAsync(cancellationToken);

        return teamLead?.UserId ?? employeeId;
    }
}
