using CRM.Application.Dashboard.Events;
using CRM.Application.Dashboard.Services;
using CRM.Domain.Tickets.Events;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Dashboard;

public class DashboardPushEventHandlerTests
{
    private readonly Mock<IDashboardPusher> _pusher = new();
    private readonly DashboardPushEventHandler _handler;

    public DashboardPushEventHandlerTests()
    {
        _handler = new DashboardPushEventHandler(_pusher.Object);
    }

    [Fact]
    public async Task Handle_TicketCreatedEvent_TriggersDebouncedKpiPush()
    {
        var evt = new TicketCreatedEvent(Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleKpiPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_TicketStatusChangedEvent_TriggersDebouncedKpiPush()
    {
        var evt = new TicketStatusChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Open", "Resolved");

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleKpiPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_TicketAssignedEvent_TriggersWorkloadPush()
    {
        var evt = new TicketAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleWorkloadPushAsync(evt.DepartmentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentStatusChangedEvent_TriggersWorkloadPush()
    {
        var evt = new AgentStatusChangedEvent(Guid.NewGuid(), Guid.NewGuid(), "Available");

        await _handler.Handle(evt, default);

        _pusher.Verify(p => p.ScheduleWorkloadPushAsync(evt.DepartmentId, default), Times.Once);
    }
}
