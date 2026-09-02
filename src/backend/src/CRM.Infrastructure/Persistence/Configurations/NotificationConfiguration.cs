using CRM.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(n => n.EntityId).IsRequired();
        builder.Property(n => n.IsRead).IsRequired();
        builder.Property(n => n.ReadAt);
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.UserId, n.Type, n.EntityId });
    }
}
