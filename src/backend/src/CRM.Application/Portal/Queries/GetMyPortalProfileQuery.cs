using CRM.Application.Portal.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Portal.Queries;

public record GetMyPortalProfileQuery(Guid CustomerId) : IRequest<PortalProfileDto>;

public class GetMyPortalProfileQueryHandler
    : IRequestHandler<GetMyPortalProfileQuery, PortalProfileDto>
{
    private readonly ICustomerRepository _customers;
    public GetMyPortalProfileQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<PortalProfileDto> Handle(
        GetMyPortalProfileQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(query.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        return Map(customer);
    }

    internal static PortalProfileDto Map(Customer c)
        => new(c.Id, c.FullName, c.Email, c.Phone, c.CompanyName, c.Country, c.City);
}
