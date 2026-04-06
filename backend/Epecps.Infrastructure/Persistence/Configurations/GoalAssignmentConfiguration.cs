using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class GoalAssignmentConfiguration : IEntityTypeConfiguration<GoalAssignment>
{
    public void Configure(EntityTypeBuilder<GoalAssignment> builder)
    {
        builder.ToTable("GoalAssignments");

        builder.HasKey(ga => ga.Id);

        builder.Property(ga => ga.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ga => ga.Description)
            .HasMaxLength(2000);

        builder.Property(ga => ga.ActivationMethod)
            .HasMaxLength(4000);

        builder.Property(ga => ga.ActivationStatus)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("PendingEmployee");

        builder.Property(ga => ga.ActivationTlComment)
            .HasMaxLength(2000);

        builder.Property(ga => ga.TargetScore)
            .HasColumnType("decimal(18,2)");

        builder.Property(ga => ga.Status)
            .HasConversion<int>();

        builder.HasOne(ga => ga.AssignedByUser)
            .WithMany(u => u.GoalAssignmentsMade)
            .HasForeignKey(ga => ga.AssignedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(ga => ga.AssignedToUser)
            .WithMany(u => u.GoalAssignmentsReceived)
            .HasForeignKey(ga => ga.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(ga => ga.GoalItem)
            .WithMany()
            .HasForeignKey(ga => ga.GoalItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(ga => ga.PersonalGoal)
            .WithMany()
            .HasForeignKey(ga => ga.PersonalGoalId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(ga => ga.ActivationReviewedByUser)
            .WithMany(u => u.GoalAssignmentsActivationReviewed)
            .HasForeignKey(ga => ga.ActivationReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasIndex(ga => ga.AssignedToUserId);
        builder.HasIndex(ga => ga.AssignedByUserId);
        builder.HasIndex(ga => ga.GoalSetId);
        builder.HasIndex(ga => new { ga.GoalSetId, ga.ActivationStatus });
    }
}
