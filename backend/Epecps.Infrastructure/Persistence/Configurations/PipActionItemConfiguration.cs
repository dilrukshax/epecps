using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class PipActionItemConfiguration : IEntityTypeConfiguration<PipActionItem>
{
    public void Configure(EntityTypeBuilder<PipActionItem> builder)
    {
        builder.ToTable("PipActionItems");

        builder.HasKey(x => x.PipActionItemId);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(x => x.Description)
            .HasMaxLength(3000);

        builder.Property(x => x.ExternalTrainingLink)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("Pending");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.PipCase)
            .WithMany(p => p.ActionItems)
            .HasForeignKey(x => x.PipCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TrainingMaterial)
            .WithMany(tm => tm.PipActionItems)
            .HasForeignKey(x => x.TrainingMaterialId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => x.PipCaseId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DueDate);
    }
}

