using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize(Roles="Admin,Contributor")]
public class ItemsController(ItemService itemService) : ControllerBase
{
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
