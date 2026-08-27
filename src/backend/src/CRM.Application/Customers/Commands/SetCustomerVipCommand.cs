using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record SetCustomerVipCommand(Guid CustomerId, bool IsVip) : IRequest;

public class SetCustomerVipCommandHandler : IRequestHandler<SetCustomerVipCommand>
{
    private readonly ICustomerRepository _repo;

    public SetCustomerVipCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task Handle(SetCustomerVipCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.SetVip(cmd.IsVip);
        await _repo.SaveChangesAsync(ct);
    }
}
