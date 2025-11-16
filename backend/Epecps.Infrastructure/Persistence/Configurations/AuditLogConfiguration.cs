using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AuditLog entity
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.AuditId);

        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.BeforeJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.AfterJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(al => al.ActorUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(al => al.ActorUserId);
        builder.HasIndex(al => al.EntityType);
        builder.HasIndex(al => al.CreatedAt);
        builder.HasIndex(al => new { al.EntityType, al.EntityId });
    }
}
