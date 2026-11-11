using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/statuses")]
[Authorize]
public class StatusesController(StatusService statusService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<ActionResult<IEnumerable<StatusDto>>> List(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default
    ) => Ok(await statusService.ListAsync(activeOnly, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StatusDto>> Create(
        [FromBody] CreateStatusRequest request,
        CancellationToken ct = default
    )
    {
        var created = await statusService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StatusDto>> Update(
        int id,
        [FromBody] UpdateStatusRequestBody body,
        CancellationToken ct = default
    )
    {
        var updated = await statusService.UpdateAsync(
            new UpdateStatusRequest(id, body.Name, body.SortOrder, body.IsActive),
            ct
        );
        return Ok(updated);
    }
}

public record UpdateStatusRequestBody(string Name, int SortOrder, bool IsActive);