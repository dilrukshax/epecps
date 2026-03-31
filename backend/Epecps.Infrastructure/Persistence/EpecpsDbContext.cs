using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Epecps.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public class EpecpsDbContext : DbContext
{
    public EpecpsDbContext(DbContextOptions<EpecpsDbContext> options) : base(options)
    {
    }

    #region Existing DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserProjectAssignment> UserProjectAssignments => Set<UserProjectAssignment>();
    public DbSet<UserManagerMapping> UserManagerMappings => Set<UserManagerMapping>();
    public DbSet<DepartmentHodMapping> DepartmentHodMappings => Set<DepartmentHodMapping>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkflowReviewWeight> WorkflowReviewWeights => Set<WorkflowReviewWeight>();
    public DbSet<PipCase> PipCases => Set<PipCase>();
    public DbSet<PipActionItem> PipActionItems => Set<PipActionItem>();
    #endregion

    #region Scoring Module DbSets
    public DbSet<ScoreTemplate> ScoreTemplates => Set<ScoreTemplate>();
    public DbSet<ScoreCategory> ScoreCategories => Set<ScoreCategory>();
    public DbSet<ScoreItem> ScoreItems => Set<ScoreItem>();
    #endregion

    #region Employee Goals Module DbSets
    public DbSet<PersonalGoal> PersonalGoals => Set<PersonalGoal>();
    public DbSet<PersonalGoalActivity> PersonalGoalActivities => Set<PersonalGoalActivity>();
    public DbSet<GoalAssignment> GoalAssignments => Set<GoalAssignment>();
    #endregion

    #region Evaluation Module DbSets
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
    public DbSet<EmployeeGoal> EmployeeGoals => Set<EmployeeGoal>();
    public DbSet<PeerAssignment> PeerAssignments => Set<PeerAssignment>();
    public DbSet<PromotionCase> PromotionCases => Set<PromotionCase>();
    public DbSet<TrainingRecommendation> TrainingRecommendations => Set<TrainingRecommendation>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<Competency> Competencies => Set<Competency>();
    public DbSet<Document> Documents => Set<Document>();
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
