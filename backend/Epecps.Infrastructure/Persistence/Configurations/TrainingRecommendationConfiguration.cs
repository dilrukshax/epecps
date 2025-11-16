using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class TrainingRecommendationConfiguration : IEntityTypeConfiguration<TrainingRecommendation>
{
    public void Configure(EntityTypeBuilder<TrainingRecommendation> builder)
    {
        builder.ToTable("TrainingRecommendations");
        builder.HasKey(tr => tr.TrainingRecId);
        builder.Property(tr => tr.Reason).HasMaxLength(2000);
        
        builder.HasOne(tr => tr.Evaluation)
            .WithMany(e => e.TrainingRecommendations)
            .HasForeignKey(tr => tr.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(tr => tr.TrainingMaterial)
            .WithMany(tm => tm.TrainingRecommendations)
            .HasForeignKey(tr => tr.TrainingMaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
