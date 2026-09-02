using CRM.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.Navigation(e => e.Messages).HasField("_messages");

        builder.HasMany(e => e.Messages)
               .WithOne()
               .HasForeignKey(m => m.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatSessionMessageConfiguration : IEntityTypeConfiguration<ChatSessionMessage>
{
    public void Configure(EntityTypeBuilder<ChatSessionMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SenderRole).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(4000).IsRequired();
    }
}
