using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class CustomerCredentialConfiguration : IEntityTypeConfiguration<CustomerCredential>
{
    public void Configure(EntityTypeBuilder<CustomerCredential> builder)
    {
        builder.ToTable("CustomerCredentials");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.CustomerId).IsRequired();
        builder.Property(c => c.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.CustomerId).IsUnique();
    }
}
