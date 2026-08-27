using CRM.Application.Auth.DTOs;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Domain.Customers;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record LoginInternalCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginInternalCommandHandler : IRequestHandler<LoginInternalCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refreshTokens;

    public LoginInternalCommandHandler(
        IUserRepository users, ICustomerRepository customers,
        ITokenService tokens, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _customers = customers;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResponse> Handle(LoginInternalCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct);

        if (user is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is inactive.");

            var accessToken = _tokens.CreateAccessToken(user);
            var (rawToken, tokenHash) = _tokens.CreateRefreshToken();

            var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7));
            await _refreshTokens.AddAsync(refreshToken, ct);
            await _refreshTokens.SaveChangesAsync(ct);

            return new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: rawToken,
                RequiresPasswordChange: user.RequiresPasswordChange,
                UserId: user.Id,
                Email: user.Email,
                FullName: $"{user.FirstName} {user.LastName}",
                Role: user.Role.ToString());
        }

        var customer = await _customers.FindByEmailAsync(cmd.Email, ct);
        if (customer is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(cmd.Password, customer.PasswordHash ?? ""))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!customer.EmailVerified)
            throw new UnauthorizedAccessException("EMAIL_NOT_VERIFIED: Please verify your email address.");

        if (!customer.IsActive)
            throw new UnauthorizedAccessException("ACCOUNT_INACTIVE: Your account has been deactivated.");

        var customerToken = _tokens.CreateAccessToken(customer.Id, customer.Email, "Customer", customer.FullName);
        var (customerRawToken, customerTokenHash) = _tokens.CreateRefreshToken();

        var customerRefreshToken = RefreshToken.Create(customer.Id, customerTokenHash, DateTime.UtcNow.AddDays(7));
        await _refreshTokens.AddAsync(customerRefreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResponse(
            AccessToken: customerToken,
            RefreshToken: customerRawToken,
            RequiresPasswordChange: false,
            UserId: customer.Id,
            Email: customer.Email,
            FullName: customer.FullName,
            Role: "Customer");
    }
}
