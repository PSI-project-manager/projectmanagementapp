using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    // TODO(US-06 follow-up): requestingUserId/isAdmin should come from the authenticated
    // caller's JWT once auth middleware is wired in (see AuthService/Program.cs), not from
    // the query string. Left explicit for now so this story doesn't block on that work.

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(
        [FromQuery] int requestingUserId,
        [FromQuery] bool isAdmin,
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAsync(requestingUserId, isAdmin, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(
        int id,
        [FromQuery] int requestingUserId,
        [FromQuery] bool isAdmin,
        CancellationToken cancellationToken
    ) =>
        Ok(await projectService.GetByIdAsync(id, requestingUserId, isAdmin, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = project.Id,
                requestingUserId = request.CreatedByUserId,
                isAdmin = false,
            },
            project
        );
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(
        int id,
        UpdateProjectRequestBody body,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.UpdateAsync(
            new UpdateProjectRequest(id, body.Name, body.Description),
            cancellationToken
        );
        return Ok(project);
    }

    [HttpGet("{id:int}/users")]
    public async Task<ActionResult<IReadOnlyList<ProjectAccessDto>>> ListAccess(
        int id,
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAccessAsync(id, cancellationToken));

    [HttpPost("{id:int}/users/{userId:int}")]
    public async Task<IActionResult> GrantAccess(
        int id,
        int userId,
        CancellationToken cancellationToken
    )
    {
        await projectService.GrantAccessAsync(
            new ProjectAccessRequest(id, userId),
            cancellationToken
        );
        return NoContent();
    }

    [HttpDelete("{id:int}/users/{userId:int}")]
    public async Task<IActionResult> RevokeAccess(
        int id,
        int userId,
        CancellationToken cancellationToken
    )
    {
        await projectService.RevokeAccessAsync(
            new ProjectAccessRequest(id, userId),
            cancellationToken
        );
        return NoContent();
    }
}

public sealed record UpdateProjectRequestBody(string Name, string? Description);