using Api.Dtos;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController(ItemService itemService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> List(
        [FromQuery] int projectId,
        CancellationToken ct = default
    )
    {
        return Ok(await itemService.GetItemsFromProjectAsync(projectId, User.GetUserId(), ct));
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
