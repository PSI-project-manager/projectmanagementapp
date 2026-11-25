using Api.Models;
using Api.Services;
using Api.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Api.Tests.Support;

/// <summary>
/// User / role / auth helpers layered on top of <see cref="TestDb"/> so TestDb itself stays untouched.
/// </summary>
public static class TestDbUserExtensions
{
    public const string DefaultPassword = "Password123!";

    // HS256 needs a key of at least 128 bits; this one is comfortably longer.
    public const string SigningKey = "unit-test-signing-key-which-is-long-enough-for-hmac-sha256";

    public static UserService CreateUserService(this TestDb db) =>
        new(
            db.CreateContext(),
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            new PasswordHasher<User>()
        );

    public static AuthService CreateAuthService(
        this TestDb db,
        TimeProvider? timeProvider = null
    ) =>
        new(
            db.CreateContext(),
            new LoginRequestValidator(),
            new PasswordHasher<User>(),
            BuildConfiguration(SigningKey),
            timeProvider ?? TimeProvider.System
        );

    public static IConfiguration BuildConfiguration(string? signingKey)
    {
        var values = new Dictionary<string, string?>();
        if (signingKey is not null)
        {
            values["Jwt:SigningKey"] = signingKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    /// <summary>Creates the role if it does not exist yet and returns its id.</summary>
    public static int AddRole(this TestDb db, string name)
    {
        using var context = db.CreateContext();

        var existing = context.Roles.FirstOrDefault(r => r.Name == name);
        if (existing is not null)
        {
            return existing.Roleid;
        }

        var role = new Role { Name = name };
        context.Roles.Add(role);
        context.SaveChanges();
        return role.Roleid;
    }

    /// <summary>
    /// Seeds a user with a real password hash and the given roles (created on demand).
    /// The email is stored exactly as given. Inactive users are flipped after the first save
    /// because the in-memory provider replaces a CLR-default <c>false</c> with the column default.
    /// </summary>
    public static int AddUserWithRoles(
        this TestDb db,
        string email,
        string fullName,
        string[] roleNames,
        bool isActive = true,
        string password = DefaultPassword,
        PasswordHasher<User>? hasher = null
    )
    {
        var roleIds = roleNames.Select(name => db.AddRole(name)).ToList();

        using var context = db.CreateContext();
        var user = new User
        {
            Email = email,
            Fullname = fullName,
            Isactive = true,
        };
        user.Passwordhash = (hasher ?? new PasswordHasher<User>()).HashPassword(user, password);
        context.Users.Add(user);
        context.SaveChanges();

        foreach (var roleId in roleIds)
        {
            context.Userroles.Add(new Userrole { Userid = user.Userid, Roleid = roleId });
        }
        context.SaveChanges();

        if (!isActive)
        {
            user.Isactive = false;
            context.SaveChanges();
        }

        return user.Userid;
    }

    public static List<string> GetRoleNames(this TestDb db, int userId)
    {
        using var context = db.CreateContext();
        return context
            .Userroles.Where(ur => ur.Userid == userId)
            .Select(ur => ur.Role.Name)
            .OrderBy(name => name)
            .ToList();
    }
}

/// <summary>A clock that always returns the same instant, so token lifetimes can be asserted exactly.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
