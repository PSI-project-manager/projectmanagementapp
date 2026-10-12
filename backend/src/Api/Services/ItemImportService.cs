using System.Globalization;
using Api.Data;
using Api.Dtos;
using Api.Imports;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ItemImportService(
    AppDbContext db,
    ProjectService projectService,
    ItemService itemService
)
{
    public async Task<ImportReport> ImportAsync(
        int projectId,
        Stream csv,
        int currentUserId,
        CancellationToken ct = default
    )
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Projectid == projectId, ct);

        // same response as a missing project so we don't leak which ids exist
        if (!projectExists || !await projectService.CanAccessAsync(projectId, currentUserId, ct))
        {
            throw new KeyNotFoundException($"Project '{projectId}' was not found.");
        }

        var report = new ImportReport();

        await foreach (var line in csv.ReadCsvLinesAsync(ct))
        {
            // line 1 is the header
            if (line.LineNumber == 1 || line.IsBlank)
            {
                continue;
            }

            report.Add(await ImportLineAsync(projectId, line, ct));
        }

        return report;
    }

    private async Task<ImportRowResult> ImportLineAsync(
        int projectId,
        CsvLine line,
        CancellationToken ct
    )
    {
        var parts = line.Text.Split(',', 4, StringSplitOptions.TrimEntries);

        if (parts.Length < 4)
        {
            return ImportRowResult.Failed(
                line.LineNumber,
                "Expected 'title,itemTypeId,statusId,description'."
            );
        }

        if (
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var typeId)
        )
        {
            return ImportRowResult.Failed(
                line.LineNumber,
                $"Item type id must be a positive integer, got '{parts[1]}'."
            );
        }

        if (
            !int.TryParse(
                parts[2],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var statusId
            )
        )
        {
            return ImportRowResult.Failed(
                line.LineNumber,
                $"Status id must be a positive integer, got '{parts[2]}'."
            );
        }

        try
        {
            var created = await itemService.CreateAsync(
                new CreateItemRequest(
                    Title: parts[0],
                    Description: parts[3],
                    ProjectId: projectId,
                    ItemTypeId: typeId,
                    StatusId: statusId
                ),
                ct
            );

            return ImportRowResult.Created(line.LineNumber, created);
        }
        catch (ValidationException ex)
        {
            return ImportRowResult.Failed(line.LineNumber, Describe(ex));
        }
        catch (KeyNotFoundException ex)
        {
            return ImportRowResult.Failed(line.LineNumber, ex.Message);
        }
    }

    // ItemService throws plain-message ValidationExceptions (no Errors) for inactive types
    private static string Describe(ValidationException ex) =>
        ex.Errors.Any() ? string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) : ex.Message;
}
