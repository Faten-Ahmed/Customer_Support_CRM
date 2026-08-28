using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.CompanyName).HasMaxLength(200);
        builder.Property(c => c.PasswordHash).HasMaxLength(500);
        builder.Property(c => c.IsVip).IsRequired();
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.DeletedAt);

        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.IsVip);

        builder.HasMany(c => c.Contacts)
               .WithOne()
               .HasForeignKey("CustomerId")
               .OnDelete(DeleteBehavior.Cascade);
    }
}
