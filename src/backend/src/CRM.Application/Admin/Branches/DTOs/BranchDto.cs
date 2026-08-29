namespace CRM.Application.Admin.Branches.DTOs;

public record BranchDto(Guid Id, string Name, string? NameAr, bool IsActive, DateTime CreatedAt);
public record BranchActiveResult(Guid Id, bool IsActive);
