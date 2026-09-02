using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record AddCustomerContactCommand(
    Guid CustomerId, string Type, string Value, bool IsPrimary)
    : IRequest<ContactDto>;

public class AddCustomerContactCommandHandler : IRequestHandler<AddCustomerContactCommand, ContactDto>
{
    private readonly ICustomerRepository _repo;
    private readonly ICustomerContactRepository _contactRepo;

    public AddCustomerContactCommandHandler(ICustomerRepository repo, ICustomerContactRepository contactRepo)
    {
        _repo = repo;
        _contactRepo = contactRepo;
    }

    public async Task<ContactDto> Handle(AddCustomerContactCommand cmd, CancellationToken ct)
    {
        _ = await _repo.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        if (cmd.IsPrimary)
        {
            var existing = await _contactRepo.FindByCustomerIdAsync(cmd.CustomerId, ct);
            foreach (var c in existing.Where(c => c.Type == cmd.Type && c.IsPrimary))
                c.DemotePrimary();
        }

        var contact = CustomerContact.Create(cmd.CustomerId, cmd.Type, cmd.Value, cmd.IsPrimary);
        await _contactRepo.AddAsync(contact, ct);
        await _contactRepo.SaveChangesAsync(ct);

        return new ContactDto(contact.Id, contact.Type, contact.Value, contact.IsPrimary);
    }
}
