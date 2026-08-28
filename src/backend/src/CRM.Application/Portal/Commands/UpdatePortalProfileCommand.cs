using CRM.Application.Portal.DTOs;
using CRM.Application.Portal.Queries;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Portal.Commands;

public record UpdatePortalProfileCommand(
    Guid CustomerId, string? FullName, string? Phone, string? City)
    : IRequest<PortalProfileDto>;

public class UpdatePortalProfileCommandHandler
    : IRequestHandler<UpdatePortalProfileCommand, PortalProfileDto>
{
    private readonly ICustomerRepository _customers;
    public UpdatePortalProfileCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<PortalProfileDto> Handle(
        UpdatePortalProfileCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.UpdateProfile(cmd.FullName, cmd.Phone, cmd.City);
        await _customers.SaveChangesAsync(ct);
        return GetMyPortalProfileQueryHandler.Map(customer);
    }
}
