using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

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

        // projected inline rather than through UserDto.FromEntity so EF can translate the
        // hop through userroles into the same statement
        return await query
            .OrderBy(u => u.Fullname)
            .Select(u => new UserDto(
                u.Userid,
                u.Email,
                u.Fullname,
                u.Isactive,
                u.Userroles.Select(ur => ur.Role.Name).ToList()
            ))
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

        // a new user starts with no roles - CreateUserRequest has no roles field
        return UserDto.FromEntity(user, []);
    }

    public async Task<UserDto> UpdateAsync(
        UpdateUserRequest request,
        CancellationToken ct = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var user = await FindAsync(request.UserId, ct);

        var normalizedEmail = NormalizeEmail(request.Email);

        bool emailAlreadyExists = await db.Users.AnyAsync(
            u => u.Userid != request.UserId && u.Email.ToLower() == normalizedEmail,
            ct
        );

        if (emailAlreadyExists)
        {
            throw new ValidationException($"A user with email '{normalizedEmail}' already exists.");
        }

        // ensure all requested role names exist in db
        List<string> existingRoleNames = await db.Roles.Select(r => r.Name).ToListAsync(ct);
        List<string> missing = request.Roles.Except(existingRoleNames).ToList();
        if (missing.Count > 0)
        {
            throw new KeyNotFoundException(
                $"Role(s) '{string.Join("', '", missing)}' do not exist."
            );
        }

        // diff current user roles vs the requested ones to find out which mappings of UserRoles table to add/remove
        var currentUserRoleNames = await db
            .Userroles.Where(ur => ur.Userid == user.Userid)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);
        var roleNamesToAdd = request.Roles.Except(currentUserRoleNames).ToList();
        var roleNamesToRemove = currentUserRoleNames.Except(request.Roles).ToList();

        // update user
        var toRemove = await db
            .Userroles.Where(ur =>
                ur.Userid == user.Userid && roleNamesToRemove.Contains(ur.Role.Name)
            )
            .ToListAsync(ct);
        db.Userroles.RemoveRange(toRemove);

        var idsToAdd = await db
            .Roles.Where(r => roleNamesToAdd.Contains(r.Name))
            .Select(r => r.Roleid)
            .ToListAsync(ct);

        foreach (var roleId in idsToAdd)
        {
            db.Userroles.Add(new Userrole { Userid = user.Userid, Roleid = roleId });
        }

        user.Email = normalizedEmail;
        user.Fullname = request.FullName.Trim();

        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user, request.Roles.Distinct().ToList());
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

        // roles are untouched here, but the dto still has to report them
        var roleNames = await db
            .Userroles.Where(ur => ur.Userid == user.Userid)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);

        return UserDto.FromEntity(user, roleNames);
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
