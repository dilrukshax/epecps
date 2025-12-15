using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Review entity
/// </summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.ReviewId);

        builder.Property(r => r.ReviewerRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.OverallComment)
            .HasMaxLength(2000);

        builder.Property(r => r.OverallScore)
            .HasPrecision(5, 2);

        // Relationships
        builder.HasOne(r => r.Evaluation)
            .WithMany(e => e.Reviews)
            .HasForeignKey(r => r.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => r.EvaluationId);
        builder.HasIndex(r => r.ReviewerUserId);
        builder.HasIndex(r => r.Status);
    }
}
