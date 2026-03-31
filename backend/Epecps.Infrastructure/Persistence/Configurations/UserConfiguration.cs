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

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500);

        builder.Property(u => u.PasswordSetAt);

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.FailedLoginCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockedUntil);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure Department relationship properly
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DeptId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.DeptId);
    }
}
