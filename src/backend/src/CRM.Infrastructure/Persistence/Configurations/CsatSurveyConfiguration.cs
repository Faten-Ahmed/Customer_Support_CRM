using CRM.Domain.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class CsatSurveyConfiguration : IEntityTypeConfiguration<CsatSurvey>
{
    public void Configure(EntityTypeBuilder<CsatSurvey> builder)
    {
        builder.ToTable("CsatSurveys");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.TicketNumber).IsRequired().HasMaxLength(40);
        builder.Property(s => s.TicketSubject).IsRequired().HasMaxLength(500);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Comment).HasMaxLength(1000);

        builder.Property(s => s.SentAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();

        // One survey per ticket
        builder.HasIndex(s => s.TicketId).IsUnique();
    }
}
