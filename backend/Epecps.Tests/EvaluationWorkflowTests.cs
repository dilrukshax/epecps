using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.DTOs.WorkflowV2;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Epecps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Epecps.Tests;

/// <summary>
/// Unit tests for the RM-first approval workflow with employee Start/Complete goals
/// </summary>
public class EvaluationWorkflowTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IWorkflowV2Service> _workflowV2ServiceMock;
    
    public EvaluationWorkflowTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _workflowV2ServiceMock = new Mock<IWorkflowV2Service>();
        // Setup email service to do nothing (async completion)
        _emailServiceMock
            .Setup(x => x.SendEvaluationNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailServiceMock
            .Setup(x => x.SendApprovalNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailServiceMock
            .Setup(x => x.SendRejectionNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private EpecpsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<EpecpsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        return new EpecpsDbContext(options);
    }

    private async Task<(EpecpsDbContext context, int employeeId, int rmId, int tlId, Guid goalSetId, int cycleId)> SetupTestDataAsync()
    {
        var context = CreateInMemoryContext();

        // Create roles
        var employeeRole = new Role { RoleId = 1, Name = "Employee" };
        var rmRole = new Role { RoleId = 2, Name = "RM" };
        var tlRole = new Role { RoleId = 3, Name = "TL" };
        var hodRole = new Role { RoleId = 4, Name = "HOD" };
        var peerRole = new Role { RoleId = 5, Name = "Peer" };
        var hrRole = new Role { RoleId = 6, Name = "HR" };
        var gmRole = new Role { RoleId = 7, Name = "GM" };
        
        context.Roles.AddRange(employeeRole, rmRole, tlRole, hodRole, peerRole, hrRole, gmRole);
        await context.SaveChangesAsync();

        // Create department
        var dept = new Department { DeptId = 1, Name = "Engineering" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        // Create users
        var employee = new User { UserId = 1, Email = "employee@test.com", FullName = "Test Employee", DeptId = 1 };
        var rm = new User { UserId = 2, Email = "rm@test.com", FullName = "Test RM", DeptId = 1 };
        var tl = new User { UserId = 3, Email = "tl@test.com", FullName = "Test TL", DeptId = 1 };
        var hod = new User { UserId = 4, Email = "hod@test.com", FullName = "Test HOD", DeptId = 1 };
        var peer1 = new User { UserId = 5, Email = "peer1@test.com", FullName = "Peer Reviewer One", DeptId = 1 };
        var peer2 = new User { UserId = 6, Email = "peer2@test.com", FullName = "Peer Reviewer Two", DeptId = 1 };
        var hr = new User { UserId = 7, Email = "hr@test.com", FullName = "Test HR", DeptId = 1 };
        var gm = new User { UserId = 8, Email = "gm@test.com", FullName = "Test GM", DeptId = 1 };
        
        context.Users.AddRange(employee, rm, tl, hod, peer1, peer2, hr, gm);
        await context.SaveChangesAsync();

        // Assign roles
        context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 }); // Employee
        context.UserRoles.Add(new UserRole { UserId = 2, RoleId = 2 }); // RM
        context.UserRoles.Add(new UserRole { UserId = 3, RoleId = 3 }); // TL
        context.UserRoles.Add(new UserRole { UserId = 4, RoleId = 4 }); // HOD
        context.UserRoles.Add(new UserRole { UserId = 5, RoleId = 5 }); // Peer
        context.UserRoles.Add(new UserRole { UserId = 6, RoleId = 5 }); // Peer
        context.UserRoles.Add(new UserRole { UserId = 7, RoleId = 6 }); // HR
        context.UserRoles.Add(new UserRole { UserId = 8, RoleId = 7 }); // GM
        await context.SaveChangesAsync();

        // Create cycle
        var cycle = new Cycle 
        { 
            CycleId = 1, 
            Name = "Test Cycle 2024", 
            StartDate = DateTime.UtcNow.AddMonths(-6), 
            EndDate = DateTime.UtcNow.AddMonths(6),
            Status = "Active"
        };
        context.Cycles.Add(cycle);
        await context.SaveChangesAsync();

        // Create score template and items
        var template = new ScoreTemplate { Id = Guid.NewGuid(), Name = "Test Template", Version = 1, IsPublished = true, IsArchived = false };
        context.ScoreTemplates.Add(template);
        await context.SaveChangesAsync();

        var category = new ScoreCategory { Id = Guid.NewGuid(), ScoreTemplateId = template.Id, Name = "Test Category", DisplayOrder = 1 };
        context.ScoreCategories.Add(category);
        await context.SaveChangesAsync();

        var goalItem = new ScoreItem 
        { 
            Id = Guid.NewGuid(), 
            ScoreCategoryId = category.Id, 
            Name = "Test Goal Item", 
            TargetScore = 100,
            MaxScore = 100,
            IsActive = true,
            ItemType = ScoreItemType.Rating
        };
        context.ScoreItems.Add(goalItem);
        await context.SaveChangesAsync();

        // Create personal goals
        var goalSetId = Guid.NewGuid();
        var goal1 = new PersonalGoal
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            GoalItemId = goalItem.Id,
            GoalSetId = goalSetId,
            Title = "Goal 1",
            Description = "Test goal 1",
            TargetScore = 100,
            CurrentScore = 0,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddMonths(3),
            Status = PersonalGoalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        var goal2 = new PersonalGoal
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            GoalItemId = goalItem.Id,
            GoalSetId = goalSetId,
            Title = "Goal 2",
            Description = "Test goal 2",
            TargetScore = 100,
            CurrentScore = 0,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddMonths(3),
            Status = PersonalGoalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        context.PersonalGoals.AddRange(goal1, goal2);
        await context.SaveChangesAsync();

        return (context, 1, 2, 3, goalSetId, 1);
    }

    [Fact]
    public async Task SubmitGoalSet_CreatesEvaluationWithPendingRMReviewStatus()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        // Act
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        // Assert
        Assert.NotNull(evaluation);
        Assert.Equal("Pending_RM_Review", evaluation.Status);
        Assert.Equal(employeeId, evaluation.EmployeeId);
        Assert.Equal(goalSetId, evaluation.GoalSetId);

        // Verify goals are updated to PendingRMReview
        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.PendingRMReview, g.Status));

        // Verify approval history was created
        var history = await context.Set<ApprovalHistory>().FirstOrDefaultAsync(h => h.EvaluationId == evaluation.EvaluationId);
        Assert.NotNull(history);
        Assert.Equal("SubmittedToRM", history.Action);
        Assert.Equal("Employee", history.ActorRole);

        // Verify notification was created for RM
        var notification = await context.Set<Notification>().FirstOrDefaultAsync(n => n.UserId == rmId);
        Assert.NotNull(notification);
    }

    [Fact]
    public async Task RMApprove_TransitionsToApprovedByRM_AndNotifiesEmployee()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        
        // Submit goal set first
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        // Act
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Goals look good, approved!");

        // Assert
        var updatedEvaluation = await context.Set<Evaluation>().FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updatedEvaluation);
        Assert.Equal("Approved_By_RM", updatedEvaluation.Status);

        // Verify goals are updated to ApprovedByRM
        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.ApprovedByRM, g.Status));

        // Verify approval history was created
        var history = await context.Set<ApprovalHistory>()
            .Where(h => h.EvaluationId == evaluation.EvaluationId && h.Action == "RMApproved")
            .FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.Equal("RM", history.ActorRole);

        // Verify notification was created for employee
        var notification = await context.Set<Notification>()
            .Where(n => n.UserId == employeeId && n.Subject.Contains("Approved"))
            .FirstOrDefaultAsync();
        Assert.NotNull(notification);
    }

    [Fact]
    public async Task MappedManagerCanApproveRmStages()
    {
        // Arrange
        var (context, employeeId, _, _, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        var mappedManager = new User
        {
            UserId = 9,
            Email = "mapped-rm@test.com",
            FullName = "Mapped RM",
            DeptId = 1,
            IsActive = true
        };
        context.Users.Add(mappedManager);
        context.UserRoles.Add(new UserRole { UserId = 9, RoleId = 2 }); // RM
        context.UserManagerMappings.Add(new UserManagerMapping
        {
            ManagerUserId = 9,
            EmployeeUserId = employeeId
        });
        await context.SaveChangesAsync();

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        // Act - first RM approval by mapped manager
        await workflowService.ApproveAsync(evaluation.EvaluationId, 9, "Approved by mapped manager");

        // Assert
        var updatedEvaluation = await context.Evaluations.FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updatedEvaluation);
        Assert.Equal("Approved_By_RM", updatedEvaluation!.Status);
    }

    [Fact]
    public async Task EmployeeCompleteAllGoals_TriggersWorkflowContinuationToRmPostCompletion()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        // Submit goal set and get RM approval
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        // Get all goals
        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();

        // Start each goal
        foreach (var goal in goals)
        {
            await personalGoalService.StartGoalAsync(goal.Id, employeeId);
        }

        // Verify goals are InProgress
        goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.InProgress, g.Status));

        // Act - Complete each goal
        GoalActionResponseDto? lastResult = null;
        foreach (var goal in goals)
        {
            lastResult = await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto { Comment = "Completed" });
        }

        // Assert
        Assert.NotNull(lastResult);
        Assert.True(lastResult.WorkflowContinued);
        Assert.Equal("Pending_RM_Review_PostCompletion", lastResult.EvaluationStatus);

        // Verify goals are UnderEvaluation
        goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.UnderEvaluation, g.Status));

        // Verify RM post-completion review was created
        var rmReview = await context.Set<Review>()
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.RM)
            .OrderByDescending(r => r.ReviewId)
            .FirstOrDefaultAsync();
        Assert.NotNull(rmReview);
        Assert.Equal("Pending", rmReview.Status);

        // Verify approval history has workflow continuation entry
        var history = await context.Set<ApprovalHistory>()
            .Where(h => h.EvaluationId == evaluation.EvaluationId && h.Action.Contains("WorkflowContinued"))
            .FirstOrDefaultAsync();
        Assert.NotNull(history);
    }

    [Fact]
    public async Task EmployeeCompleteAllGoals_InV2ActiveGoals_AutoContinuesToRmPostCompletion()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_ACTIVE_GOALS"
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        foreach (var goal in goals)
        {
            goal.Status = PersonalGoalStatus.InProgress;
            goal.StartedAt = DateTime.UtcNow.AddDays(-1);
            goal.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        }
        await context.SaveChangesAsync();

        // Act - Complete each goal
        GoalActionResponseDto? lastResult = null;
        foreach (var goal in goals)
        {
            lastResult = await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto
            {
                EvidenceUrl = $"https://evidence.example.com/{goal.Id}",
                CertificationUrl = $"https://cert.example.com/{goal.Id}",
                Summary = $"Completed {goal.Title}",
                Comment = "Completed via auto-submit path",
                CurrentScore = 90
            });
        }

        // Assert auto-transition to RM post-completion
        Assert.NotNull(lastResult);
        Assert.True(lastResult!.WorkflowContinued);
        Assert.Equal("Pending_RM_Review_PostCompletion", lastResult.EvaluationStatus);

        var updatedEvaluation = await context.Evaluations
            .Include(e => e.Reviews)
            .FirstAsync(e => e.EvaluationId == evaluation.EvaluationId);
        Assert.Equal("Pending_RM_Review_PostCompletion", updatedEvaluation.Status);

        // Assert goals moved to under evaluation
        var updatedGoals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(updatedGoals, g => Assert.Equal(PersonalGoalStatus.UnderEvaluation, g.Status));

        // Assert auto-generated self review exists and is completed
        var selfReview = updatedEvaluation.Reviews
            .Where(r => r.ReviewerRole == ReviewerRole.Self && r.ReviewerUserId == employeeId)
            .OrderByDescending(r => r.ReviewId)
            .FirstOrDefault();
        Assert.NotNull(selfReview);
        Assert.Equal("Completed", selfReview!.Status);

        // Assert RM post-completion review is pending
        var rmReview = updatedEvaluation.Reviews
            .Where(r => r.ReviewerRole == ReviewerRole.RM && r.ReviewerUserId == rmId)
            .OrderByDescending(r => r.ReviewId)
            .FirstOrDefault();
        Assert.NotNull(rmReview);
        Assert.Equal("Pending", rmReview!.Status);
    }

    [Fact]
    public async Task GetMyGoalSets_ReturnsFullChronologicalApprovalHistory_WithStableSecondaryOrdering()
    {
        // Arrange
        var (context, employeeId, rmId, _, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved by RM");

        var goals = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId)
            .ToListAsync();

        foreach (var goal in goals)
        {
            await personalGoalService.StartGoalAsync(goal.Id, employeeId);
            await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto
            {
                Comment = "Completed",
                Summary = "Goal completed",
                EvidenceUrl = "https://example.com/evidence"
            });
        }

        var tieTimestamp = DateTime.UtcNow.AddMinutes(1);
        context.Set<ApprovalHistory>().AddRange(
            new ApprovalHistory
            {
                EvaluationId = evaluation.EvaluationId,
                ActorUserId = rmId,
                ActorRole = "RM",
                Action = "TieOrderA",
                Comment = "Tie A",
                FromStatus = "Pending_RM_Review_PostCompletion",
                ToStatus = "Pending_RM_Review_PostCompletion",
                CreatedAt = tieTimestamp
            },
            new ApprovalHistory
            {
                EvaluationId = evaluation.EvaluationId,
                ActorUserId = rmId,
                ActorRole = "RM",
                Action = "TieOrderB",
                Comment = "Tie B",
                FromStatus = "Pending_RM_Review_PostCompletion",
                ToStatus = "Pending_RM_Review_PostCompletion",
                CreatedAt = tieTimestamp
            });

        // Add a system-origin style event with blank user name to validate actor fallback.
        var systemLikeUser = new User
        {
            UserId = 99,
            Email = "system-like@test.com",
            FullName = string.Empty,
            DeptId = 1,
            IsActive = true
        };
        context.Users.Add(systemLikeUser);
        context.UserRoles.Add(new UserRole { UserId = 99, RoleId = 1 });
        context.Set<ApprovalHistory>().Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = 99,
            ActorRole = "System",
            Action = "SystemGeneratedEvent",
            Comment = "Created by system workflow",
            FromStatus = "Pending_RM_Review_PostCompletion",
            ToStatus = "Pending_RM_Review_PostCompletion",
            CreatedAt = tieTimestamp.AddMinutes(1)
        });

        await context.SaveChangesAsync();

        // Act
        var goalSets = await personalGoalService.GetMyGoalSetsAsync(employeeId);
        var goalSet = goalSets.Single(gs => gs.GoalSetId == goalSetId);
        var approvalHistory = goalSet.EvaluationInfo!.ApprovalHistory;

        // Assert - full history includes auto-transition event
        Assert.NotEmpty(approvalHistory);
        Assert.Contains(approvalHistory, h => h.Action.Contains("WorkflowContinued"));

        // Assert - stable ordering: CreatedAt, then Id
        for (var i = 1; i < approvalHistory.Count; i++)
        {
            var previous = approvalHistory[i - 1];
            var current = approvalHistory[i];

            var isOrdered = previous.CreatedAt < current.CreatedAt
                || (previous.CreatedAt == current.CreatedAt && previous.Id < current.Id);

            Assert.True(isOrdered, $"Approval history out of order between IDs {previous.Id} and {current.Id}");
        }

        // Assert - same timestamp events are sorted by Id
        var tieEvents = approvalHistory
            .Where(h => h.Action == "TieOrderA" || h.Action == "TieOrderB")
            .ToList();
        Assert.Equal(2, tieEvents.Count);
        Assert.True(tieEvents[0].Id < tieEvents[1].Id);

        // Assert - actor display falls back to role for system-like events
        var systemEvent = approvalHistory.Single(h => h.Action == "SystemGeneratedEvent");
        Assert.Equal("System", systemEvent.ActorName);
    }

    [Fact]
    public async Task GetPendingApprovals_AutoAdvancesCompletedV2ActiveGoals_ForRm()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_ACTIVE_GOALS"
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        foreach (var goal in goals)
        {
            goal.Status = PersonalGoalStatus.Completed;
            goal.CurrentScore = goal.TargetScore;
            goal.CompletedAt = DateTime.UtcNow;
            goal.CompletionSummary = $"Completed {goal.Title}";
            goal.CompletionEvidenceUrl = $"https://evidence.example.com/{goal.Id}";
            goal.CompletionComment = "Completed";
        }

        context.EmployeeGoals.AddRange(goals.Select(g => new EmployeeGoal
        {
            EvaluationId = evaluation.EvaluationId,
            PersonalGoalId = g.Id,
            Title = g.Title,
            Description = g.Description ?? string.Empty,
            WeightPct = 100m / goals.Count
        }));
        await context.SaveChangesAsync();

        // Act
        var pendingApprovals = (await workflowService.GetPendingApprovalsForUserAsync(rmId)).ToList();

        // Assert
        var rmApproval = pendingApprovals.FirstOrDefault(p =>
            p.EvaluationId == evaluation.EvaluationId &&
            p.RequiredRole == "RM");
        Assert.NotNull(rmApproval);
        Assert.Equal("Pending_RM_Review_PostCompletion", rmApproval!.Status);

        var updatedEvaluation = await context.Evaluations
            .Include(e => e.Reviews)
            .FirstAsync(e => e.EvaluationId == evaluation.EvaluationId);
        Assert.Equal("Pending_RM_Review_PostCompletion", updatedEvaluation.Status);
        Assert.Contains(updatedEvaluation.Reviews, r =>
            r.ReviewerRole == ReviewerRole.RM &&
            r.ReviewerUserId == rmId &&
            r.Status == "Pending");
    }

    [Fact]
    public async Task GetAvailablePeersAsync_FiltersToActiveNonManagerCandidates()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        var adminRole = new Role { RoleId = 9, Name = "Admin" };
        var superAdminRole = new Role { RoleId = 10, Name = "SuperAdmin" };
        context.Roles.AddRange(adminRole, superAdminRole);

        var adminUser = new User { UserId = 20, Email = "admin@test.com", FullName = "Admin User", DeptId = 1, IsActive = true };
        var superAdminUser = new User { UserId = 21, Email = "superadmin@test.com", FullName = "Super Admin User", DeptId = 1, IsActive = true };
        var inactiveEmployee = new User { UserId = 22, Email = "inactive@test.com", FullName = "Inactive Employee", DeptId = 1, IsActive = false };
        var eligibleEmployee = new User { UserId = 23, Email = "eligible@test.com", FullName = "Eligible Employee", DeptId = 1, IsActive = true };
        context.Users.AddRange(adminUser, superAdminUser, inactiveEmployee, eligibleEmployee);

        context.UserRoles.AddRange(
            new UserRole { UserId = 20, RoleId = 9 },   // Admin
            new UserRole { UserId = 21, RoleId = 10 },  // SuperAdmin
            new UserRole { UserId = 22, RoleId = 1 },   // Employee but inactive
            new UserRole { UserId = 23, RoleId = 1 });  // Eligible employee-level user

        await context.SaveChangesAsync();

        // Act
        var peers = (await workflowService.GetAvailablePeersAsync(evaluation.EvaluationId)).ToList();
        var peerIds = peers.Select(p => p.UserId).ToHashSet();

        // Assert - core exclusions
        Assert.DoesNotContain(employeeId, peerIds);
        Assert.DoesNotContain(rmId, peerIds);
        Assert.DoesNotContain(tlId, peerIds);

        // Assert - manager/privileged exclusions
        Assert.DoesNotContain(4, peerIds);   // HOD
        Assert.DoesNotContain(7, peerIds);   // HR
        Assert.DoesNotContain(8, peerIds);   // GM
        Assert.DoesNotContain(20, peerIds);  // Admin
        Assert.DoesNotContain(21, peerIds);  // SuperAdmin

        // Assert - inactive excluded
        Assert.DoesNotContain(22, peerIds);

        // Assert - eligible non-manager users are included
        Assert.Contains(5, peerIds);
        Assert.Contains(6, peerIds);
        Assert.Contains(23, peerIds);
    }

    [Fact]
    public async Task RMReject_TransitionsToReturnedToEmployee()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        
        // Submit goal set first
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        // Act
        await workflowService.RejectAsync(evaluation.EvaluationId, rmId, "Please revise goals based on feedback.");

        // Assert
        var updatedEvaluation = await context.Set<Evaluation>().FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updatedEvaluation);
        Assert.Equal("Returned_To_Employee", updatedEvaluation.Status);

        // Verify goals are returned to employee
        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.ReturnedToEmployee, g.Status));

        // Verify approval history was created
        var history = await context.Set<ApprovalHistory>()
            .Where(h => h.EvaluationId == evaluation.EvaluationId && h.Action == "RMRejected")
            .FirstOrDefaultAsync();
        Assert.NotNull(history);
    }

    [Fact]
    public async Task StartGoal_RequiresApprovedByRMStatus()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        // Submit goal set but DON'T get RM approval
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        var goalToStart = goals.First();

        // Act & Assert - should throw because goal is not approved by RM yet
        await Assert.ThrowsAsync<BusinessRuleException>(() => 
            personalGoalService.StartGoalAsync(goalToStart.Id, employeeId));
    }

    [Fact]
    public async Task StartGoal_InWorkflowV2PendingEmployeeActivation_IsBlockedUntilRmApproval()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var goal = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .OrderBy(g => g.Title)
            .FirstAsync();
        goal.Status = PersonalGoalStatus.ApprovedByRM;

        context.Evaluations.Add(new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_PENDING_EMPLOYEE_ACTIVATION"
        });
        await context.SaveChangesAsync();

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            personalGoalService.StartGoalAsync(goal.Id, employeeId));

        // Assert
        Assert.Contains("activation approval", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteGoal_RequiresInProgressStatus()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        // Submit and approve
        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        var goalToComplete = goals.First();
        
        // Goal is in ApprovedByRM status but not started yet

        // Act & Assert - should throw because goal is not in progress
        await Assert.ThrowsAsync<BusinessRuleException>(() => 
            personalGoalService.CompleteGoalAsync(goalToComplete.Id, employeeId, null));
    }

    [Fact]
    public async Task CompleteGoal_InWorkflowV2PendingActivationReview_IsBlockedUntilRmApproval()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var goal = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .OrderBy(g => g.Title)
            .FirstAsync();
        goal.Status = PersonalGoalStatus.InProgress;
        goal.StartedAt = DateTime.UtcNow.AddDays(-1);

        context.Evaluations.Add(new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_PENDING_RM_ACTIVATION_REVIEW"
        });
        await context.SaveChangesAsync();

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto { Comment = "done" }));

        // Assert
        Assert.Contains("activation approval", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteGoal_PersistsCompletionFields_AndReturnsThemInEvaluationDetails()
    {
        // Arrange
        var (context, employeeId, rmId, _, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        var goal = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId)
            .OrderBy(g => g.Title)
            .FirstAsync();

        await personalGoalService.StartGoalAsync(goal.Id, employeeId);

        var request = new CompleteGoalRequestDto
        {
            CurrentScore = 92,
            EvidenceUrl = "https://evidence.example.com/work-item",
            CertificationUrl = "https://cert.example.com/certificate",
            Summary = "Delivered the expected milestone with measurable outcomes.",
            Comment = "Completed on schedule with all dependencies closed."
        };

        // Act
        var response = await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, request);

        // Assert persistence on PersonalGoal
        var updatedGoal = await context.PersonalGoals.FindAsync(goal.Id);
        Assert.NotNull(updatedGoal);
        Assert.Equal(request.EvidenceUrl, updatedGoal!.CompletionEvidenceUrl);
        Assert.Equal(request.CertificationUrl, updatedGoal.CompletionCertificationUrl);
        Assert.Equal(request.Summary, updatedGoal.CompletionSummary);
        Assert.Equal(request.Comment, updatedGoal.CompletionComment);
        Assert.False(response.WorkflowContinued);

        // Assert backward-compatible sync to EmployeeGoal.EvidenceUri
        var employeeGoal = await context.EmployeeGoals
            .FirstOrDefaultAsync(eg => eg.EvaluationId == evaluation.EvaluationId && eg.PersonalGoalId == goal.Id);
        Assert.NotNull(employeeGoal);
        Assert.Equal(request.EvidenceUrl, employeeGoal!.EvidenceUri);

        // Assert evaluation detail API includes completion fields
        var detail = await workflowService.GetEvaluationDetailsAsync(evaluation.EvaluationId, employeeId);
        var goalDto = detail.Goals.First(g => g.PersonalGoalId == goal.Id);

        Assert.Equal(request.EvidenceUrl, goalDto.CompletionEvidenceUrl);
        Assert.Equal(request.CertificationUrl, goalDto.CompletionCertificationUrl);
        Assert.Equal(request.Summary, goalDto.CompletionSummary);
        Assert.Equal(request.Comment, goalDto.CompletionComment);
    }

    [Fact]
    public async Task GetMyGoalSets_ReturnsGoalActivationFieldsForInlinePlanUi()
    {
        // Arrange
        var (context, employeeId, rmId, _, goalSetId, _) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var goal = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .OrderBy(g => g.Title)
            .FirstAsync();

        var assignmentId = Guid.NewGuid();
        context.GoalAssignments.Add(new GoalAssignment
        {
            Id = assignmentId,
            AssignedByUserId = rmId,
            AssignedToUserId = employeeId,
            GoalItemId = goal.GoalItemId,
            GoalSetId = goalSetId,
            PersonalGoalId = goal.Id,
            Title = goal.Title,
            Description = goal.Description,
            TargetScore = goal.TargetScore,
            StartDate = goal.StartDate,
            DueDate = goal.DueDate,
            Status = AssignedGoalStatus.Accepted,
            ActivationStatus = "PendingRM",
            ActivationMethod = "Deliver by weekly milestones",
            ActivationTlComment = "Please clarify dependencies",
            ActivationSubmittedAt = DateTime.UtcNow.AddHours(-2),
            ActivationReviewedAt = DateTime.UtcNow.AddHours(-1)
        });
        await context.SaveChangesAsync();

        // Act
        var sets = await personalGoalService.GetMyGoalSetsAsync(employeeId);
        var dto = sets.SelectMany(s => s.Goals).First(g => g.Id == goal.Id);

        // Assert
        Assert.Equal(assignmentId, dto.GoalAssignmentId);
        Assert.Equal("PendingRM", dto.ActivationStatus);
        Assert.Equal("Deliver by weekly milestones", dto.ActivationMethod);
        Assert.Equal("Please clarify dependencies", dto.ActivationComment);
        Assert.NotNull(dto.ActivationSubmittedAt);
        Assert.NotNull(dto.ActivationReviewedAt);
    }

    [Fact]
    public async Task RMCannotApprovePostCompletionWithoutPerGoalScoring()
    {
        // Arrange
        var (context, employeeId, rmId, _, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        foreach (var goal in goals)
        {
            await personalGoalService.StartGoalAsync(goal.Id, employeeId);
            await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto
            {
                Summary = "Done",
                EvidenceUrl = "https://evidence.example.com",
                Comment = "Completed"
            });
        }

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approve without scoring"));

        // Assert
        Assert.Contains("must submit per-goal scores", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task AssignPeerReviewers_RejectsManagerialPeerSelection()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            Status = "Pending_Peer_Assignment",
            WorkflowVersion = "v1"
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            workflowService.AssignPeerReviewersAsync(evaluation.EvaluationId, tlId, 4, 5));

        // Assert
        Assert.Contains("active non-manager employees", ex.Message);
    }

    [Fact]
    public async Task SubmitTlCombinedReview_RejectsManagerialPeerSelection()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            Status = "Pending_TL_Review",
            WorkflowVersion = "v1"
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        context.Reviews.Add(new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = tlId,
            ReviewerRole = ReviewerRole.TL,
            Status = "Pending"
        });
        await context.SaveChangesAsync();

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            workflowService.SubmitTlOverallAndAssignPeersAsync(
                evaluation.EvaluationId,
                tlId,
                8.5m,
                "TL submission",
                4,
                5));

        // Assert
        Assert.Contains("active non-manager employees", ex.Message);
    }

    [Fact]
    public async Task TlCombinedSubmit_SavesOverallScore_AssignsPeers_AndMovesToPeerReview()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        foreach (var goal in goals)
        {
            await personalGoalService.StartGoalAsync(goal.Id, employeeId);
            await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto
            {
                Summary = "Done",
                EvidenceUrl = "https://evidence.example.com",
                Comment = "Completed"
            });
        }

        var rmPostReview = await context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.RM)
            .OrderByDescending(r => r.ReviewId)
            .FirstAsync();
        rmPostReview.Status = "Completed";
        rmPostReview.OverallScore = 8.8m;
        rmPostReview.SubmittedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "RM scored and approved");

        // Act
        await workflowService.SubmitTlOverallAndAssignPeersAsync(
            evaluation.EvaluationId,
            tlId,
            9.0m,
            "Strong overall performance",
            5,
            6);

        // Assert status transition
        var updatedEvaluation = await context.Evaluations.FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updatedEvaluation);
        Assert.Equal("Pending_Peer_Reviews", updatedEvaluation!.Status);

        // Assert TL review and score
        var tlReview = await context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.TL)
            .OrderByDescending(r => r.ReviewId)
            .FirstAsync();
        Assert.Equal("Completed", tlReview.Status);
        Assert.Equal(9.0m, tlReview.OverallScore);

        var tlOverallScore = await context.Set<ReviewScore>()
            .FirstOrDefaultAsync(rs => rs.ReviewId == tlReview.ReviewId && rs.PersonalGoalId == null);
        Assert.NotNull(tlOverallScore);
        Assert.Equal(9.0m, tlOverallScore!.ScoreValue);

        // Assert peer assignments and pending peer reviews
        var assignments = await context.PeerAssignments
            .Where(pa => pa.EvaluationId == evaluation.EvaluationId)
            .OrderBy(pa => pa.PeerUserId)
            .ToListAsync();
        Assert.Equal(2, assignments.Count);
        Assert.Equal(new[] { 5, 6 }, assignments.Select(a => a.PeerUserId).ToArray());

        var peerReviews = await context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.Peer)
            .OrderBy(r => r.ReviewerUserId)
            .ToListAsync();
        Assert.Equal(2, peerReviews.Count);
        Assert.All(peerReviews, r => Assert.Equal("Pending", r.Status));
        Assert.Equal(new[] { 5, 6 }, peerReviews.Select(r => r.ReviewerUserId).ToArray());
    }

    [Fact]
    public async Task PeerSubmitGoalScoresThenApprove_TransitionsToHodAfterBothPeers()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var reviewScoringService = new ReviewScoringService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        var personalGoalService = new PersonalGoalService(context, workflowService);

        var evaluation = await workflowService.StartEvaluationForGoalSetAsync(employeeId, goalSetId, cycleId);
        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "Approved");

        var goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        foreach (var goal in goals)
        {
            await personalGoalService.StartGoalAsync(goal.Id, employeeId);
            await personalGoalService.CompleteGoalAsync(goal.Id, employeeId, new CompleteGoalRequestDto
            {
                Summary = "Done",
                EvidenceUrl = "https://evidence.example.com",
                Comment = "Completed"
            });
        }

        var rmPostReview = await context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.RM)
            .OrderByDescending(r => r.ReviewId)
            .FirstAsync();
        rmPostReview.Status = "Completed";
        rmPostReview.OverallScore = 8.8m;
        rmPostReview.SubmittedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await workflowService.ApproveAsync(evaluation.EvaluationId, rmId, "RM scored and approved");
        await workflowService.SubmitTlOverallAndAssignPeersAsync(
            evaluation.EvaluationId,
            tlId,
            9.0m,
            "Strong overall performance",
            5,
            6);

        var personalGoalIds = goals.Select(g => g.Id).ToList();

        async Task SubmitPeerScoresAndApproveAsync(int peerUserId, decimal score)
        {
            var peerReview = await context.Reviews
                .Where(r =>
                    r.EvaluationId == evaluation.EvaluationId &&
                    r.ReviewerRole == ReviewerRole.Peer &&
                    r.ReviewerUserId == peerUserId)
                .OrderByDescending(r => r.ReviewId)
                .FirstAsync();

            var dto = new SubmitReviewWithGoalScoresDto
            {
                GoalScores = personalGoalIds
                    .Select(goalId => new ReviewItemScoreDto
                    {
                        PersonalGoalId = goalId,
                        ScoreValue = score,
                        Comment = "Peer score"
                    })
                    .ToList(),
                OverallComment = "Peer completed review"
            };

            await reviewScoringService.SubmitReviewWithGoalScoresAsync(
                evaluation.EvaluationId,
                peerReview.ReviewId,
                peerUserId,
                dto);

            await workflowService.ApproveAsync(
                evaluation.EvaluationId,
                peerUserId,
                "Peer approval");
        }

        // Act
        await SubmitPeerScoresAndApproveAsync(5, 8.2m);

        var statusAfterFirstPeer = await context.Evaluations.FindAsync(evaluation.EvaluationId);
        Assert.NotNull(statusAfterFirstPeer);
        Assert.Equal("Pending_Peer_Reviews", statusAfterFirstPeer!.Status);

        await SubmitPeerScoresAndApproveAsync(6, 8.7m);

        // Assert
        var updatedEvaluation = await context.Evaluations
            .Include(e => e.Reviews)
            .FirstAsync(e => e.EvaluationId == evaluation.EvaluationId);

        Assert.Equal("Pending_HOD_Review", updatedEvaluation.Status);
        Assert.Contains(updatedEvaluation.Reviews, r =>
            r.ReviewerRole == ReviewerRole.HOD &&
            r.Status == "Pending");
    }

    [Fact]
    public async Task HodSubmitScore_RoutesBy85Threshold_InV1()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);
        const int hodId = 4;

        var highEval = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            Status = "Pending_HOD_Review",
            WorkflowVersion = "v1"
        };
        context.Evaluations.Add(highEval);
        await context.SaveChangesAsync();

        context.Reviews.Add(new Review
        {
            EvaluationId = highEval.EvaluationId,
            ReviewerUserId = hodId,
            ReviewerRole = ReviewerRole.HOD,
            Status = "Pending"
        });
        await context.SaveChangesAsync();

        // Act (>=85 route)
        await workflowService.HodSubmitScoreAsync(highEval.EvaluationId, hodId, 8.5m, "High performer");

        // Assert GM route for >=85
        var highUpdated = await context.Evaluations
            .Include(e => e.PromotionCases)
            .FirstAsync(e => e.EvaluationId == highEval.EvaluationId);
        Assert.Equal("Pending_GM_Decision", highUpdated.Status);
        Assert.True(highUpdated.PromotionCases.Any());

        var lowEval = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = Guid.NewGuid(),
            Status = "Pending_HOD_Review",
            WorkflowVersion = "v1"
        };
        context.Evaluations.Add(lowEval);
        await context.SaveChangesAsync();

        context.Reviews.Add(new Review
        {
            EvaluationId = lowEval.EvaluationId,
            ReviewerUserId = hodId,
            ReviewerRole = ReviewerRole.HOD,
            Status = "Pending"
        });
        await context.SaveChangesAsync();

        // Act (<85 route)
        await workflowService.HodSubmitScoreAsync(lowEval.EvaluationId, hodId, 8.4m, "Below threshold");

        // Assert HR route for <85
        var lowUpdated = await context.Evaluations.FindAsync(lowEval.EvaluationId);
        Assert.NotNull(lowUpdated);
        Assert.Equal("Pending_HR_Processing", lowUpdated!.Status);
    }

    [Fact]
    public async Task HrCanFinalizeLowScoreWithoutGmApprovedPromotionCase()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowService = new EvaluationWorkflowService(context, _emailServiceMock.Object, _workflowV2ServiceMock.Object);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            Status = "Pending_HR_Processing",
            WorkflowVersion = "v1",
            OverallScore = 82m
        };

        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        // Act
        await workflowService.FinalizePromotionByHrAsync(evaluation.EvaluationId, 7, proceed: true, comment: "Direct HR low-score handling");

        // Assert
        var updated = await context.Evaluations.FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updated);
        Assert.Equal("Completed_NoPromotion", updated!.Status);
    }

    [Fact]
    public async Task WorkflowV2_HodFinalization_RoutesBy85Threshold()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowV2Service = new WorkflowV2Service(context);
        const int hodId = 4;

        var highEval = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_PENDING_HOD_REVIEW"
        };
        context.Evaluations.Add(highEval);
        await context.SaveChangesAsync();

        context.PeerAssignments.AddRange(
            new PeerAssignment { EvaluationId = highEval.EvaluationId, PeerUserId = 5 },
            new PeerAssignment { EvaluationId = highEval.EvaluationId, PeerUserId = 6 });

        context.Reviews.AddRange(
            new Review { EvaluationId = highEval.EvaluationId, ReviewerUserId = employeeId, ReviewerRole = ReviewerRole.Self, Status = "Completed", OverallScore = 9.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = highEval.EvaluationId, ReviewerUserId = tlId, ReviewerRole = ReviewerRole.TL, Status = "Completed", OverallScore = 9.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = highEval.EvaluationId, ReviewerUserId = rmId, ReviewerRole = ReviewerRole.RM, Status = "Completed", OverallScore = 9.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = highEval.EvaluationId, ReviewerUserId = 5, ReviewerRole = ReviewerRole.Peer, Status = "Completed", OverallScore = 9.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = highEval.EvaluationId, ReviewerUserId = 6, ReviewerRole = ReviewerRole.Peer, Status = "Completed", OverallScore = 9.0m, SubmittedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act (>=85 route)
        await workflowV2Service.HODFinalizeAsync(highEval.EvaluationId, hodId, "High performer");

        // Assert GM route for >=85
        var highUpdated = await context.Evaluations
            .Include(e => e.PromotionCases)
            .FirstAsync(e => e.EvaluationId == highEval.EvaluationId);
        Assert.Equal("V2_PENDING_GM_DECISION", highUpdated.Status);
        Assert.True(highUpdated.PromotionCases.Any());

        var lowEval = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = Guid.NewGuid(),
            WorkflowVersion = "v2",
            Status = "V2_PENDING_HOD_REVIEW"
        };
        context.Evaluations.Add(lowEval);
        await context.SaveChangesAsync();

        context.PeerAssignments.AddRange(
            new PeerAssignment { EvaluationId = lowEval.EvaluationId, PeerUserId = 5 },
            new PeerAssignment { EvaluationId = lowEval.EvaluationId, PeerUserId = 6 });
        context.Reviews.AddRange(
            new Review { EvaluationId = lowEval.EvaluationId, ReviewerUserId = employeeId, ReviewerRole = ReviewerRole.Self, Status = "Completed", OverallScore = 8.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = lowEval.EvaluationId, ReviewerUserId = tlId, ReviewerRole = ReviewerRole.TL, Status = "Completed", OverallScore = 8.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = lowEval.EvaluationId, ReviewerUserId = rmId, ReviewerRole = ReviewerRole.RM, Status = "Completed", OverallScore = 8.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = lowEval.EvaluationId, ReviewerUserId = 5, ReviewerRole = ReviewerRole.Peer, Status = "Completed", OverallScore = 8.0m, SubmittedAt = DateTime.UtcNow },
            new Review { EvaluationId = lowEval.EvaluationId, ReviewerUserId = 6, ReviewerRole = ReviewerRole.Peer, Status = "Completed", OverallScore = 8.0m, SubmittedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act (<85 route)
        await workflowV2Service.HODFinalizeAsync(lowEval.EvaluationId, hodId, "Low performer path");

        // Assert HR low-performer route
        var lowUpdated = await context.Evaluations
            .Include(e => e.PipCases)
            .FirstAsync(e => e.EvaluationId == lowEval.EvaluationId);
        Assert.Equal("V2_PENDING_HR_LOW_PERFORMER", lowUpdated.Status);
        Assert.True(lowUpdated.PipCases.Any());
    }

    [Fact]
    public async Task WorkflowV2_SubmitSelfEvaluation_PersistsCompletionFields()
    {
        // Arrange
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();
        var workflowV2Service = new WorkflowV2Service(context);

        var cycle = await context.Cycles.FirstAsync(c => c.CycleId == cycleId);
        cycle.EndDate = DateTime.UtcNow.AddDays(-1);

        var seedGoal = await context.PersonalGoals.FirstAsync(g => g.GoalSetId == goalSetId);
        var additionalGoals = Enumerable.Range(0, 3)
            .Select(i => new PersonalGoal
            {
                Id = Guid.NewGuid(),
                UserId = employeeId,
                GoalItemId = seedGoal.GoalItemId,
                GoalSetId = goalSetId,
                Title = $"Extra Goal {i + 1}",
                Description = $"Additional goal {i + 1}",
                TargetScore = 100,
                CurrentScore = 0,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                DueDate = DateTime.UtcNow.AddDays(5),
                Status = PersonalGoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        context.PersonalGoals.AddRange(additionalGoals);
        await context.SaveChangesAsync();

        var allGoals = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .OrderBy(g => g.Title)
            .ToListAsync();
        Assert.Equal(5, allGoals.Count);

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = "V2_ACTIVE_GOALS"
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        context.EmployeeGoals.AddRange(allGoals.Select(g => new EmployeeGoal
        {
            EvaluationId = evaluation.EvaluationId,
            PersonalGoalId = g.Id,
            Title = g.Title,
            Description = g.Description ?? string.Empty,
            WeightPct = 20m
        }));
        await context.SaveChangesAsync();

        var request = new SubmitSelfEvaluationV2Dto
        {
            OverallComment = "Overall self-evaluation comment",
            Goals = allGoals.Select((g, index) => new SelfEvaluationGoalInputDto
            {
                PersonalGoalId = g.Id,
                Score = 90 + (index % 2),
                Summary = $"Summary for {g.Title}",
                EvidenceUrl = $"https://evidence.example.com/{index + 1}",
                CertificationUrl = $"https://cert.example.com/{index + 1}",
                Comment = $"Comment for {g.Title}"
            }).ToList()
        };

        // Act
        await workflowV2Service.SubmitSelfEvaluationAsync(evaluation.EvaluationId, employeeId, request);

        // Assert
        var updatedEvaluation = await context.Evaluations.FindAsync(evaluation.EvaluationId);
        Assert.NotNull(updatedEvaluation);
        Assert.Equal("Pending_RM_Review_PostCompletion", updatedEvaluation!.Status);

        var updatedGoals = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .OrderBy(g => g.Title)
            .ToListAsync();

        Assert.All(updatedGoals, g =>
        {
            Assert.Equal(PersonalGoalStatus.UnderEvaluation, g.Status);
            Assert.False(string.IsNullOrWhiteSpace(g.CompletionSummary));
            Assert.False(string.IsNullOrWhiteSpace(g.CompletionEvidenceUrl));
            Assert.False(string.IsNullOrWhiteSpace(g.CompletionCertificationUrl));
        });

        var updatedEmployeeGoals = await context.EmployeeGoals
            .Where(eg => eg.EvaluationId == evaluation.EvaluationId)
            .ToListAsync();
        Assert.Equal(5, updatedEmployeeGoals.Count);
        Assert.All(updatedEmployeeGoals, eg => Assert.False(string.IsNullOrWhiteSpace(eg.EvidenceUri)));

        var rmReview = await context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.RM)
            .OrderByDescending(r => r.ReviewId)
            .FirstOrDefaultAsync();
        Assert.NotNull(rmReview);
        Assert.Equal("Pending", rmReview!.Status);
    }

    private async Task<(EpecpsDbContext context, int employeeId, int rmId, int tlId, Guid goalSetId, int evaluationId, List<GoalAssignment> assignments)>
        SetupWorkflowV2ActivationScenarioAsync(
            string evaluationStatus,
            string assignmentActivationStatus = "PendingEmployee",
            bool withActivationMethods = false)
    {
        var (context, employeeId, rmId, tlId, goalSetId, cycleId) = await SetupTestDataAsync();

        var seedGoal = await context.PersonalGoals.FirstAsync(g => g.GoalSetId == goalSetId);
        var goals = await context.PersonalGoals
            .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
            .ToListAsync();

        if (goals.Count < 5)
        {
            var required = 5 - goals.Count;
            var additionalGoals = Enumerable.Range(0, required)
                .Select(i => new PersonalGoal
                {
                    Id = Guid.NewGuid(),
                    UserId = employeeId,
                    GoalItemId = seedGoal.GoalItemId,
                    GoalSetId = goalSetId,
                    Title = $"Activation Goal {i + 1}",
                    Description = $"Activation test goal {i + 1}",
                    TargetScore = 100,
                    CurrentScore = 0,
                    StartDate = DateTime.UtcNow.AddDays(-7),
                    DueDate = DateTime.UtcNow.AddMonths(3),
                    Status = PersonalGoalStatus.ApprovedByRM,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            context.PersonalGoals.AddRange(additionalGoals);
            await context.SaveChangesAsync();

            goals = await context.PersonalGoals
                .Where(g => g.GoalSetId == goalSetId && g.UserId == employeeId)
                .ToListAsync();
        }

        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = rmId,
            TeamLeadId = tlId,
            GoalSetId = goalSetId,
            WorkflowVersion = "v2",
            Status = evaluationStatus
        };
        context.Evaluations.Add(evaluation);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var assignments = goals.Select((goal, index) => new GoalAssignment
        {
            Id = Guid.NewGuid(),
            AssignedByUserId = rmId,
            AssignedToUserId = employeeId,
            GoalItemId = goal.GoalItemId,
            GoalSetId = goalSetId,
            Title = goal.Title,
            Description = goal.Description,
            TargetScore = goal.TargetScore,
            StartDate = goal.StartDate,
            DueDate = goal.DueDate,
            Status = AssignedGoalStatus.Accepted,
            PersonalGoalId = goal.Id,
            ActivationStatus = assignmentActivationStatus,
            ActivationMethod = withActivationMethods ? $"Method {index + 1}" : null,
            ActivationSubmittedAt = withActivationMethods ? now : null,
            CreatedAt = now
        }).ToList();

        context.GoalAssignments.AddRange(assignments);
        await context.SaveChangesAsync();

        return (context, employeeId, rmId, tlId, goalSetId, evaluation.EvaluationId, assignments);
    }

    [Fact]
    public async Task WorkflowV2_SubmitActivationPlan_MovesToPendingRmActivationReview()
    {
        // Arrange
        var setup = await SetupWorkflowV2ActivationScenarioAsync("V2_PENDING_EMPLOYEE_ACTIVATION");
        var workflowV2Service = new WorkflowV2Service(setup.context);

        var request = new SubmitActivationPlanRequestDto
        {
            Goals = setup.assignments
                .Select((assignment, index) => new GoalActivationMethodDto
                {
                    GoalAssignmentId = assignment.Id,
                    Method = $"Execution method {index + 1}"
                })
                .ToList()
        };

        // Act
        await workflowV2Service.SubmitActivationPlanAsync(setup.goalSetId, setup.employeeId, request);

        // Assert
        var evaluation = await setup.context.Evaluations.FindAsync(setup.evaluationId);
        Assert.NotNull(evaluation);
        Assert.Equal("V2_PENDING_RM_ACTIVATION_REVIEW", evaluation!.Status);

        var assignments = await setup.context.GoalAssignments
            .Where(a => a.GoalSetId == setup.goalSetId && a.AssignedToUserId == setup.employeeId)
            .ToListAsync();

        Assert.All(assignments, assignment =>
        {
            Assert.Equal("PendingRM", assignment.ActivationStatus);
            Assert.False(string.IsNullOrWhiteSpace(assignment.ActivationMethod));
            Assert.NotNull(assignment.ActivationSubmittedAt);
        });

        var rmNotification = await setup.context.Notifications
            .OrderByDescending(n => n.SentAt)
            .FirstOrDefaultAsync(n => n.UserId == setup.rmId);
        Assert.NotNull(rmNotification);
        Assert.Contains("pending RM review", rmNotification!.Subject);
    }

    [Fact]
    public async Task WorkflowV2_AssignedRmCanApproveActivationPlan()
    {
        // Arrange
        var setup = await SetupWorkflowV2ActivationScenarioAsync(
            "V2_PENDING_RM_ACTIVATION_REVIEW",
            assignmentActivationStatus: "PendingRM",
            withActivationMethods: true);
        var workflowV2Service = new WorkflowV2Service(setup.context);

        // Act
        await workflowV2Service.ProcessActivationDecisionAsync(
            setup.evaluationId,
            setup.rmId,
            new ActivationPlanDecisionDto
            {
                Approved = true,
                Comment = "Approved by RM"
            });

        // Assert
        var evaluation = await setup.context.Evaluations.FindAsync(setup.evaluationId);
        Assert.NotNull(evaluation);
        Assert.Equal("V2_ACTIVE_GOALS", evaluation!.Status);

        var assignments = await setup.context.GoalAssignments
            .Where(a => a.GoalSetId == setup.goalSetId && a.AssignedToUserId == setup.employeeId)
            .ToListAsync();

        Assert.All(assignments, assignment =>
        {
            Assert.Equal("Approved", assignment.ActivationStatus);
            Assert.Equal(setup.rmId, assignment.ActivationReviewedByUserId);
            Assert.Equal("Approved by RM", assignment.ActivationTlComment);
            Assert.NotNull(assignment.ActivationReviewedAt);
        });

        var approvalHistory = await setup.context.ApprovalHistories
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync(h => h.EvaluationId == setup.evaluationId);
        Assert.NotNull(approvalHistory);
        Assert.Equal("RM", approvalHistory!.ActorRole);
        Assert.Equal("ActivationApprovedByRM", approvalHistory.Action);
    }

    [Fact]
    public async Task WorkflowV2_TlCannotProcessActivationDecision()
    {
        // Arrange
        var setup = await SetupWorkflowV2ActivationScenarioAsync(
            "V2_PENDING_RM_ACTIVATION_REVIEW",
            assignmentActivationStatus: "PendingRM",
            withActivationMethods: true);
        var workflowV2Service = new WorkflowV2Service(setup.context);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            workflowV2Service.ProcessActivationDecisionAsync(
                setup.evaluationId,
                setup.tlId,
                new ActivationPlanDecisionDto
                {
                    Approved = true
                }));

        Assert.Contains("Only assigned Reporting Manager", ex.Message);
    }

    [Fact]
    public async Task WorkflowV2_RmCanProcessLegacyTlActivationStatus()
    {
        // Arrange
        var setup = await SetupWorkflowV2ActivationScenarioAsync(
            "V2_PENDING_TL_ACTIVATION_REVIEW",
            assignmentActivationStatus: "PendingRM",
            withActivationMethods: true);
        var workflowV2Service = new WorkflowV2Service(setup.context);

        var rejectedGoalIds = setup.assignments.Take(2).Select(a => a.Id).ToList();

        // Act
        await workflowV2Service.ProcessActivationDecisionAsync(
            setup.evaluationId,
            setup.rmId,
            new ActivationPlanDecisionDto
            {
                Approved = false,
                Comment = "Please add more implementation detail.",
                RejectedGoalAssignmentIds = rejectedGoalIds
            });

        // Assert
        var evaluation = await setup.context.Evaluations.FindAsync(setup.evaluationId);
        Assert.NotNull(evaluation);
        Assert.Equal("V2_RETURNED_FOR_ACTIVATION", evaluation!.Status);

        var reloadedAssignments = await setup.context.GoalAssignments
            .Where(a => rejectedGoalIds.Contains(a.Id))
            .ToListAsync();
        Assert.All(reloadedAssignments, assignment =>
        {
            Assert.Equal("Rejected", assignment.ActivationStatus);
            Assert.Equal("Please add more implementation detail.", assignment.ActivationTlComment);
            Assert.Equal(setup.rmId, assignment.ActivationReviewedByUserId);
        });
    }
}
