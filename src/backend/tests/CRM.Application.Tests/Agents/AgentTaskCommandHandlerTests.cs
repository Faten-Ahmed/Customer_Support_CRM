using CRM.Application.Agents.Commands;
using CRM.Application.Agents.Queries;
using CRM.Domain.Agents;
using CRM.Domain.Common;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class AgentTaskCommandHandlerTests
{
    private readonly Mock<IAgentTaskRepository> _repo = new();
    private readonly CreateAgentTaskCommandHandler _createHandler;
    private readonly UpdateAgentTaskCommandHandler _updateHandler;
    private readonly DeleteAgentTaskCommandHandler _deleteHandler;
    private readonly ListMyTasksQueryHandler _listHandler;

    public AgentTaskCommandHandlerTests()
    {
        _createHandler = new CreateAgentTaskCommandHandler(_repo.Object);
        _updateHandler = new UpdateAgentTaskCommandHandler(_repo.Object);
        _deleteHandler = new DeleteAgentTaskCommandHandler(_repo.Object);
        _listHandler = new ListMyTasksQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_ValidTask_PersistsIt()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.CountIncompleteAsync(agentId, default)).ReturnsAsync(0);

        var result = await _createHandler.Handle(
            new CreateAgentTaskCommand(
                agentId, "Follow up Sara", "Call at 2pm",
                AgentTaskPriority.High, DateTime.UtcNow.AddDays(1),
                null, null),
            default);

        Assert.Equal("Follow up Sara", result.Title);
        Assert.Equal("Pending", result.Status);
        _repo.Verify(r => r.AddAsync(It.IsAny<AgentTask>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Create_At200TaskLimit_ThrowsValidationException()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.CountIncompleteAsync(agentId, default)).ReturnsAsync(200);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _createHandler.Handle(
                new CreateAgentTaskCommand(
                    agentId, "Task 201", null,
                    AgentTaskPriority.Low, null, null, null),
                default));

        Assert.Contains("MAX_TASKS_REACHED", ex.Message);
    }

    [Fact]
    public async Task Update_OtherAgentTask_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var task = AgentTask.Create(ownerId, "Task", null, AgentTaskPriority.Low, null, null, null);
        _repo.Setup(r => r.FindByIdAsync(task.Id, default)).ReturnsAsync(task);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _updateHandler.Handle(
                new UpdateAgentTaskCommand(task.Id, otherId, null, null, null, null, null),
                default));
    }

    [Fact]
    public async Task Delete_OtherAgentTask_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var task = AgentTask.Create(ownerId, "Task", null, AgentTaskPriority.Low, null, null, null);
        _repo.Setup(r => r.FindByIdAsync(task.Id, default)).ReturnsAsync(task);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _deleteHandler.Handle(
                new DeleteAgentTaskCommand(task.Id, otherId), default));
    }

    [Fact]
    public async Task List_ReturnsTasksForAgent()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(agentId, null, null, null, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<AgentTask>(new List<AgentTask>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListMyTasksQuery(agentId, null, null, null, false, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
