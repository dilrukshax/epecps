using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ScoreCategory entity
/// </summary>
public class ScoreCategoryConfiguration : IEntityTypeConfiguration<ScoreCategory>
{
    public void Configure(EntityTypeBuilder<ScoreCategory> builder)
    {
        builder.ToTable("ScoreCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.WeightPercent)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(c => c.MaxScore)
            .HasPrecision(10, 2);

        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(c => c.Template)
            .WithMany(t => t.Categories)
            .HasForeignKey(c => c.ScoreTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Category)
            .HasForeignKey(i => i.ScoreCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.ScoreTemplateId);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => new { c.ScoreTemplateId, c.DisplayOrder });
    }
}
