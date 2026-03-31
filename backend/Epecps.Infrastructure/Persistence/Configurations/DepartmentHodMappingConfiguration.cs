using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class DepartmentHodMappingConfiguration : IEntityTypeConfiguration<DepartmentHodMapping>
{
    public void Configure(EntityTypeBuilder<DepartmentHodMapping> builder)
    {
        builder.ToTable("DepartmentHodMappings");

        builder.HasKey(x => new { x.DeptId, x.HodUserId });

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Department)
            .WithMany(d => d.DepartmentHodMappings)
            .HasForeignKey(x => x.DeptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HodUser)
            .WithMany(u => u.DepartmentHodMappings)
            .HasForeignKey(x => x.HodUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.HodUserId);
    }
}

