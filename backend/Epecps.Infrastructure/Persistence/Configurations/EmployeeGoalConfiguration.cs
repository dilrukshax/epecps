using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class EmployeeGoalConfiguration : IEntityTypeConfiguration<EmployeeGoal>
{
    public void Configure(EntityTypeBuilder<EmployeeGoal> builder)
    {
        builder.ToTable("EmployeeGoals");
        builder.HasKey(eg => eg.GoalId);
        builder.Property(eg => eg.Title).IsRequired().HasMaxLength(200);
        builder.Property(eg => eg.Description).IsRequired().HasMaxLength(2000);
        builder.Property(eg => eg.WeightPct).HasPrecision(5, 2);
        builder.Property(eg => eg.EvidenceUri).HasMaxLength(2000);
        
        builder.HasOne(eg => eg.Evaluation)
            .WithMany(e => e.EmployeeGoals)
            .HasForeignKey(eg => eg.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(eg => eg.PersonalGoal)
            .WithMany()
            .HasForeignKey(eg => eg.PersonalGoalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(eg => eg.PersonalGoalId);
    }
}
