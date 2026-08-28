using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.TicketNumber).IsRequired().HasMaxLength(40);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.CustomFieldValues).HasColumnType("nvarchar(max)");

        builder.Property(t => t.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(t => t.Priority)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(t => t.Channel)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.Property(t => t.ResolvedAt);
        builder.Property(t => t.ClosedAt);

        builder.HasIndex(t => t.TicketNumber).IsUnique();
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.AssignedToUserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);

        builder.Navigation(t => t.History).HasField("_history");

        builder.HasOne(t => t.Customer)
               .WithMany()
               .HasForeignKey(t => t.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedTo)
               .WithMany()
               .HasForeignKey(t => t.AssignedToUserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.History)
               .WithOne()
               .HasForeignKey(h => h.TicketId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
