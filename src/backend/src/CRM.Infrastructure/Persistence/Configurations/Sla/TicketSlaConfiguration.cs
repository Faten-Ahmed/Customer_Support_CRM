using CRM.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations.Sla;

public class TicketSlaConfiguration : IEntityTypeConfiguration<TicketSla>
{
    public void Configure(EntityTypeBuilder<TicketSla> builder)
    {
        builder.ToTable("TicketSlas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BreachTier).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => s.TicketId).IsUnique();
    }
}
