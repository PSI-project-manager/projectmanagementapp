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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemDto>> Update(
        int id,
        [FromBody] UpdateItemRequestBody body,
        CancellationToken ct = default
    )
    {
        var request = new UpdateItemRequest(
            id,
            body.Title,
            body.Description,
            body.ItemTypeId,
            body.StatusId
        );
        var updated = await itemService.UpdateAsync(
            request,
            User.GetUserId(),
            User.IsInRole("Admin"),
            ct
        );
        return Ok(updated);
    }
}

public record UpdateItemRequestBody(string Title, string Description, int ItemTypeId, int StatusId);
