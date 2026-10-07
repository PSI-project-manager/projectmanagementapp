using Api.Dtos;

namespace Api.Imports;

public sealed record ImportRowResult(
    int LineNumber,
    ImportOutcome Outcome,
    string? Title = null,
    int? ItemId = null,
    string? Error = null
)
{
    public static ImportRowResult Created(int lineNumber, ItemDto item) =>
        new(lineNumber, ImportOutcome.Created, Title: item.Title, ItemId: item.Id);

    public static ImportRowResult Failed(int lineNumber, string error) =>
        new(lineNumber, ImportOutcome.Failed, Error: error);
}
