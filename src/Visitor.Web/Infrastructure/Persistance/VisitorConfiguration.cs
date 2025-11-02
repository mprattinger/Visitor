using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorTracking.Data.Entities;

namespace VisitorTracking.Data.Entities
{
    public class VisitorConfiguration : IEntityTypeConfiguration<Visitor>
    {
        public void Configure(EntityTypeBuilder<Visitor> entity)
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Company).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VisitorToken).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.VisitorToken).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Name, e.Company, e.CreatedAt });
        }
    }
}
