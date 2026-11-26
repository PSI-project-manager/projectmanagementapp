using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration.Infrastructure;

public record TestUser(int Id, string Email, string FullName, string[] Roles);

// writes rows straight into the database so tests can set up the state they need
// without going through the endpoints under test
public class TestData(ApiFactory factory)
{
    private int counter;

    public Task<TestUser> CreateAdminAsync(string? fullName = null) =>
        CreateUserAsync(["Admin"], fullName);

    public Task<TestUser> CreateContributorAsync(string? fullName = null) =>
        CreateUserAsync(["Contributor"], fullName);

    public async Task<TestUser> CreateUserAsync(string[] roles, string? fullName = null)
    {
        var n = Interlocked.Increment(ref counter);
        var email = $"user{n}@test.local";
        fullName ??= $"User {n}";

        return await WithDbAsync(async db =>
        {
            var user = new User
            {
                Email = email,
                Fullname = fullName,
                Passwordhash = "not-used-by-these-tests",
                Isactive = true,
            };

            foreach (var roleName in roles)
            {
                var role =
                    await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                    ?? db.Roles.Add(new Role { Name = roleName }).Entity;
                user.Userroles.Add(new Userrole { Role = role });
            }

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return new TestUser(user.Userid, email, fullName, roles);
        });
    }

    public Task<Project> CreateProjectAsync(
        TestUser createdBy,
        string? name = null,
        params TestUser[] members
    ) =>
        WithDbAsync(async db =>
        {
            var project = new Project
            {
                Name = name ?? $"Project {Interlocked.Increment(ref counter)}",
                Createdbyuserid = createdBy.Id,
            };

            foreach (var member in members)
            {
                project.Projectusers.Add(new Projectuser { Userid = member.Id });
            }

            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return project;
        });

    public Task<Itemtype> CreateItemTypeAsync(string? name = null, bool isActive = true) =>
        WithDbAsync(async db =>
        {
            var type = new Itemtype
            {
                Name = name ?? $"Type {Interlocked.Increment(ref counter)}",
                Isactive = isActive,
            };
            db.Itemtypes.Add(type);
            await db.SaveChangesAsync();
            return type;
        });

    public Task<Status> CreateStatusAsync(
        string? name = null,
        bool isActive = true,
        int sortOrder = 0
    ) =>
        WithDbAsync(async db =>
        {
            var status = new Status
            {
                Name = name ?? $"Status {Interlocked.Increment(ref counter)}",
                Isactive = isActive,
                Sortorder = sortOrder,
            };
            db.Statuses.Add(status);
            await db.SaveChangesAsync();
            return status;
        });

    public Task<Item> CreateItemAsync(
        int projectId,
        int itemTypeId,
        int statusId,
        string title,
        string? description = "description"
    ) =>
        WithDbAsync(async db =>
        {
            var item = new Item
            {
                Projectid = projectId,
                Itemtypeid = itemTypeId,
                Statusid = statusId,
                Title = title,
                Description = description,
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            return item;
        });

    // reads through a fresh DbContext, so assertions see what was really committed
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }
}
