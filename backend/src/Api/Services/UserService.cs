using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// User administration (US-08). Restricting these operations to admins isn't wired up yet,
/// so every endpoint is currently open — see <c>UsersController</c>.
/// </summary>
public class UserService(
    AppDbContext db,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    PasswordHasher<User> passwordHasher
)
{
    public async Task<IEnumerable<UserDto>> ListAsync(
        bool activeOnly = false,
        CancellationToken ct = default
    )
    {
        var query = db.Users.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(u => u.Isactive);
        }

        return await query
            .OrderBy(u => u.Fullname)
            .Select(u => UserDto.FromEntity(u))
            .ToListAsync(ct);
    }

    public async Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var normalizedEmail = NormalizeEmail(request.Email);

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (exists)
        {
            throw new ValidationException($"A user with email '{normalizedEmail}' already exists.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            Fullname = request.FullName.Trim(),
            Isactive = true,
        };

        user.Passwordhash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }

    public async Task<UserDto> UpdateAsync(
        UpdateUserRequest request,
        CancellationToken ct = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var user = await FindAsync(request.UserId, ct);

        var normalizedEmail = NormalizeEmail(request.Email);

        var duplicateExists = await db.Users.AnyAsync(
            u => u.Userid != request.UserId && u.Email.ToLower() == normalizedEmail,
            ct
        );

        if (duplicateExists)
        {
            throw new ValidationException($"A user with email '{normalizedEmail}' already exists.");
        }

        user.Email = normalizedEmail;
        user.Fullname = request.FullName.Trim();

        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }

    public async Task<UserDto> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken ct = default
    )
    {
        var user = await FindAsync(userId, ct);

        user.Isactive = isActive;

        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }

    private async Task<User> FindAsync(int userId, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Userid == userId, ct)
        ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found.");

    /// <summary>
    /// Stored lower-cased so the case-sensitive unique index on <c>users.email</c> lines up with
    /// the case-insensitive lookup <see cref="AuthService"/> does at login.
    /// </summary>
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
