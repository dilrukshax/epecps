using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionCase entity
/// </summary>
public class PromotionCaseConfiguration : IEntityTypeConfiguration<PromotionCase>
{
    public void Configure(EntityTypeBuilder<PromotionCase> builder)
    {
        builder.ToTable("PromotionCases");

        builder.HasKey(pc => pc.PromotionCaseId);

        builder.Property(pc => pc.GmDecision)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(pc => pc.DecisionReason)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(pc => pc.Evaluation)
            .WithMany(e => e.PromotionCases)
            .HasForeignKey(pc => pc.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.RecommendedByHod)
            .WithMany(u => u.PromotionCasesRecommended)
            .HasForeignKey(pc => pc.RecommendedByHodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pc => pc.GmDecidedBy)
            .WithMany(u => u.PromotionCasesDecided)
            .HasForeignKey(pc => pc.GmDecidedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pc => pc.EvaluationId);
        builder.HasIndex(pc => pc.GmDecision);
    }
}
