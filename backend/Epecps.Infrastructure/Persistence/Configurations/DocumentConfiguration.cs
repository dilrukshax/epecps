using Epecps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epecps.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.Type).HasConversion<int>();
        builder.Property(d => d.Uri).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.Checksum).IsRequired().HasMaxLength(200);
        
        builder.HasOne(d => d.Evaluation)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
