using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(x => x.ProjectId);

        builder.Property(x => x.ProjectCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProjectName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Active");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ProjectCode).IsUnique();
        builder.HasIndex(x => x.ProjectManagerUserId);
        builder.HasIndex(x => x.SupervisorUserId);

        builder.HasOne(x => x.ProjectManagerUser)
            .WithMany()
            .HasForeignKey(x => x.ProjectManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupervisorUser)
            .WithMany()
            .HasForeignKey(x => x.SupervisorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
