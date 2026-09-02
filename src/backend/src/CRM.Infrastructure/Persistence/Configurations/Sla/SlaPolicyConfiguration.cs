using CRM.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations.Sla;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FirstResponseMinutes).IsRequired();
        builder.Property(p => p.ResolutionMinutes).IsRequired();
        builder.Property(p => p.WarningThresholdPercent).IsRequired();
        builder.Property(p => p.BreachThresholdPercent).IsRequired();
        builder.Property(p => p.CriticalBreachThresholdPercent).IsRequired();
    }
}
