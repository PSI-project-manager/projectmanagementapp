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

    
    [HttpGet]
    [Authorize(Roles="Admin, Contributor")]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAsync(CurrentUserId, cancellationToken));

    [HttpPost]
    [Authorize(Roles="Admin")]
    public async Task<ActionResult<ProjectDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken
    )
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { }, project);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles="Admin")]
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
