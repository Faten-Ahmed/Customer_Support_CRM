using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdateAvailabilityCommand(
    Guid AgentId,
    AvailabilityStatus Status) : IRequest<AvailabilityResult>;

public record AvailabilityResult(
    Guid Id,
    string AvailabilityStatus,
    DateTime? LastAvailabilityChange);

public class UpdateAvailabilityCommandHandler
    : IRequestHandler<UpdateAvailabilityCommand, AvailabilityResult>
{
    private readonly IUserRepository _users;

    public UpdateAvailabilityCommandHandler(IUserRepository users) => _users = users;

    public async Task<AvailabilityResult> Handle(
        UpdateAvailabilityCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.AgentId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.AgentId} not found.");

        user.SetAvailability(cmd.Status);
        await _users.SaveChangesAsync(ct);

        return new AvailabilityResult(
            user.Id,
            user.AvailabilityStatus.ToString(),
            user.LastAvailabilityChange);
    }
}
