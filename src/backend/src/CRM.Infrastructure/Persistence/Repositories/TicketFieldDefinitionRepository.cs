using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

public class TicketFieldDefinitionRepository : ITicketFieldDefinitionRepository
{
    public Task<TicketFieldDefinition?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<TicketFieldDefinition?>(null);

    public Task<IReadOnlyList<TicketFieldDefinition>> GetActiveAsync(
        Guid? departmentId, Guid? categoryId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TicketFieldDefinition>>(new List<TicketFieldDefinition>());

    public Task AddAsync(TicketFieldDefinition field, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
