using Api.Dtos;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/items/import")]
[Authorize]
public class ItemImportController(ItemImportService importService) : ControllerBase
{
    private const long MaxCsvBytes = 1_000_000;

    [HttpPost]
    [Consumes("text/csv")]
    [RequestSizeLimit(MaxCsvBytes)]
    public async Task<ActionResult<ImportReportDto>> Import(
        int projectId,
        CancellationToken cancellationToken
    )
    {
        var report = await importService.ImportAsync(
            projectId,
            Request.Body,
            User.GetUserId(),
            cancellationToken
        );
        return Ok(ImportReportDto.FromReport(report));
    }
}
