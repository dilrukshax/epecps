using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable("ReviewItems");
        builder.HasKey(ri => ri.ItemId);
        builder.Property(ri => ri.RatingValue).HasPrecision(10, 2);
        builder.Property(ri => ri.Comment).HasMaxLength(2000);
        
        builder.HasOne(ri => ri.Review)
            .WithMany(r => r.ReviewItems)
            .HasForeignKey(ri => ri.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(ri => ri.Goal)
            .WithMany(g => g.ReviewItems)
            .HasForeignKey(ri => ri.GoalId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(ri => ri.Competency)
            .WithMany(c => c.ReviewItems)
            .HasForeignKey(ri => ri.CompetencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
