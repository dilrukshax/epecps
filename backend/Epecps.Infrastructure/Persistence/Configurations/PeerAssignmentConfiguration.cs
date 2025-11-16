using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PeerAssignment entity
/// </summary>
public class PeerAssignmentConfiguration : IEntityTypeConfiguration<PeerAssignment>
{
    public void Configure(EntityTypeBuilder<PeerAssignment> builder)
    {
        builder.ToTable("PeerAssignments");

        builder.HasKey(pa => pa.PeerAssignmentId);

        // Relationships
        builder.HasOne(pa => pa.Evaluation)
            .WithMany(e => e.PeerAssignments)
            .HasForeignKey(pa => pa.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.PeerUser)
            .WithMany(u => u.PeerAssignments)
            .HasForeignKey(pa => pa.PeerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pa => pa.EvaluationId);
        builder.HasIndex(pa => pa.PeerUserId);
        builder.HasIndex(pa => new { pa.EvaluationId, pa.PeerUserId }).IsUnique();
    }
}
