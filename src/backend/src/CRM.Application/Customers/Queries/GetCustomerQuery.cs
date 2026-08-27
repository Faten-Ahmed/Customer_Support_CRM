using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record GetCustomerQuery(Guid CustomerId) : IRequest<CustomerDetailDto>;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDetailDto>
{
    private readonly ICustomerRepository _repo;

    public GetCustomerQueryHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerDetailDto> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await _repo.FindByIdWithContactsAsync(query.CustomerId, ct);

        if (customer is null || !customer.IsActive)
            throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        var contacts = customer.Contacts
            .Select(c => new ContactDto(c.Id, c.Type, c.Value, c.IsPrimary))
            .ToList();

        return new CustomerDetailDto(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.Phone,
            customer.CompanyName,
            customer.IsVip,
            customer.IsActive,
            customer.CreatedAt,
            contacts);
    }
}
