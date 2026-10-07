using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public static class DevDataSeeder
{
    // makes an admin account so we can log in
    public static void Seed(IServiceProvider services)
    {

        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();


        // create roles - these are the only roles that exist throughout the entire app for now.
        var adminRole = new Role
        {
            Name = "Admin",
            Description = "Full access"
        };
        db.Roles.Add(adminRole);
        db.SaveChanges();

        var contributorRole = new Role
        {
            Name = "Contributor",
            Description = "Allowed to view projects and add new tasks to projects"
        };
        db.Roles.Add(contributorRole);
        db.SaveChanges();

        // create users
        const string testPassword = "Test1234!";

        CreateTestUser(
            db,
            hasher,
            "admin@test.local",
            "Test Admin",
            "TestAdmin123!",
            ["Admin"]
        );

        CreateTestUser(
            db,
            hasher,
            "contributor@test.local",
            "Test Contributor",
            testPassword,
            ["Contributor"]
        );

        CreateTestUser(
            db,
            hasher,
            "admincontributor@test.local",
            "Test Admin Contributor",
            testPassword,
            ["Admin", "Contributor"]
        );
    }

    // creates and activates a user, used for hardcoding admins into the app or testing.
    private static void CreateTestUser(
        AppDbContext db,
        PasswordHasher<User> hasher,
        string email,
        string fullname,
        string password,
        string[] roleNames
    )
    {
        // dont create the user again if the seeder runs on an existing database
        if (db.Users.Any(u => u.Email == email))
        {
            return;
        }

        var user = new User
        {
            Email = email,
            Fullname = fullname,
            Isactive = true
        };
        user.Passwordhash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();

        foreach (var roleName in roleNames)
        {
            // only allow adding already existing roles
            var role = db.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                throw new KeyNotFoundException($"Role '{roleName}' doesn't exist - it must be created before an attempt to assign it.");
            }

            var userRole = new Userrole
            {
                Userid = user.Userid,
                Roleid = role.Roleid
            };
            db.Userroles.Add(userRole);
        }

        db.SaveChanges();
    }
}
