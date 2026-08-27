using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record RemoveCustomerContactCommand(Guid CustomerId, Guid ContactId) : IRequest;

public class RemoveCustomerContactCommandHandler : IRequestHandler<RemoveCustomerContactCommand>
{
    private readonly ICustomerRepository _repo;

    public RemoveCustomerContactCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task Handle(RemoveCustomerContactCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdWithContactsAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.RemoveContact(cmd.ContactId);
        await _repo.SaveChangesAsync(ct);
    }
}
