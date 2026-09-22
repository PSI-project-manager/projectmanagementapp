using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private AppDbContext db;

    public ProjectsController(AppDbContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public IActionResult GetProjects()
    {
        List<Project> projects = db.Projects.OrderBy(p => p.Name).ToList();

        var result = new List<ProjectDto>();
        foreach (Project project in projects)
        {
            result.Add(ToDto(project));
        }

        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateProject(CreateProjectRequest request)
    {
        string? error = CheckProject(request.Name, request.Description);
        if (error != null)
        {
            return BadRequest(new { detail = error });
        }

        if (request.CreatedByUserId <= 0)
        {
            return BadRequest(new { detail = "A project must have a creator." });
        }

        var project = new Project();
        project.Name = request.Name.Trim();
        project.Description = CleanDescription(request.Description);
        project.Createdbyuserid = request.CreatedByUserId;

        db.Projects.Add(project);
        db.SaveChanges();

        return StatusCode(201, ToDto(project));
    }

    [HttpPut("{id}")]
    public IActionResult UpdateProject(int id, UpdateProjectRequest request)
    {
        string? error = CheckProject(request.Name, request.Description);
        if (error != null)
        {
            return BadRequest(new { detail = error });
        }

        Project? project = db.Projects.Find(id);
        if (project == null)
        {
            return NotFound(new { detail = "Project " + id + " was not found." });
        }

        project.Name = request.Name.Trim();
        project.Description = CleanDescription(request.Description);
        db.SaveChanges();

        return Ok(ToDto(project));
    }

    // returns an error message, or null if everything is ok
    private string? CheckProject(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Project name is required.";
        }

        if (name.Length > Project.MaxNameLength)
        {
            return "Project name cannot exceed " + Project.MaxNameLength + " characters.";
        }

        if (description != null && description.Length > Project.MaxDescriptionLength)
        {
            return "Description cannot exceed " + Project.MaxDescriptionLength + " characters.";
        }

        return null;
    }

    // empty descriptions are saved as null
    private string? CleanDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private ProjectDto ToDto(Project project)
    {
        return new ProjectDto(
            project.Projectid,
            project.Name,
            project.Description,
            project.Isactive,
            project.Createdbyuserid
        );
    }
}
