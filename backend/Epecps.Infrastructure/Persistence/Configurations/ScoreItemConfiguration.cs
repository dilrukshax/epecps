using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ScoreItem entity
/// </summary>
public class ScoreItemConfiguration : IEntityTypeConfiguration<ScoreItem>
{
    public void Configure(EntityTypeBuilder<ScoreItem> builder)
    {
        builder.ToTable("ScoreItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .HasMaxLength(1000);

        builder.Property(i => i.ItemType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ScoreItemType.Rating);

        builder.Property(i => i.MaxScore)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(i => i.WeightWithinCategory)
            .HasPrecision(10, 2);

        builder.Property(i => i.IsMandatory)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.EvidenceRequired)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.EvidenceHint)
            .HasMaxLength(500);

        builder.Property(i => i.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.ScoreCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(i => i.ScoreCategoryId);
        builder.HasIndex(i => i.IsActive);
        builder.HasIndex(i => new { i.ScoreCategoryId, i.DisplayOrder });
    }
}
