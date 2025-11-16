using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class TrainingMaterialConfiguration : IEntityTypeConfiguration<TrainingMaterial>
{
    public void Configure(EntityTypeBuilder<TrainingMaterial> builder)
    {
        builder.ToTable("TrainingMaterials");
        builder.HasKey(tm => tm.TrainingMaterialId);
        builder.Property(tm => tm.Title).IsRequired().HasMaxLength(500);
        builder.Property(tm => tm.Link).IsRequired().HasMaxLength(2000);
        builder.Property(tm => tm.Tags).HasMaxLength(1000);
    }
}
