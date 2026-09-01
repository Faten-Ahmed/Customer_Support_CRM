namespace CRM.Domain.Users;

public record AgentCapacityDto(
    Guid AgentId,
    int OpenTicketCount,
    DateTime? LastAssignedAt,
    IReadOnlyList<Guid> SkillCategoryIds);
