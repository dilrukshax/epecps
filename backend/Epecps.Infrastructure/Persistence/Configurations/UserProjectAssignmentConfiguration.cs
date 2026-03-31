using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class UserProjectAssignmentConfiguration : IEntityTypeConfiguration<UserProjectAssignment>
{
    public void Configure(EntityTypeBuilder<UserProjectAssignment> builder)
    {
        builder.ToTable("UserProjectAssignments");

        builder.HasKey(x => x.UserProjectAssignmentId);

        builder.Property(x => x.AssignmentRole)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.UserProjectAssignments)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => new { x.UserId, x.ProjectId }).IsUnique();
    }
}
