using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Visitor.Web.Infrastructure.Persistance.Configurations;

public class VisitorConfiguration : IEntityTypeConfiguration<Features.VisitorManagement.DomainEntities.VisitorEntity>
{
    public void Configure(EntityTypeBuilder<Features.VisitorManagement.DomainEntities.VisitorEntity> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Company)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Status)
            .IsRequired();

        entity.Property(e => e.CreatedAt)
            .IsRequired();


        entity.HasIndex(e => e.Status);

        entity.HasIndex(e => e.CreatedAt);

        entity.HasIndex(e => new { e.Name, e.Company, e.CreatedAt });
    }
}
