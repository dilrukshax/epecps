using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ApprovalHistory entity
/// </summary>
public class ApprovalHistoryConfiguration : IEntityTypeConfiguration<ApprovalHistory>
{
    public void Configure(EntityTypeBuilder<ApprovalHistory> builder)
    {
        builder.ToTable("ApprovalHistories");

        builder.HasKey(ah => ah.Id);

        builder.Property(ah => ah.ActorRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ah => ah.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ah => ah.Comment)
            .HasMaxLength(2000);

        builder.Property(ah => ah.FromStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ah => ah.ToStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ah => ah.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ah => ah.Evaluation)
            .WithMany()
            .HasForeignKey(ah => ah.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ah => ah.Review)
            .WithMany()
            .HasForeignKey(ah => ah.ReviewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ah => ah.ActorUser)
            .WithMany()
            .HasForeignKey(ah => ah.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ah => ah.EvaluationId);
        builder.HasIndex(ah => ah.ReviewId);
        builder.HasIndex(ah => ah.ActorUserId);
        builder.HasIndex(ah => ah.CreatedAt);
    }
}
