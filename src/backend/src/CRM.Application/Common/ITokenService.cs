using CRM.Domain.Users;

namespace CRM.Application.Common;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateAccessToken(Guid id, string email, string role, string fullName = "");
    (string RawToken, string TokenHash) CreateRefreshToken();
}
