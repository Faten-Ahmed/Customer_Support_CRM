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

    public AddCustomerContactCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<ContactDto> Handle(AddCustomerContactCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdWithContactsAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.AddContact(cmd.Type, cmd.Value, cmd.IsPrimary);
        await _repo.SaveChangesAsync(ct);

        var added = customer.Contacts.Last();
        return new ContactDto(added.Id, added.Type, added.Value, added.IsPrimary);
    }
}
