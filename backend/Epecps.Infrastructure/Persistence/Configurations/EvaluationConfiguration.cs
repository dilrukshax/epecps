using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Evaluation entity
/// </summary>
public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("Evaluations");

        builder.HasKey(e => e.EvaluationId);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.WorkflowVersion)
            .IsRequired()
            .HasMaxLength(16)
            .HasDefaultValue("v1");

        builder.Property(e => e.OverallScore)
            .HasPrecision(10, 2);
        
        // Add GoalSetId for linking evaluations to specific goal sets
        builder.Property(e => e.GoalSetId)
            .IsRequired(false); // Nullable for backwards compatibility

        // Relationships - Configure multiple relationships to User
        builder.HasOne(e => e.Employee)
            .WithMany(u => u.EvaluationsAsEmployee)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReportingManager)
            .WithMany(u => u.EvaluationsAsReportingManager)
            .HasForeignKey(e => e.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeamLead)
            .WithMany(u => u.EvaluationsAsTeamLead)
            .HasForeignKey(e => e.TeamLeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Cycle)
            .WithMany()
            .HasForeignKey(e => e.CycleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship
        builder.HasOne(e => e.PreviousEvaluation)
            .WithMany(e => e.NextEvaluations)
            .HasForeignKey(e => e.PreviousEvaluationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.CycleId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.WorkflowVersion);
        builder.HasIndex(e => e.GoalSetId); // Index for faster lookups by goal set
    }
}
