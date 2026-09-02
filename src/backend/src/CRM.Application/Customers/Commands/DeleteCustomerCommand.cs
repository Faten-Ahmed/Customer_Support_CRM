using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record DeleteCustomerCommand(Guid CustomerId) : IRequest;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;

    public DeleteCustomerCommandHandler(ICustomerRepository customers, ITicketRepository tickets)
    {
        _customers = customers;
        _tickets = tickets;
    }

    public async Task Handle(DeleteCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        if (await _tickets.HasOpenTicketsAsync(cmd.CustomerId, ct))
            throw new InvalidOperationException(
                "Cannot delete a customer with open tickets.");

        customer.Deactivate();
        await _customers.SaveChangesAsync(ct);
    }
}
