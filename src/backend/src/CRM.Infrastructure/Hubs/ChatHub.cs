using CRM.Application.Chat.Commands.AcceptHandoff;
using CRM.Application.Chat.Commands.CloseSession;
using CRM.Application.Chat.Commands.SendChatMessage;
using CRM.Application.Chat.Commands.StartChatSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private string CurrentUserName =>
        Context.User!.FindFirst(ClaimTypes.Name)?.Value
        ?? Context.User!.FindFirst("name")?.Value
        ?? "Unknown";

    private bool IsCustomer =>
        Context.User!.FindFirst(ClaimTypes.Role)?.Value == "Customer";

    // Customer calls this to create a new chat session and join it
    public async Task<Guid> StartSession(Guid? departmentId)
    {
        var sessionId = await _mediator.Send(
            new StartChatSessionCommand(CurrentUserId, CurrentUserName, departmentId));

        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroup(sessionId));

        // Notify agents watching this department
        var deptGroup = departmentId.HasValue
            ? DeptGroup(departmentId.Value)
            : "dept-chat-all";

        await Clients.Group(deptGroup).SendAsync("HandoffRequested", new
        {
            SessionId = sessionId,
            CustomerName = CurrentUserName,
            DepartmentId = departmentId,
        });

        return sessionId;
    }

    // Agent calls this to join an existing session group
    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroup(sessionId));
    }

    // Agent subscribes to a department's incoming handoff requests
    public async Task SubscribeToDepartment(Guid? departmentId)
    {
        var group = departmentId.HasValue ? DeptGroup(departmentId.Value) : "dept-chat-all";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    // Agent accepts a handoff
    public async Task AcceptHandoff(Guid sessionId)
    {
        await _mediator.Send(new AcceptHandoffCommand(sessionId, CurrentUserId));
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroup(sessionId));

        await Clients.Group(ChatGroup(sessionId)).SendAsync("HandoffAccepted", new
        {
            SessionId = sessionId,
            AgentName = CurrentUserName,
        });
    }

    // Send a chat message
    public async Task SendMessage(Guid sessionId, string body)
    {
        var role = IsCustomer ? "Customer" : "Agent";
        var dto = await _mediator.Send(
            new SendChatMessageCommand(sessionId, role, CurrentUserId, body));

        await Clients.Group(ChatGroup(sessionId)).SendAsync("ReceiveMessage", dto);
    }

    // Close the session
    public async Task CloseSession(Guid sessionId, string reason = "Closed")
    {
        await _mediator.Send(new CloseSessionCommand(sessionId, reason));

        await Clients.Group(ChatGroup(sessionId)).SendAsync("SessionClosed", new
        {
            SessionId = sessionId,
            Reason = reason,
        });
    }

    // Typing indicators — no DB, just broadcast
    public async Task CustomerTyping(Guid sessionId) =>
        await Clients.OthersInGroup(ChatGroup(sessionId)).SendAsync("CustomerTyping");

    public async Task AgentTyping(Guid sessionId) =>
        await Clients.OthersInGroup(ChatGroup(sessionId)).SendAsync("AgentTyping");

    private static string ChatGroup(Guid sessionId) => $"chat-{sessionId}";
    private static string DeptGroup(Guid deptId) => $"dept-chat-{deptId}";
}
