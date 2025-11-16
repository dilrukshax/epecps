using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PersonalGoalActivity entity
/// </summary>
public class PersonalGoalActivityConfiguration : IEntityTypeConfiguration<PersonalGoalActivity>
{
    public void Configure(EntityTypeBuilder<PersonalGoalActivity> builder)
    {
        builder.ToTable("PersonalGoalActivities");

        builder.HasKey(pga => pga.Id);

        builder.Property(pga => pga.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(pga => pga.IsFromTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pga => pga.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ActivityStatus.NotStarted);

        builder.Property(pga => pga.DueDate);

        builder.Property(pga => pga.EvidenceUrl)
            .HasMaxLength(2000);

        builder.Property(pga => pga.EvidenceNotes)
            .HasMaxLength(2000);

        builder.Property(pga => pga.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pga => pga.UpdatedAt);

        // Relationships
        builder.HasOne(pga => pga.PersonalGoal)
            .WithMany(pg => pg.Activities)
            .HasForeignKey(pga => pga.PersonalGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // REMOVED: SuggestedActivity relationship - feature removed
        // SuggestedActivityId column remains for backward compatibility but has no FK

        // Indexes
        builder.HasIndex(pga => pga.PersonalGoalId);
        builder.HasIndex(pga => pga.Status);
        // REMOVED: Index on SuggestedActivityId
    }
}
