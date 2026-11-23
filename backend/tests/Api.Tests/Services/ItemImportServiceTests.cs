using System.Text;
using Api.Imports;
using Api.Services;
using Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Services;

public class ItemImportServiceTests
{
    private const int MissingId = 9999;
    private const string Header = "title,itemTypeId,statusId,description";

    private readonly TestDb db = new();
    private readonly int adminId;
    private readonly int memberId;
    private readonly int outsiderId;
    private readonly int projectId;
    private readonly int bugTypeId;
    private readonly int inactiveTypeId;
    private readonly int todoStatusId;
    private readonly int inactiveStatusId;

    public ItemImportServiceTests()
    {
        adminId = db.AddUser("admin@test.local");
        memberId = db.AddUser("member@test.local");
        outsiderId = db.AddUser("outsider@test.local");

        projectId = db.AddProject("Website", adminId, memberId);

        bugTypeId = db.AddItemType("Bug");
        inactiveTypeId = db.AddItemType("Legacy", isActive: false);
        todoStatusId = db.AddStatus("To Do");
        inactiveStatusId = db.AddStatus("Closed", isActive: false);
    }

    private string ValidRow(string title, string description = "Some description") =>
        $"{title},{bugTypeId},{todoStatusId},{description}";

    private static string Csv(params string[] rows) =>
        string.Join("\n", new[] { Header }.Concat(rows));

    // Runs the real ItemImportService from a real DI container (it needs an
    // IServiceScopeFactory because it creates one scope per row).
    private async Task<ImportReport> ImportAsync(
        string csv,
        int targetProjectId,
        int userId,
        bool isAdmin = false
    )
    {
        await using var provider = db.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ItemImportService>();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        return await service.ImportAsync(targetProjectId, stream, userId, isAdmin);
    }

    // ------------------------------------------------------------ happy paths

    [Fact]
    public async Task ImportAsync_ValidRows_CreatesEveryItem()
    {
        var report = await ImportAsync(
            Csv(ValidRow("First"), ValidRow("Second")),
            projectId,
            memberId
        );

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(2, report.CreatedCount);
        Assert.Equal(0, report.FailedCount);

        var rows = report.ToList();
        Assert.All(
            rows,
            row =>
            {
                Assert.Equal(ImportOutcome.Created, row.Outcome);
                Assert.NotNull(row.ItemId);
                Assert.Null(row.Error);
            }
        );
        Assert.Equal(new[] { "First", "Second" }, rows.Select(r => r.Title));
        Assert.Equal(new[] { 2, 3 }, rows.Select(r => r.LineNumber));

        using var context = db.CreateContext();
        var saved = await context.Items.OrderBy(i => i.Title).ToListAsync();
        Assert.Equal(new[] { "First", "Second" }, saved.Select(i => i.Title));
        Assert.All(
            saved,
            item =>
            {
                Assert.Equal(projectId, item.Projectid);
                Assert.Equal(bugTypeId, item.Itemtypeid);
                Assert.Equal(todoStatusId, item.Statusid);
            }
        );
    }

    [Fact]
    public async Task ImportAsync_OnlyHeader_ReturnsEmptyReport()
    {
        var report = await ImportAsync(Csv(), projectId, memberId);

        Assert.Equal(0, report.TotalCount);
    }

    [Fact]
    public async Task ImportAsync_HeaderAndBlankLines_AreSkippedButStillCountedInLineNumbers()
    {
        var csv = Header + "\n\n   \n" + ValidRow("Only one") + "\n";

        var report = await ImportAsync(csv, projectId, memberId);

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Created, row.Outcome);
        Assert.Equal(4, row.LineNumber);
    }

    [Fact]
    public async Task ImportAsync_CommasInDescription_AreKept()
    {
        var report = await ImportAsync(
            Csv(ValidRow("Commas", "one, two, three")),
            projectId,
            memberId
        );

        Assert.Equal(1, report.CreatedCount);
        using var context = db.CreateContext();
        var saved = await context.Items.SingleAsync();
        Assert.Equal("one, two, three", saved.Description);
    }

    // ----------------------------------------------------------- failed rows

    [Fact]
    public async Task ImportAsync_TooFewColumns_FailsTheRow()
    {
        var report = await ImportAsync(Csv("Only title,1,1"), projectId, memberId);

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal("Expected 'title,itemTypeId,statusId,description'.", row.Error);
        Assert.Null(row.ItemId);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("")]
    public async Task ImportAsync_ItemTypeIdIsNotAPositiveInteger_FailsTheRow(string typeId)
    {
        var report = await ImportAsync(
            Csv($"Title,{typeId},{todoStatusId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal($"Item type id must be a positive integer, got '{typeId}'.", row.Error);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("")]
    public async Task ImportAsync_StatusIdIsNotAPositiveInteger_FailsTheRow(string statusId)
    {
        var report = await ImportAsync(
            Csv($"Title,{bugTypeId},{statusId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal($"Status id must be a positive integer, got '{statusId}'.", row.Error);
    }

    [Theory]
    [InlineData(",1,1,Description", "Title is required.")]
    [InlineData("Title,0,1,Description", "Item type is required.")]
    [InlineData("Title,1,0,Description", "Status is required.")]
    [InlineData("Title,1,1,", "Description is required.")]
    public async Task ImportAsync_RowBreaksValidationRules_FailsWithTheValidationMessage(
        string row,
        string expectedError
    )
    {
        var report = await ImportAsync(Csv(row), projectId, memberId);

        var result = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task ImportAsync_UnknownItemType_FailsTheRow()
    {
        var report = await ImportAsync(
            Csv($"Title,{MissingId},{todoStatusId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal($"Item type '{MissingId}' was not found.", row.Error);
    }

    [Fact]
    public async Task ImportAsync_InactiveItemType_FailsTheRow()
    {
        var report = await ImportAsync(
            Csv($"Title,{inactiveTypeId},{todoStatusId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal("Item type 'Legacy' is inactive and cannot be assigned.", row.Error);
    }

    [Fact]
    public async Task ImportAsync_UnknownStatus_FailsTheRow()
    {
        var report = await ImportAsync(
            Csv($"Title,{bugTypeId},{MissingId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal($"Status '{MissingId}' was not found.", row.Error);
    }

    [Fact]
    public async Task ImportAsync_InactiveStatus_FailsTheRow()
    {
        var report = await ImportAsync(
            Csv($"Title,{bugTypeId},{inactiveStatusId},Description"),
            projectId,
            memberId
        );

        var row = Assert.Single(report.ToList());
        Assert.Equal(ImportOutcome.Failed, row.Outcome);
        Assert.Equal("Status 'Closed' is inactive and cannot be assigned.", row.Error);
    }

    [Fact]
    public async Task ImportAsync_FailedRows_AreNotSaved()
    {
        await ImportAsync(
            Csv("broken", $"Title,{MissingId},{todoStatusId},d"),
            projectId,
            memberId
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Items);
    }

    // ------------------------------------------------------ mixed + parallel

    [Fact]
    public async Task ImportAsync_MixOfGoodAndBadRows_ReportsEachRowAndKeepsTheGoodOnes()
    {
        var csv = Csv(
            ValidRow("Good one"),
            "broken row",
            ValidRow("Good two"),
            $"Bad type,{MissingId},{todoStatusId},d"
        );

        var report = await ImportAsync(csv, projectId, memberId);

        Assert.Equal(4, report.TotalCount);
        Assert.Equal(2, report.CreatedCount);
        Assert.Equal(2, report.FailedCount);

        var rows = report.ToList();
        Assert.Equal(new[] { 2, 3, 4, 5 }, rows.Select(r => r.LineNumber));
        Assert.Equal(
            new[]
            {
                ImportOutcome.Created,
                ImportOutcome.Failed,
                ImportOutcome.Created,
                ImportOutcome.Failed,
            },
            rows.Select(r => r.Outcome)
        );

        using var context = db.CreateContext();
        Assert.Equal(2, await context.Items.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_ManyRows_AreAllCreatedAndReportedInLineOrder()
    {
        // the service imports 4 rows at a time, so completion order is not line order;
        // the report must still come back sorted by line number
        var titles = Enumerable.Range(1, 50).Select(n => $"Item {n}").ToArray();
        var csv = Csv(titles.Select(title => ValidRow(title)).ToArray());

        var report = await ImportAsync(csv, projectId, memberId);

        Assert.Equal(50, report.TotalCount);
        Assert.Equal(50, report.CreatedCount);

        var rows = report.ToList();
        Assert.Equal(Enumerable.Range(2, 50), rows.Select(r => r.LineNumber));
        Assert.Equal(titles, rows.Select(r => r.Title));
        Assert.Equal(50, rows.Select(r => r.ItemId).Distinct().Count());

        using var context = db.CreateContext();
        Assert.Equal(50, await context.Items.CountAsync());
    }

    // ----------------------------------------------------------------- access

    [Fact]
    public async Task ImportAsync_MissingProject_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            ImportAsync(Csv(ValidRow("X")), MissingId, adminId, isAdmin: true)
        );

        Assert.Equal($"Project '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task ImportAsync_UserWithoutProjectAccess_ThrowsKeyNotFoundAndImportsNothing()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            ImportAsync(Csv(ValidRow("X")), projectId, outsiderId)
        );

        Assert.Equal($"Project '{projectId}' was not found.", exception.Message);
        using var context = db.CreateContext();
        Assert.Empty(context.Items);
    }

    [Fact]
    public async Task ImportAsync_AdminWhoIsNotAMember_CanImport()
    {
        var report = await ImportAsync(
            Csv(ValidRow("By admin")),
            projectId,
            outsiderId,
            isAdmin: true
        );

        Assert.Equal(1, report.CreatedCount);
    }
}
