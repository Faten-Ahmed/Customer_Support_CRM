using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record UpdateCategoryCommand(
    Guid CategoryId, string? Name, string? NameAr, int? SortOrder)
    : IRequest<CategoryDto>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categories;
    public UpdateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<CategoryDto> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");
        category.Update(cmd.Name, cmd.NameAr, cmd.SortOrder);
        await _categories.SaveChangesAsync(ct);
        return CreateCategoryCommandHandler.Map(category);
    }
}
