using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ReviewScore entity
/// </summary>
public class ReviewScoreConfiguration : IEntityTypeConfiguration<ReviewScore>
{
    public void Configure(EntityTypeBuilder<ReviewScore> builder)
    {
        builder.ToTable("ReviewScores");

        builder.HasKey(rs => rs.Id);

        builder.Property(rs => rs.ScoreValue)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(rs => rs.Comment)
            .HasMaxLength(500);

        builder.Property(rs => rs.CreatedAt)
            .IsRequired();

        builder.Property(rs => rs.UpdatedAt);

        // Relationships
        builder.HasOne(rs => rs.Evaluation)
            .WithMany()
            .HasForeignKey(rs => rs.EvaluationId)
            // SQL Server does not allow multiple cascade paths here because
            // Reviews already cascade from Evaluations and ReviewScores cascade from Reviews.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rs => rs.Review)
            .WithMany(r => r.ReviewScores)
            .HasForeignKey(rs => rs.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rs => rs.Reviewer)
            .WithMany()
            .HasForeignKey(rs => rs.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rs => rs.PersonalGoal)
            .WithMany()
            .HasForeignKey(rs => rs.PersonalGoalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(rs => rs.EvaluationId);
        builder.HasIndex(rs => rs.ReviewId);
        builder.HasIndex(rs => rs.ReviewerId);
        builder.HasIndex(rs => rs.PersonalGoalId);
        builder.HasIndex(rs => new { rs.EvaluationId, rs.ReviewerId });
    }
}
