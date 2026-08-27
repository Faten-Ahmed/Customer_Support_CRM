using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record UpdateCustomerCommand(
    Guid CustomerId, string FullName, string? Phone, string? CompanyName)
    : IRequest<CustomerDetailDto>;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDetailDto>
{
    private readonly ICustomerRepository _repo;

    public UpdateCustomerCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerDetailDto> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdWithContactsAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.Update(cmd.FullName, cmd.Phone, cmd.CompanyName);
        await _repo.SaveChangesAsync(ct);

        return MapToDetailDto(customer);
    }

    private static CustomerDetailDto MapToDetailDto(Customer c) =>
        new(c.Id, c.FullName, c.Email, c.Phone, c.CompanyName, c.IsVip, c.IsActive, c.CreatedAt,
            c.Contacts.Select(x => new ContactDto(x.Id, x.Type, x.Value, x.IsPrimary)).ToList());
}
