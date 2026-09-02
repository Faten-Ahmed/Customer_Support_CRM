using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.CustomerId).IsRequired();
        builder.Property(c => c.Type).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Value).IsRequired().HasMaxLength(256);
        builder.Property(c => c.IsPrimary).IsRequired();

        builder.HasIndex(c => c.CustomerId);
    }
}
