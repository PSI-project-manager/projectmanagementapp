using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/item-types")]
public sealed class ItemTypesController(IItemTypeService itemTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemTypeDto>>> List(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default
    )
    {
        var itemTypes = await itemTypeService.ListAsync(activeOnly, ct);
        return Ok(itemTypes);
    }

    [HttpPost]
    public async Task<ActionResult<ItemTypeDto>> Create(
        [FromBody] CreateItemTypeRequest request,
        CancellationToken ct = default
    )
    {
        var created = await itemTypeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemTypeDto>> Update(
        int id,
        [FromBody] UpdateItemTypeRequestBody body,
        CancellationToken ct = default
    )
    {
        var request = new UpdateItemTypeRequest(id, body.Name, body.IsActive);
        var updated = await itemTypeService.UpdateAsync(request, ct);
        return Ok(updated);
    }
}


public sealed record UpdateItemTypeRequestBody(string Name, bool IsActive);
