using CRM.Domain.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.NameAr).HasMaxLength(200);
        builder.Property(b => b.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(b => b.CreatedAt).IsRequired();
    }
}
