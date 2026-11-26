using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Support;

/// <summary>
/// One isolated in-memory database. Every test class instance creates its own TestDb
/// (xUnit makes a new instance per test), so tests never see each other's data.
/// Every call to <see cref="CreateContext"/> returns a fresh DbContext over the same
/// data, so what a service saved can be checked without the change tracker's cache.
/// </summary>
public sealed class TestDb
{
    private readonly string databaseName = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot databaseRoot = new();

    public AppDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName, databaseRoot)
                .Options
        );

    /// <summary>Real ItemService with real validators and real helper services.</summary>
    public ItemService CreateItemService()
    {
        var context = CreateContext();

        return new ItemService(
            context,
            new CreateItemRequestValidator(),
            new ItemTypeService(
                context,
                new CreateItemTypeRequestValidator(),
                new UpdateItemTypeRequestValidator()
            ),
            new StatusService(
                context,
                new CreateStatusRequestValidator(),
                new UpdateStatusRequestValidator()
            ),
            new ProjectService(
                context,
                new CreateProjectRequestValidator(),
                new UpdateProjectRequestValidator()
            )
        );
    }

    /// <summary>Real ItemTypeService with real validators.</summary>
    public ItemTypeService CreateItemTypeService() =>
        new(
            CreateContext(),
            new CreateItemTypeRequestValidator(),
            new UpdateItemTypeRequestValidator()
        );

    /// <summary>Real StatusService with real validators.</summary>
    public StatusService CreateStatusService() =>
        new(
            CreateContext(),
            new CreateStatusRequestValidator(),
            new UpdateStatusRequestValidator()
        );

    /// <summary>
    /// Mirrors the registrations in Program.cs. ItemImportService needs a real
    /// IServiceScopeFactory because it opens one scope (and one DbContext) per CSV row.
    /// </summary>
    public ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName, databaseRoot)
        );

        services.AddScoped<IValidator<CreateItemRequest>, CreateItemRequestValidator>();
        services.AddScoped<IValidator<CreateItemTypeRequest>, CreateItemTypeRequestValidator>();
        services.AddScoped<IValidator<UpdateItemTypeRequest>, UpdateItemTypeRequestValidator>();
        services.AddScoped<IValidator<CreateStatusRequest>, CreateStatusRequestValidator>();
        services.AddScoped<IValidator<UpdateStatusRequest>, UpdateStatusRequestValidator>();
        services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
        services.AddScoped<IValidator<UpdateProjectRequest>, UpdateProjectRequestValidator>();

        services.AddScoped<ItemTypeService>();
        services.AddScoped<StatusService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<ItemService>();
        services.AddScoped<ItemImportService>();

        return services.BuildServiceProvider();
    }

    // ---- seeding helpers; each one saves right away and returns the new id ----

    public int AddUser(string email)
    {
        using var context = CreateContext();
        var user = new User
        {
            Email = email,
            Fullname = email,
            Passwordhash = "not-a-real-hash",
            Isactive = true,
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user.Userid;
    }

    public int AddProject(string name, int createdByUserId, params int[] memberUserIds)
    {
        using var context = CreateContext();
        var project = new Project { Name = name, Createdbyuserid = createdByUserId };
        foreach (var memberUserId in memberUserIds)
        {
            project.Projectusers.Add(new Projectuser { Userid = memberUserId });
        }
        context.Projects.Add(project);
        context.SaveChanges();
        return project.Projectid;
    }

    public int AddItemType(string name, bool isActive = true)
    {
        using var context = CreateContext();
        var itemType = new Itemtype { Name = name };
        context.Itemtypes.Add(itemType);
        context.SaveChanges();

        if (!isActive)
        {
            itemType.Isactive = false;
            context.SaveChanges();
        }

        return itemType.Itemtypeid;
    }

    public int AddStatus(string name, bool isActive = true, int sortOrder = 1)
    {
        using var context = CreateContext();
        var status = new Status
        {
            Name = name,
            Sortorder = sortOrder,
            Isactive = true,
        };
        context.Statuses.Add(status);
        context.SaveChanges();

        if (!isActive)
        {
            status.Isactive = false;
            context.SaveChanges();
        }

        return status.Statusid;
    }

    public int AddItem(
        int projectId,
        string title,
        int itemTypeId,
        int statusId,
        string description = "Some description"
    )
    {
        using var context = CreateContext();
        var item = new Item
        {
            Projectid = projectId,
            Title = title,
            Description = description,
            Itemtypeid = itemTypeId,
            Statusid = statusId,
        };
        context.Items.Add(item);
        context.SaveChanges();
        return item.Itemid;
    }
}
