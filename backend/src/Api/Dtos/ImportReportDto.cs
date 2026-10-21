using Api.Imports;

namespace Api.Dtos;

public record ImportReportDto(int Total, int Created, int Failed, IReadOnlyList<ImportRowDto> Rows)
{
    public static ImportReportDto FromReport(ImportReport report)
    {
        var rows = new List<ImportRowDto>();
        foreach (var result in report)
        {
            rows.Add(ImportRowDto.FromResult(result));
        }

        return new ImportReportDto(
            report.TotalCount,
            report.CreatedCount,
            report.FailedCount,
            rows
        );
    }
}
