using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record ReactivateCustomerCommand(Guid CustomerId) : IRequest;

public class ReactivateCustomerCommandHandler : IRequestHandler<ReactivateCustomerCommand>
{
    private readonly ICustomerRepository _repo;

    public ReactivateCustomerCommandHandler(ICustomerRepository repo) => _repo = repo;

    public async Task Handle(ReactivateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _repo.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.Reactivate();
        await _repo.SaveChangesAsync(ct);
    }
}
