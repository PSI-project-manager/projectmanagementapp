using System.Security.Claims;
using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAsync(CurrentUserId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { }, project);
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
            CurrentUserId,
            cancellationToken
        );
        return Ok(project);
    }
}

public record UpdateProjectRequestBody(string Name, string? Description);