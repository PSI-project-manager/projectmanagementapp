using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/statuses")]
[Authorize(Roles = "Admin,Contributor")]
public class StatusesController(StatusService statusService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusDto>>> List(CancellationToken ct = default) =>
        Ok(await statusService.ListAsync(ct));
}
