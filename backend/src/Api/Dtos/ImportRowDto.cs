using Api.Imports;

namespace Api.Dtos;

public record ImportRowDto(int Line, string Outcome, string? Title, int? ItemId, string? Error)
{
    public static ImportRowDto FromResult(ImportRowResult result) =>
        new(
            result.LineNumber,
            result.Outcome.ToString(),
            result.Title,
            result.ItemId,
            result.Error
        );
}
