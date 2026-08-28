using CRM.Application.Portal.Tickets.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Tickets;

public class CreateTicketPortalCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ICustomerCredentialRepository> _credRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly CreateTicketPortalCommandHandler _handler;

    public CreateTicketPortalCommandHandlerTests()
    {
        _handler = new CreateTicketPortalCommandHandler(
            _customerRepo.Object, _credRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Handle_VerifiedCustomer_CreatesTicketWithPortalChannel()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali Hassan", "ali@portal.test", null, null);
        var cred = CustomerCredential.Create(customerId, "hash");
        cred.VerifyEmail();

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _credRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(cred);

        Ticket? captured = null;
        _ticketRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>(), default))
                   .Callback<Ticket, CancellationToken>((t, _) => captured = t)
                   .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateTicketPortalCommand(
            "My screen is black", "Description here", TicketPriority.Medium,
            null, null, null, customerId), default);

        Assert.NotNull(captured);
        Assert.Equal(TicketChannel.Portal, captured!.Channel);
        Assert.Equal(TicketStatus.New, captured.Status);
    }

    [Fact]
    public async Task Handle_UnverifiedEmail_ThrowsUnauthorizedAccessException()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali Hassan", "ali@portal.test", null, null);
        var cred = CustomerCredential.Create(customerId, "hash"); // Not verified

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _credRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(cred);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new CreateTicketPortalCommand(
                "Subj", "Desc", TicketPriority.Low, null, null, null, customerId), default));
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketPortalCommand(
                "Subj", "Desc", TicketPriority.Low, null, null, null, Guid.NewGuid()), default));
    }
}
