using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record CreateCategoryCommand(
    string Name, string? NameAr, Guid? ParentCategoryId, int SortOrder)
    : IRequest<CategoryDto>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categories;
    public CreateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<CategoryDto> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        if (cmd.ParentCategoryId.HasValue)
        {
            bool parentIsChild = await _categories.IsChildCategoryAsync(
                cmd.ParentCategoryId.Value, ct);
            if (parentIsChild)
                throw new InvalidOperationException(
                    "Maximum category depth is 1. A child category cannot have children.");
        }

        var category = TicketCategory.Create(
            cmd.Name, cmd.NameAr, cmd.ParentCategoryId, cmd.SortOrder);
        await _categories.AddAsync(category, ct);
        await _categories.SaveChangesAsync(ct);

        return Map(category);
    }

    internal static CategoryDto Map(TicketCategory c, IReadOnlyList<CategoryDto>? children = null)
        => new(c.Id, c.Name, c.NameAr, c.ParentCategoryId, c.SortOrder, c.IsActive, children);
}
