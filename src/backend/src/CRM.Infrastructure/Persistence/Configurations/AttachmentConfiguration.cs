using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(500);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(a => a.FileSize).IsRequired();
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.UploadedAt).IsRequired();

        builder.HasIndex(a => a.TicketId);

        builder.HasOne<CRM.Domain.Tickets.Ticket>()
               .WithMany()
               .HasForeignKey(a => a.TicketId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
