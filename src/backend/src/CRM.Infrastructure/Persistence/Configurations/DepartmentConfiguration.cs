using CRM.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.NameAr).HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.BusinessHoursId);
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.CreatedAt).IsRequired();
    }
}
