using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class PipCaseConfiguration : IEntityTypeConfiguration<PipCase>
{
    public void Configure(EntityTypeBuilder<PipCase> builder)
    {
        builder.ToTable("PipCases");

        builder.HasKey(x => x.PipCaseId);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("Open");

        builder.Property(x => x.Reason)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Evaluation)
            .WithMany(e => e.PipCases)
            .HasForeignKey(x => x.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EmployeeUser)
            .WithMany(u => u.PipCasesAsEmployee)
            .HasForeignKey(x => x.EmployeeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedHrUser)
            .WithMany(u => u.PipCasesAsAssignedHr)
            .HasForeignKey(x => x.AssignedHrUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EvaluationId);
        builder.HasIndex(x => x.EmployeeUserId);
        builder.HasIndex(x => x.AssignedHrUserId);
        builder.HasIndex(x => x.Status);
    }
}

