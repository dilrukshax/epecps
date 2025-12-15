using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ReviewScoreHistory entity
/// </summary>
public class ReviewScoreHistoryConfiguration : IEntityTypeConfiguration<ReviewScoreHistory>
{
    public void Configure(EntityTypeBuilder<ReviewScoreHistory> builder)
    {
        builder.ToTable("ReviewScoreHistory");

        builder.HasKey(rsh => rsh.Id);

        builder.Property(rsh => rsh.ReviewerRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(rsh => rsh.GoalTitle)
            .HasMaxLength(500);

        builder.Property(rsh => rsh.PreviousScore)
            .HasPrecision(5, 2);

        builder.Property(rsh => rsh.NewScore)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(rsh => rsh.PreviousComment)
            .HasMaxLength(2000);

        builder.Property(rsh => rsh.NewComment)
            .HasMaxLength(2000);

        builder.Property(rsh => rsh.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rsh => rsh.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(rsh => rsh.Review)
            .WithMany()
            .HasForeignKey(rsh => rsh.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rsh => rsh.Evaluation)
            .WithMany()
            .HasForeignKey(rsh => rsh.EvaluationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rsh => rsh.Reviewer)
            .WithMany()
            .HasForeignKey(rsh => rsh.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rsh => rsh.PersonalGoal)
            .WithMany()
            .HasForeignKey(rsh => rsh.PersonalGoalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(rsh => rsh.ReviewId);
        builder.HasIndex(rsh => rsh.EvaluationId);
        builder.HasIndex(rsh => rsh.ReviewerUserId);
        builder.HasIndex(rsh => rsh.CreatedAt);
    }
}
