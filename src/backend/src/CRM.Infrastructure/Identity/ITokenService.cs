using CRM.Domain.Users;

namespace CRM.Infrastructure.Identity;

public interface ITokenService
{
    string CreateAccessToken(User user);
    (string RawToken, string TokenHash) CreateRefreshToken();
}
