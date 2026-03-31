using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class WorkflowReviewWeightConfiguration : IEntityTypeConfiguration<WorkflowReviewWeight>
{
    public void Configure(EntityTypeBuilder<WorkflowReviewWeight> builder)
    {
        builder.ToTable("WorkflowReviewWeights");

        builder.HasKey(x => x.WorkflowReviewWeightId);

        builder.Property(x => x.ReviewerKey)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.WeightPercent)
            .IsRequired()
            .HasPrecision(6, 2);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ReviewerKey).IsUnique();
    }
}

