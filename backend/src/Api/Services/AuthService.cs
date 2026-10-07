using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class AuthService(
    AppDbContext db,
    IValidator<LoginRequest> validator,
    PasswordHasher<User> passwordHasher,
    IConfiguration configuration
)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly string signingKey =
        configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken
        );

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.Passwordhash,
            request.Password
        );

        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        
        if(!user.Isactive)
        {
            throw new UnauthorizedAccessException("User is not activated.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Passwordhash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }
        
        List<string> userRoles= await db.Userroles.Where(ur => ur.Userid == user.Userid).Select(ur => ur.Role.Name).ToListAsync(cancellationToken);

        return new LoginResponse(GenerateToken(user, userRoles), user.Userid, user.Email, user.Fullname);
    }

    private string GenerateToken(User user, IEnumerable<string> userRoles)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256
        );

        var claims = new List<Claim>
        {
          new(JwtRegisteredClaimNames.Sub, user.Userid.ToString()),
          new(JwtRegisteredClaimNames.Email, user.Email),
        };
        claims.AddRange(userRoles.Select(name => new Claim("role", name)));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
