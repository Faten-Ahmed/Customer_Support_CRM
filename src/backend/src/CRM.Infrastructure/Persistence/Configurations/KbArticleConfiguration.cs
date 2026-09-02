using CRM.Domain.KnowledgeBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class KbArticleConfiguration : IEntityTypeConfiguration<KbArticle>
{
    public void Configure(EntityTypeBuilder<KbArticle> builder)
    {
        builder.ToTable("KbArticles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(500);
        builder.Property(a => a.TitleAr).HasMaxLength(500);
        builder.Property(a => a.Content).HasColumnType("nvarchar(max)");
        builder.Property(a => a.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Visibility).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.RejectionNote).HasMaxLength(2000);

        builder.HasIndex(a => a.CategoryId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.CreatedByUserId);
    }
}
