using CRM.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations.Sla;

public class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> builder)
    {
        builder.ToTable("BusinessHours");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TimeZone).HasMaxLength(100).IsRequired();
        builder.Property(b => b.WorkDays)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries),
                new ValueComparer<string[]>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v.ToArray()));

        builder.HasMany(b => b.Holidays)
            .WithOne()
            .HasForeignKey(h => h.BusinessHoursId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("BusinessHourHolidays");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
    }
}
