using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Queries;

public record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public class ListCategoriesQueryHandler
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categories;
    public ListCategoriesQueryHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<IReadOnlyList<CategoryDto>> Handle(
        ListCategoriesQuery query, CancellationToken ct)
    {
        var all = await _categories.ListAllAsync(ct);
        var parents = all.Where(c => c.ParentCategoryId == null).ToList();

        return parents.Select(p =>
        {
            var children = all
                .Where(c => c.ParentCategoryId == p.Id)
                .Select(c => CreateCategoryCommandHandler.Map(c))
                .ToList();
            return CreateCategoryCommandHandler.Map(p, children);
        }).ToList();
    }
}
