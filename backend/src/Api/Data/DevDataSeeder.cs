using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DevDataSeeder
{
    // makes an admin account so we can log in
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();

        // get admin info from appsettings
        string email = config["Seed:AdminEmail"];
        string password = config["Seed:AdminPassword"];
        string name = config["Seed:AdminFullName"];

        // check if admin already exists
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            return;
        }

        // find admin role
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

        // if there is no admin role make one
        if (role == null)
        {
            role = new Role();
            role.Name = "Admin";
            role.Description = "Full access";
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        // make the admin user
        var user = new User();
        user.Email = email;
        user.Fullname = name;
        user.Isactive = true;
        user.Passwordhash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // give the user the admin role
        var userRole = new Userrole();
        userRole.Userid = user.Userid;
        userRole.Roleid = role.Roleid;
        db.Userroles.Add(userRole);
        await db.SaveChangesAsync();
    }
}
