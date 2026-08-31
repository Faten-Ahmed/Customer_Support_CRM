using System.Text.Json;
using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class TicketFieldDefinitionConfiguration : IEntityTypeConfiguration<TicketFieldDefinition>
{
    private static readonly JsonSerializerOptions _json = new();

    public void Configure(EntityTypeBuilder<TicketFieldDefinition> builder)
    {
        builder.ToTable("TicketFieldDefinitions");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.DepartmentId).IsRequired();
        builder.Property(f => f.CategoryId);
        builder.Property(f => f.FieldName).IsRequired().HasMaxLength(200);
        builder.Property(f => f.FieldNameAr).HasMaxLength(200);

        builder.Property(f => f.FieldType)
               .IsRequired()
               .HasConversion<int>();

        var listComparer = new ValueComparer<IReadOnlyList<string>?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v == null ? null : (IReadOnlyList<string>)v.ToList());

        builder.Property(f => f.Options)
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, _json),
                   v => v == null ? null : (IReadOnlyList<string>?)JsonSerializer.Deserialize<List<string>>(v, _json),
                   listComparer)
               .HasColumnType("nvarchar(max)");

        builder.Property(f => f.IsRequired).IsRequired();
        builder.Property(f => f.SortOrder).IsRequired().HasDefaultValue(0);
        builder.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(f => f.CreatedAt).IsRequired();
    }
}
