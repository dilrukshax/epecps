using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(t => t.TemplateId);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.RoleName).IsRequired().HasMaxLength(100);
        builder.Property(t => t.RatingScaleJson).IsRequired().HasColumnType("nvarchar(max)");
    }
}
