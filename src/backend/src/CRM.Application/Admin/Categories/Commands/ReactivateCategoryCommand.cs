using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record ReactivateCategoryCommand(Guid CategoryId) : IRequest;

public class ReactivateCategoryCommandHandler : IRequestHandler<ReactivateCategoryCommand>
{
    private readonly ICategoryRepository _categories;
    public ReactivateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task Handle(ReactivateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");
        category.Reactivate();
        await _categories.SaveChangesAsync(ct);
    }
}
