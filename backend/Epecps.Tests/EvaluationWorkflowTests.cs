using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Epecps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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
        
        context.Roles.AddRange(employeeRole, rmRole, tlRole, hodRole);
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
        
        context.Users.AddRange(employee, rm, tl, hod);
        await context.SaveChangesAsync();

        // Assign roles
        context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 }); // Employee
        context.UserRoles.Add(new UserRole { UserId = 2, RoleId = 2 }); // RM
        context.UserRoles.Add(new UserRole { UserId = 3, RoleId = 3 }); // TL
        context.UserRoles.Add(new UserRole { UserId = 4, RoleId = 4 }); // HOD
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
    public async Task EmployeeCompleteAllGoals_TriggersWorkflowContinuationToTL()
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
        Assert.Equal("Pending_TL_Review", lastResult.EvaluationStatus);

        // Verify goals are UnderEvaluation
        goals = await context.PersonalGoals.Where(g => g.GoalSetId == goalSetId).ToListAsync();
        Assert.All(goals, g => Assert.Equal(PersonalGoalStatus.UnderEvaluation, g.Status));

        // Verify TL review was created
        var tlReview = await context.Set<Review>()
            .FirstOrDefaultAsync(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.TL);
        Assert.NotNull(tlReview);
        Assert.Equal("Pending", tlReview.Status);

        // Verify approval history has workflow continuation entry
        var history = await context.Set<ApprovalHistory>()
            .Where(h => h.EvaluationId == evaluation.EvaluationId && h.Action.Contains("WorkflowContinued"))
            .FirstOrDefaultAsync();
        Assert.NotNull(history);
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
}
