using System.Security.Claims;
using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController(ProjectService projectService) : ControllerBase
{
    // AuthService puts the user id in the "sub" claim; depending on claim mapping it can
    // show up as either "sub" or NameIdentifier
    private int CurrentUserId =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var id
        )
            ? id
            : throw new UnauthorizedAccessException("Token has no valid user id.");

    // read off the token's role claims, same as the [Authorize(Roles=...)] attributes
    private bool CurrentUserIsAdmin => User.IsInRole("Admin");

    [HttpGet]
    [Authorize(Roles = "Admin, Contributor")]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAsync(CurrentUserId, CurrentUserIsAdmin, cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectRequestBody body,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.CreateAsync(
            new CreateProjectRequest(body.Name, body.Description, CurrentUserId),
            cancellationToken
        );
        return CreatedAtAction(nameof(List), new { }, project);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> Update(
        int id,
        UpdateProjectRequestBody body,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.UpdateAsync(
            new UpdateProjectRequest(id, body.Name, body.Description),
            CurrentUserId,
            CurrentUserIsAdmin,
            cancellationToken
        );
        return Ok(project);
    }

    [HttpGet("{id:int}/users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListUsers(
        int id,
        CancellationToken cancellationToken
    )
    {
        var users = await projectService.ListUsersAsync(id, cancellationToken);
        return Ok(users);
    }

    [HttpPost("{id:int}/users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> AddUser(
        int id,
        AddProjectUserRequestBody body,
        CancellationToken cancellationToken
    )
    {
        await projectService.AddUserAsync(body.UserId, id, CurrentUserIsAdmin, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}/users/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RemoveUser(
            int id,
            int userId,
            CancellationToken cancellationToken
            )
    {
        await projectService.RemoveUserAsync(userId, id, CurrentUserIsAdmin,cancellationToken);
        return NoContent();
    }
}

public record CreateProjectRequestBody(string Name, string? Description);

public record UpdateProjectRequestBody(string Name, string? Description);

public record AddProjectUserRequestBody(int UserId);
