using CRM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.FirstNameAr).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastNameAr).IsRequired().HasMaxLength(100);
        builder.Property(u => u.JobTitle).HasMaxLength(200);
        builder.Property(u => u.JobTitleAr).HasMaxLength(200);

        builder.Property(u => u.Role)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(u => u.AvailabilityStatus)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.RequiresPasswordChange).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.OwnsMany(u => u.Departments, ud =>
        {
            ud.ToTable("UserDepartments");
            ud.HasKey(x => new { x.UserId, x.DepartmentId });
            ud.Property(x => x.IsPrimary).IsRequired().HasDefaultValue(false);
        });

        builder.OwnsMany(u => u.Skills, us =>
        {
            us.ToTable("UserSkills");
            us.HasKey(x => new { x.UserId, x.CategoryId });
        });
    }
}
