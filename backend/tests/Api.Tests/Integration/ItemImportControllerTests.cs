using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Dtos;
using Api.Models;
using Api.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

public class ItemImportControllerTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Header = "title,itemTypeId,statusId,description";

    private TestUser admin = null!;
    private TestUser member = null!;
    private Project project = null!;
    private Itemtype bug = null!;
    private Status todo = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        admin = await Data.CreateAdminAsync();
        member = await Data.CreateContributorAsync();
        project = await Data.CreateProjectAsync(admin, "Project", member);
        bug = await Data.CreateItemTypeAsync("Bug");
        todo = await Data.CreateStatusAsync("To Do");
    }

    private string Url(int? projectId = null) =>
        $"/api/projects/{projectId ?? project.Projectid}/items/import";

    private static StringContent Csv(params string[] lines) =>
        new(string.Join("\n", lines), Encoding.UTF8, "text/csv");

    private static async Task<ImportReportDto> ReadReportAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ImportReportDto>())!;
    }

    private Task<List<Item>> StoredItemsAsync() =>
        Data.WithDbAsync(db => db.Items.OrderBy(i => i.Title).ToListAsync());

    // auth / request shape

    [Fact]
    public async Task Import_WithoutToken_Returns401()
    {
        var response = await AnonymousClient().PostAsync(Url(), Csv(Header, "A,1,1,d"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithNonCsvContentType_Returns415()
    {
        var response = await ClientFor(member)
            .PostAsync(Url(), new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    // project access

    [Fact]
    public async Task Import_IntoMissingProject_Returns404()
    {
        var response = await ClientFor(admin)
            .PostAsync(Url(999), Csv(Header, $"A,{bug.Itemtypeid},{todo.Statusid},d"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Import_IntoProjectTheUserIsNotIn_Returns404_AndCreatesNothing()
    {
        var outsider = await Data.CreateContributorAsync();

        var response = await ClientFor(outsider)
            .PostAsync(Url(), Csv(Header, $"A,{bug.Itemtypeid},{todo.Statusid},d"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await StoredItemsAsync());
    }

    // happy path

    [Fact]
    public async Task Import_ValidCsv_CreatesEveryRow_SkippingHeaderAndBlankLines()
    {
        var t = bug.Itemtypeid;
        var s = todo.Statusid;

        var report = await ReadReportAsync(
            await ClientFor(member)
                .PostAsync(
                    Url(),
                    Csv(
                        Header,
                        $"Login bug,{t},{s},Users cannot log in",
                        "",
                        $"  Padded  , {t} , {s} ,  spaces around  ",
                        "   ",
                        $"Commas,{t},{s},first, second, third"
                    )
                )
        );

        Assert.Equal(3, report.Total);
        Assert.Equal(3, report.Created);
        Assert.Equal(0, report.Failed);
        // the report lists rows in file order, with the real line numbers (header is line 1)
        Assert.Equal([2, 4, 6], report.Rows.Select(r => r.Line));
        Assert.All(report.Rows, r => Assert.Equal("Created", r.Outcome));
        Assert.All(report.Rows, r => Assert.Null(r.Error));

        var stored = await StoredItemsAsync();
        Assert.Equal(["Commas", "Login bug", "Padded"], stored.Select(i => i.Title));
        Assert.All(stored, i => Assert.Equal(project.Projectid, i.Projectid));
        // everything after the third comma is the description, commas included
        Assert.Equal("first, second, third", stored[0].Description);
        Assert.Equal("spaces around", stored[2].Description);
        // the item ids in the report are the ones actually saved
        Assert.Equal(
            stored.Select(i => i.Itemid).Order(),
            report.Rows.Select(r => r.ItemId!.Value).Order()
        );
    }

    [Fact]
    public async Task Import_ManyRows_CreatesAllOfThemDespiteRunningInParallel()
    {
        const int rowCount = 60;
        var lines = new[] { Header }
            .Concat(
                Enumerable
                    .Range(1, rowCount)
                    .Select(n => $"Item {n:D3},{bug.Itemtypeid},{todo.Statusid},Row {n}")
            )
            .ToArray();

        var report = await ReadReportAsync(await ClientFor(member).PostAsync(Url(), Csv(lines)));

        Assert.Equal(rowCount, report.Created);
        Assert.Equal(Enumerable.Range(2, rowCount), report.Rows.Select(r => r.Line));
        Assert.Equal(rowCount, report.Rows.Select(r => r.ItemId).Distinct().Count());
        // line N holds "Item {N-1}", so every row in the report is matched to its own line
        Assert.All(report.Rows, r => Assert.Equal($"Item {r.Line - 1:D3}", r.Title));
        Assert.Equal(rowCount, (await StoredItemsAsync()).Count);
    }

    // per-row failures

    [Fact]
    public async Task Import_MixedCsv_ReportsEveryFailureOnItsLine_AndKeepsTheGoodRows()
    {
        var t = bug.Itemtypeid;
        var s = todo.Statusid;
        var archived = await Data.CreateStatusAsync("Archived", isActive: false);

        var report = await ReadReportAsync(
            await ClientFor(member)
                .PostAsync(
                    Url(),
                    Csv(
                        Header, //                                       line 1
                        $"Good one,{t},{s},fine", //                     line 2
                        "Too,few", //                                    line 3
                        $"Bad type,abc,{s},d", //                        line 4
                        $"Bad status,{t},-1,d", //                       line 5
                        $"Unknown type,999,{s},d", //                    line 6
                        $"Inactive status,{t},{archived.Statusid},d", // line 7
                        $",{t},{s},no title", //                         line 8
                        $"No description,{t},{s},", //                   line 9
                        "", //                                           line 10
                        $"Good two,{t},{s},fine" //                      line 11
                    )
                )
        );

        Assert.Equal(9, report.Total);
        Assert.Equal(2, report.Created);
        Assert.Equal(7, report.Failed);

        var byLine = report.Rows.ToDictionary(r => r.Line);
        Assert.Equal("Created", byLine[2].Outcome);
        Assert.Equal("Created", byLine[11].Outcome);
        Assert.Equal("Expected 'title,itemTypeId,statusId,description'.", byLine[3].Error);
        Assert.Equal("Item type id must be a positive integer, got 'abc'.", byLine[4].Error);
        Assert.Equal("Status id must be a positive integer, got '-1'.", byLine[5].Error);
        Assert.Equal("Item type '999' was not found.", byLine[6].Error);
        Assert.Equal("Status 'Archived' is inactive and cannot be assigned.", byLine[7].Error);
        Assert.Equal("Title is required.", byLine[8].Error);
        Assert.Equal("Description is required.", byLine[9].Error);
        Assert.All(
            report.Rows.Where(r => r.Outcome == "Failed"),
            r =>
            {
                Assert.Null(r.ItemId);
                Assert.Null(r.Title);
            }
        );

        Assert.Equal(["Good one", "Good two"], (await StoredItemsAsync()).Select(i => i.Title));
    }
}
