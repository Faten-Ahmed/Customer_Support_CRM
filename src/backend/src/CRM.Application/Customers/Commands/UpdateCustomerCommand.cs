using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record UpdateCustomerCommand(
    Guid CustomerId, string FullName, string FullNameAr, string? Phone, string? CompanyName, string? CompanyNameAr)
    : IRequest<CustomerDetailDto>;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDetailDto>
{
    private readonly ICustomerRepository _repo;

    public UpdateCustomerCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerDetailDto> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdWithContactsAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.Update(cmd.FullName, cmd.FullNameAr, cmd.Phone, cmd.CompanyName, cmd.CompanyNameAr);
        await _repo.SaveChangesAsync(ct);

        return MapToDetailDto(customer);
    }

    private static CustomerDetailDto MapToDetailDto(Customer c) =>
        new(c.Id, c.FullName, c.FullNameAr, c.Email, c.Phone, c.CompanyName, c.CompanyNameAr,
            c.IsVip, c.IsActive, c.CreatedAt,
            c.Contacts.Select(x => new ContactDto(x.Id, x.Type, x.Value, x.IsPrimary)).ToList());
}
