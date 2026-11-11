using Api.Dtos;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize(Roles = "Admin,Contributor")]
public class ItemsController(ItemService itemService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> List(
        [FromQuery] int? projectId,
        [FromQuery] int? itemTypeId,
        [FromQuery] int? statusId,
        CancellationToken ct = default
    )
    {
        var filter = new ItemFilter(
            ProjectId: projectId,
            ItemTypeId: itemTypeId,
            StatusId: statusId
        );

        return Ok(
            await itemService.ListAsync(filter, User.GetUserId(), User.IsInRole("Admin"), ct)
        );
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] CreateItemRequest request,
        CancellationToken ct = default
    )
    {
        var created = await itemService.CreateAsync(request, ct);
        return Created($"/api/items/{created.Id}", created);
    }
}
