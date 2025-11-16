using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.DeptId)
            .IsRequired();

        // Configure Department relationship properly
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DeptId)
            .OnDelete(DeleteBehavior.Restrict);

        // IGNORE navigation properties that are not yet needed or configured
        builder.Ignore(u => u.UserRoles);
        builder.Ignore(u => u.EvaluationsAsEmployee);
        builder.Ignore(u => u.EvaluationsAsReportingManager);
        builder.Ignore(u => u.EvaluationsAsTeamLead);
        builder.Ignore(u => u.Reviews);
        builder.Ignore(u => u.PeerAssignments);
        builder.Ignore(u => u.PromotionCasesRecommended);
        builder.Ignore(u => u.PromotionCasesDecided);
        builder.Ignore(u => u.Notifications);
        builder.Ignore(u => u.AuditLogs);
        // PersonalGoals is properly configured in PersonalGoalConfiguration

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.DeptId);
    }
}
