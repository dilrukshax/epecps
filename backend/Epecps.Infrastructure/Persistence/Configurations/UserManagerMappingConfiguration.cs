using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class UserManagerMappingConfiguration : IEntityTypeConfiguration<UserManagerMapping>
{
    public void Configure(EntityTypeBuilder<UserManagerMapping> builder)
    {
        builder.ToTable("UserManagerMappings");

        builder.HasKey(x => new { x.EmployeeUserId, x.ManagerUserId });

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.EmployeeUser)
            .WithMany(u => u.ManagerMappingsAsEmployee)
            .HasForeignKey(x => x.EmployeeUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ManagerUser)
            .WithMany(u => u.ManagerMappingsAsManager)
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ManagerUserId);
    }
}

