using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ScoreTemplate entity
/// </summary>
public class ScoreTemplateConfiguration : IEntityTypeConfiguration<ScoreTemplate>
{
    public void Configure(EntityTypeBuilder<ScoreTemplate> builder)
    {
        builder.ToTable("ScoreTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        // Relationships
        builder.HasMany(t => t.Categories)
            .WithOne(c => c.Template)
            .HasForeignKey(c => c.ScoreTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.IsPublished);
        builder.HasIndex(t => t.IsArchived);
    }
}
