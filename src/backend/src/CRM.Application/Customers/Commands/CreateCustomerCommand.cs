using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record CreateCustomerCommand(
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName) : IRequest<CustomerDto>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _repo;

    public CreateCustomerCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerDto> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var existing = await _repo.FindByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A customer with email '{cmd.Email}' already exists.");

        var customer = Customer.Create(cmd.FullName, cmd.Email, cmd.Phone, cmd.CompanyName);
        await _repo.AddAsync(customer, ct);
        await _repo.SaveChangesAsync(ct);

        return new CustomerDto(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.Phone,
            customer.CompanyName,
            customer.IsVip,
            customer.IsActive,
            customer.CreatedAt);
    }
}
