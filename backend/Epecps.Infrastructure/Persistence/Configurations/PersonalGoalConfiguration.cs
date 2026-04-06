using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PersonalGoal entity
/// </summary>
public class PersonalGoalConfiguration : IEntityTypeConfiguration<PersonalGoal>
{
    public void Configure(EntityTypeBuilder<PersonalGoal> builder)
    {
        builder.ToTable("PersonalGoals");

        builder.HasKey(pg => pg.Id);

        builder.Property(pg => pg.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pg => pg.Description)
            .HasMaxLength(2000);

        builder.Property(pg => pg.TargetScore)
            .IsRequired()
            .HasPrecision(10, 2)
            .HasDefaultValue(100);

        builder.Property(pg => pg.CurrentScore)
            .IsRequired()
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(pg => pg.StartDate)
            .IsRequired();

        builder.Property(pg => pg.DueDate)
            .IsRequired();

        builder.Property(pg => pg.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PersonalGoalStatus.Draft);

        builder.Property(pg => pg.StartedAt);

        builder.Property(pg => pg.CompletedAt);

        builder.Property(pg => pg.CompletionEvidenceUrl)
            .HasMaxLength(2000);

        builder.Property(pg => pg.CompletionCertificationUrl)
            .HasMaxLength(2000);

        builder.Property(pg => pg.CompletionSummary)
            .HasMaxLength(4000);

        builder.Property(pg => pg.CompletionComment)
            .HasMaxLength(2000);

        builder.Property(pg => pg.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pg => pg.UpdatedAt);

        // Relationships
        builder.HasOne(pg => pg.User)
            .WithMany(u => u.PersonalGoals)
            .HasForeignKey(pg => pg.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pg => pg.GoalItem)
            .WithMany(gi => gi.PersonalGoals)
            .HasForeignKey(pg => pg.GoalItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pg => pg.UserId);
        builder.HasIndex(pg => pg.GoalItemId);
        builder.HasIndex(pg => pg.GoalSetId);
        builder.HasIndex(pg => pg.Status);
        builder.HasIndex(pg => new { pg.UserId, pg.Status });
        builder.HasIndex(pg => new { pg.UserId, pg.DueDate });
        builder.HasIndex(pg => new { pg.UserId, pg.GoalSetId });
    }
}
