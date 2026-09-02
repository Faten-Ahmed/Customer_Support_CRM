using CRM.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("TicketCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameAr).HasMaxLength(200);
        builder.Property(c => c.SortOrder).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne<TicketCategory>()
               .WithMany()
               .HasForeignKey(c => c.ParentCategoryId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
