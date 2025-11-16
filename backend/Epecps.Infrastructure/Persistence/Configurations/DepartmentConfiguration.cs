using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.DeptId);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        
        // Self-referencing relationship for department hierarchy
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.SubDepartments)
            .HasForeignKey(d => d.ParentDeptId)
            .OnDelete(DeleteBehavior.Restrict);

        // The User relationship is configured from the User side (UserConfiguration)
        // to avoid circular configuration
    }
}
