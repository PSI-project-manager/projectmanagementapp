using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// TODO (US-08): restrict to admins once role-based authorization is wired up.
[ApiController]
[Route("api/users")]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> List(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default
    )
    {
        var users = await userService.ListAsync(activeOnly, ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct = default
    )
    {
        var created = await userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        [FromBody] UpdateUserRequestBody body,
        CancellationToken ct = default
    )
    {
        var request = new UpdateUserRequest(id, body.Email, body.FullName);
        var updated = await userService.UpdateAsync(request, ct);
        return Ok(updated);
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<UserDto>> Activate(int id, CancellationToken ct = default) =>
        Ok(await userService.SetActiveAsync(id, true, ct));

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<UserDto>> Deactivate(int id, CancellationToken ct = default) =>
        Ok(await userService.SetActiveAsync(id, false, ct));
}

public record UpdateUserRequestBody(string Email, string FullName);
