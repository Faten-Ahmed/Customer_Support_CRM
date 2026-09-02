using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CRM.API.Tests;

public static class TestJwtHelper
{
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string Secret = "CHANGE_THIS_TO_A_STRONG_SECRET_AT_LEAST_32_CHARS";
    private const string Issuer = "azm-crm-api";
    private const string Audience = "azm-crm-clients";

    public static string CreateTestToken(Guid? userId = null, string role = "Agent")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, (userId ?? TestUserId).ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@crm.test"),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreatePortalCustomerToken(Guid? customerId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, (customerId ?? TestUserId).ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "customer@portal.test"),
            new Claim(ClaimTypes.Role, "Customer"),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
