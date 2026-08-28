using CRM.Application.Tickets.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class CreateTicketInternalCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly CreateTicketInternalCommandHandler _handler;

    public CreateTicketInternalCommandHandlerTests()
    {
        _handler = new CreateTicketInternalCommandHandler(
            _customerRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTicketWithStatusNew()
    {
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customer = Customer.Create("Ali Hassan", "ali@crm.test", null, null);

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(new CreateTicketInternalCommand(
            customerId, "Cannot login", "User cannot login to portal",
            TicketPriority.High, TicketChannel.Internal, agentId, null, null, null), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("New", result.Status);
        Assert.Equal("Cannot login", result.Subject);
        _ticketRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Once);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketInternalCommand(
                Guid.NewGuid(), "Subj", "Desc",
                TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid(),
                null, null, null), default));
    }

    [Fact]
    public async Task Handle_InactiveCustomer_ThrowsKeyNotFoundException()
    {
        var customer = Customer.Create("Ali Hassan", "ali@crm.test", null, null);
        customer.Deactivate();

        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketInternalCommand(
                Guid.NewGuid(), "Subj", "Desc",
                TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid(),
                null, null, null), default));
    }
}
