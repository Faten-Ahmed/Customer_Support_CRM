using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.ToTable("TicketMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.Body).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(m => m.IsInternal).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.TicketId);
        builder.HasIndex(m => m.CreatedAt);

        builder.HasOne<CRM.Domain.Tickets.Ticket>()
               .WithMany()
               .HasForeignKey(m => m.TicketId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
