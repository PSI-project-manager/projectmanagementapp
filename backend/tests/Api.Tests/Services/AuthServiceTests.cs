using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Tests.Support;
using Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Api.Tests.Services;

public class AuthServiceTests
{
    private const string AdminEmail = "admin@test.local";
    private const string WrongPassword = "WrongPassword1!";

    private static readonly DateTimeOffset Now = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly TestDb db = new();

    private AuthService CreateService() => db.CreateAuthService(new FixedTimeProvider(Now));

    private static JwtSecurityToken Read(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static List<string> Roles(JwtSecurityToken token) =>
        token.Claims.Where(c => c.Type == "role").Select(c => c.Value).Order().ToList();

    // ------------------------------------------------------------ happy paths

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUserDetailsAndToken()
    {
        var id = db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        Assert.Equal(id, response.UserId);
        Assert.Equal(AdminEmail, response.Email);
        Assert.Equal("Test Admin", response.FullName);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task LoginAsync_EmailIsMatchedIgnoringCase()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest("ADMIN@TEST.LOCAL", TestDbUserExtensions.DefaultPassword));

        Assert.Equal(AdminEmail, response.Email);
    }

    [Fact]
    public async Task LoginAsync_Token_ContainsSubjectEmailAndEveryRole()
    {
        var id = db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin", "Contributor"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));
        var token = Read(response.Token);

        Assert.Equal(id.ToString(), token.Subject);
        Assert.Equal(AdminEmail, token.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal(new[] { "Admin", "Contributor" }, Roles(token));
    }

    [Fact]
    public async Task LoginAsync_UserWithoutRoles_GetsATokenWithNoRoleClaims()
    {
        db.AddUserWithRoles(AdminEmail, "No Roles", []);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        Assert.Empty(Roles(Read(response.Token)));
    }

    [Fact]
    public async Task LoginAsync_Token_OnlyContainsTheLoggingInUsersRoles()
    {
        db.AddUserWithRoles("other@test.local", "Other", ["Admin"]);
        db.AddUserWithRoles(AdminEmail, "Contributor", ["Contributor"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        Assert.Equal(new[] { "Contributor" }, Roles(Read(response.Token)));
    }

    [Fact]
    public async Task LoginAsync_Token_ExpiresEightHoursAfterNow()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        Assert.Equal(Now.AddHours(8).UtcDateTime, Read(response.Token).ValidTo);
    }

    [Fact]
    public async Task LoginAsync_Token_IsSignedWithTheConfiguredKey()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        TokenValidationParameters ParametersFor(string key) =>
            new()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // the fixed clock is not the real clock
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            };

        var handler = new JwtSecurityTokenHandler();

        handler.ValidateToken(
            response.Token,
            ParametersFor(TestDbUserExtensions.SigningKey),
            out var validated
        );
        Assert.NotNull(validated);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                response.Token,
                ParametersFor("a-completely-different-signing-key-of-sufficient-length"),
                out _
            )
        );
    }

    // ---------------------------------------------------------- failed logins

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().LoginAsync(new LoginRequest(AdminEmail, WrongPassword))
        );

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsTheSameErrorAsAWrongPassword()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService()
                .LoginAsync(
                    new LoginRequest("nobody@test.local", TestDbUserExtensions.DefaultPassword)
                )
        );

        // identical message, so the response doesn't reveal which emails exist
        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUserWithCorrectPassword_ThrowsNotActivated()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"], isActive: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService()
                .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword))
        );

        Assert.Equal("User is not activated.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUserWithWrongPassword_DoesNotRevealThatTheAccountIsInactive()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"], isActive: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().LoginAsync(new LoginRequest(AdminEmail, WrongPassword))
        );

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("admin@test.local", "short")]
    [InlineData("admin@test.local", "")]
    public async Task LoginAsync_InvalidRequest_ThrowsValidationException(
        string email,
        string password
    )
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().LoginAsync(new LoginRequest(email, password))
        );
    }

    // ---------------------------------------------------------------- rehash

    [Fact]
    public async Task LoginAsync_HashFromAnOlderWorkFactor_IsUpgradedAndStillWorks()
    {
        // hash with far fewer iterations than the service's default hasher uses,
        // which makes VerifyHashedPassword answer SuccessRehashNeeded
        var weakHasher = new PasswordHasher<User>(
            Options.Create(new PasswordHasherOptions { IterationCount = 1000 })
        );
        db.AddUserWithRoles(
            AdminEmail,
            "Test Admin",
            ["Admin"],
            password: TestDbUserExtensions.DefaultPassword,
            hasher: weakHasher
        );

        string oldHash;
        using (var before = db.CreateContext())
        {
            oldHash = (await before.Users.SingleAsync()).Passwordhash;
        }

        var response = await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        using var after = db.CreateContext();
        var saved = await after.Users.SingleAsync();
        Assert.NotEqual(oldHash, saved.Passwordhash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(
                saved,
                saved.Passwordhash,
                TestDbUserExtensions.DefaultPassword
            )
        );
    }

    [Fact]
    public async Task LoginAsync_UpToDateHash_IsLeftUntouched()
    {
        db.AddUserWithRoles(AdminEmail, "Test Admin", ["Admin"]);

        string oldHash;
        using (var before = db.CreateContext())
        {
            oldHash = (await before.Users.SingleAsync()).Passwordhash;
        }

        await CreateService()
            .LoginAsync(new LoginRequest(AdminEmail, TestDbUserExtensions.DefaultPassword));

        using var after = db.CreateContext();
        Assert.Equal(oldHash, (await after.Users.SingleAsync()).Passwordhash);
    }

    // ----------------------------------------------------------- configuration

    [Fact]
    public void Constructor_MissingSigningKey_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new AuthService(
                db.CreateContext(),
                new LoginRequestValidator(),
                new PasswordHasher<User>(),
                TestDbUserExtensions.BuildConfiguration(signingKey: null),
                TimeProvider.System
            )
        );

        Assert.Equal("Jwt:SigningKey is not configured.", exception.Message);
    }
}
