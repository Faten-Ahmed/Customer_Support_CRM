using CRM.Domain.Categories;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record DeactivateCategoryCommand(Guid CategoryId) : IRequest;

public class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand>
{
    private readonly ICategoryRepository _categories;
    private readonly ITicketRepository _tickets;

    public DeactivateCategoryCommandHandler(
        ICategoryRepository categories, ITicketRepository tickets)
    {
        _categories = categories;
        _tickets = tickets;
    }

    public async Task Handle(DeactivateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");

        int openTickets = await _tickets.CountOpenForCategoryAsync(cmd.CategoryId, ct);
        if (openTickets > 0)
            throw new InvalidOperationException(
                $"Cannot deactivate: {openTickets} open ticket(s) assigned to this category.");

        var children = await _categories.GetChildrenAsync(cmd.CategoryId, ct);
        foreach (var child in children)
            child.Deactivate();

        category.Deactivate();
        await _categories.SaveChangesAsync(ct);
    }
}
