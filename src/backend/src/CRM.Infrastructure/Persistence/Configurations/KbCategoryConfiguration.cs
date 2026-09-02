using CRM.Domain.KnowledgeBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class KbCategoryConfiguration : IEntityTypeConfiguration<KbCategory>
{
    public void Configure(EntityTypeBuilder<KbCategory> builder)
    {
        builder.ToTable("KbCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
    }
}
