using Api.Dtos;
using Api.Extensions;
using Api.Pagination;
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
        [FromQuery] int projectId,
        CancellationToken ct = default
    )
    {
        return Ok(
            await itemService.GetItemsFromProjectAsync(
                projectId,
                User.GetUserId(),
                User.IsInRole("Admin"),
                ct
            )
        );
    }

    [HttpGet("paged")]
    public async Task<ActionResult<Page<ItemDto>>> ListPaged(
        [FromQuery] int projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    ) =>
        Ok(
            await itemService.GetItemsPageAsync(
                projectId,
                User.GetUserId(),
                User.IsInRole("Admin"),
                pageNumber,
                pageSize,
                ct
            )
        );

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(
        [FromBody] CreateItemRequest request,
        CancellationToken ct = default
    )
    {
        var created = await itemService.CreateAsync(
            request,
            User.GetUserId(),
            User.IsInRole("Admin"),
            ct
        );
        return Created($"/api/items/{created.Id}", created);
    }
}
