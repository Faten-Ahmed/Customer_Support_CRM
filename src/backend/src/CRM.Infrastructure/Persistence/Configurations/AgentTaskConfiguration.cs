using CRM.Domain.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class AgentTaskConfiguration : IEntityTypeConfiguration<AgentTask>
{
    public void Configure(EntityTypeBuilder<AgentTask> builder)
    {
        builder.ToTable("AgentTasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.AgentId).IsRequired();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).HasColumnType("nvarchar(max)");

        builder.Property(t => t.Priority)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(t => t.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(t => t.DueAt);
        builder.Property(t => t.TicketId);
        builder.Property(t => t.CustomerId);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.Property(t => t.CompletedAt);

        builder.HasIndex(t => new { t.AgentId, t.Status });
    }
}
