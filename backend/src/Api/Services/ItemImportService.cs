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
    public async Task<ImportResultDto> ImportAsync(
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

        var total = 0;
        var created = 0;
        var errors = new List<string>();

        await foreach (var line in csv.ReadCsvLinesAsync(ct))
        {
            // line 1 is the header
            if (line.LineNumber == 1 || line.IsBlank)
            {
                continue;
            }

            total++;
            var error = await TryImportLineAsync(projectId, line, ct);

            if (error is null)
            {
                created++;
            }
            else
            {
                errors.Add($"Line {line.LineNumber}: {error}");
            }
        }

        return new ImportResultDto(total, created, errors);
    }

    // returns null on success, otherwise the error message
    private async Task<string?> TryImportLineAsync(
        int projectId,
        CsvLine line,
        CancellationToken ct
    )
    {
        var parts = line.Text.Split(',', 4, StringSplitOptions.TrimEntries);

        if (parts.Length < 4)
        {
            return "Expected 'title,itemTypeId,statusId,description'.";
        }

        if (
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var typeId)
        )
        {
            return $"Item type id must be a positive integer, got '{parts[1]}'.";
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
            return $"Status id must be a positive integer, got '{parts[2]}'.";
        }

        try
        {
            await itemService.CreateAsync(
                new CreateItemRequest(
                    Title: parts[0],
                    Description: parts[3],
                    ProjectId: projectId,
                    ItemTypeId: typeId,
                    StatusId: statusId
                ),
                ct
            );

            return null;
        }
        catch (ValidationException ex)
        {
            return Describe(ex);
        }
        catch (KeyNotFoundException ex)
        {
            return ex.Message;
        }
    }

    // ItemService throws plain-message ValidationExceptions (no Errors) for inactive types
    private static string Describe(ValidationException ex) =>
        ex.Errors.Any() ? string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)) : ex.Message;
}
