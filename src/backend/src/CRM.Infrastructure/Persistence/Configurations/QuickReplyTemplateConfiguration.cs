using CRM.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class QuickReplyTemplateConfiguration : IEntityTypeConfiguration<QuickReplyTemplate>
{
    public void Configure(EntityTypeBuilder<QuickReplyTemplate> builder)
    {
        builder.ToTable("QuickReplyTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(t => t.Category).HasMaxLength(100);

        builder.Property(t => t.Scope)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(t => t.CreatedByUserId).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => new { t.Scope, t.CreatedByUserId });
    }
}
