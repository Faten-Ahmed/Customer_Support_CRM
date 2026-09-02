namespace CRM.Domain.Tickets;

public interface ITicketFieldDefinitionRepository
{
    Task<TicketFieldDefinition?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketFieldDefinition>> GetActiveAsync(
        Guid? departmentId, Guid? categoryId, CancellationToken ct = default);
    Task AddAsync(TicketFieldDefinition field, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
