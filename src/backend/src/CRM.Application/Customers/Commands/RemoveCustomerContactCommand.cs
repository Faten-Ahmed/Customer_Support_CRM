using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record RemoveCustomerContactCommand(Guid CustomerId, Guid ContactId) : IRequest;

public class RemoveCustomerContactCommandHandler : IRequestHandler<RemoveCustomerContactCommand>
{
    private readonly ICustomerRepository _repo;
    private readonly ICustomerContactRepository _contactRepo;

    public RemoveCustomerContactCommandHandler(ICustomerRepository repo, ICustomerContactRepository contactRepo)
    {
        _repo = repo;
        _contactRepo = contactRepo;
    }

    public async Task Handle(RemoveCustomerContactCommand cmd, CancellationToken ct)
    {
        _ = await _repo.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        var contact = await _contactRepo.FindByIdAsync(cmd.ContactId, ct)
            ?? throw new KeyNotFoundException($"Contact {cmd.ContactId} not found.");

        if (contact.CustomerId != cmd.CustomerId)
            throw new InvalidOperationException("Contact does not belong to the specified customer.");

        _contactRepo.Remove(contact);
        await _contactRepo.SaveChangesAsync(ct);
    }
}
