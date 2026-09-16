using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> List(
        CancellationToken cancellationToken
    ) => Ok(await projectService.ListAsync(cancellationToken));

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
            cancellationToken
        );
        return Ok(project);
    }
}

public sealed record UpdateProjectRequestBody(string Name, string? Description);
