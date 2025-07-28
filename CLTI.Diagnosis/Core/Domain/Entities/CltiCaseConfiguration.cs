using CLTI.Diagnosis.Core.Domain.Entities;
using CLTI.Diagnosis.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CLTI.Diagnosis.Data.EntityConfigurations;

public class CltiCaseConfiguration : IEntityTypeConfiguration<CltiCase>
{
    public void Configure(EntityTypeBuilder<CltiCase> builder)
    {
        builder.ToTable("u_clti");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Guid)
            .IsRequired();
            
        builder.Property(e => e.CreatedAt)
            .IsRequired();
            
        builder.HasMany(e => e.Photos)
            .WithOne()
            .HasForeignKey("CltiCaseId")
            .OnDelete(DeleteBehavior.Cascade);

        // Configure value objects
        builder.OwnsOne(e => e.WifiCriteria);
        builder.OwnsOne(e => e.GlassCriteria);
        
        // Add indexes
        builder.HasIndex(e => e.Guid);
        builder.HasIndex(e => e.CreatedAt);
    }
}
