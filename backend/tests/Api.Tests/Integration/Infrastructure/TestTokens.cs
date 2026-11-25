using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests.Integration.Infrastructure;

// builds tokens the same shape AuthService does ("sub" + an array "role" claim),
// signed with the key the test host is configured with
public static class TestTokens
{
    public static string For(TestUser user) => Create(user.Id.ToString(), user.Roles);

    public static string Create(string subject, IEnumerable<string> roles)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.SigningKey)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, subject)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        token.Payload["role"] = roles.ToArray();

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
