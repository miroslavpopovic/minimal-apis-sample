using Asp.Versioning.Builder;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Extensions;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Projects;

public class ProjectsModule : ICarterModule
{
    private readonly Lazy<ApiVersionSet> _apiVersionSet;

    public ProjectsModule(Lazy<ApiVersionSet> apiVersionSet)
    {
        _apiVersionSet = apiVersionSet;
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var projectsGroup = app.MapGroup("/api/v{version:apiVersion}/projects")
            .WithApiVersionSet(_apiVersionSet.Value)
            .WithTags("Projects");
        var projectsAdminGroup = projectsGroup.MapGroup("/")
            .RequireAuthorization("AdminPolicy");

        projectsGroup
            .MapGet("/", GetProjects)
            .WithName(nameof(GetProjects))
            .WithOpenApi(
                "Get a paged list of projects.", 
                "Gets one page of the available projects.", 
                "Page number.", 
                "Page size.");

        projectsGroup
            .MapGet("/{id:long}", GetProject)
            .WithName(nameof(GetProject))
            .WithOpenApi(
                "Get a project by id.", 
                "Gets a single project by id value.", 
                "Id of the project to retrieve.");

        projectsAdminGroup
            .MapDelete("/{id:long}", DeleteProject)
            .WithName(nameof(DeleteProject))
            .WithOpenApi(
                "Delete a project by id.", 
                "Deletes a single project by id value.", 
                "Id of the project to delete.");

        projectsAdminGroup
            .MapPost("/", CreateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .WithName(nameof(CreateProject))
            .WithOpenApi(
                "Create a new project.", 
                "Creates a new project with supplied values.");

        projectsAdminGroup
            .MapPut("/{id:long}", UpdateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .WithName(nameof(UpdateProject))
            .WithOpenApi(
                "Update a project by id.", 
                "Updates a project with the given id, using the supplied data.", 
                "Id of the project to update.");
    }

    private static async Task<Results<ValidationProblem, NotFound, Created<ProjectModel>>> CreateProject(
        ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Creating a new project with name {Name}", model.Name);

        var client = await dbContext.Clients!.FindAsync(model.ClientId);
        if (client == null)
        {
            return TypedResults.NotFound();
        }

        var project = new Project { Client = client };
        model.MapTo(project);

        await dbContext.Projects!.AddAsync(project);
        await dbContext.SaveChangesAsync();

        var resultModel = ProjectModel.FromProject(project);

        return TypedResults.Created($"/api/v1/projects/{project.Id}", resultModel);
    }

    private static async Task<Results<NotFound, Ok>> DeleteProject(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Deleting a project with id {Id}", id);

        var project = await dbContext.Projects!.FindAsync(id);

        if (project == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Results<NotFound, Ok<ProjectModel>>> GetProject(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a project with id {Id}", id);

        var project = await dbContext.Projects!
            .Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return project == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProjectModel.FromProject(project));
    }

    private static async Task<Ok<PagedList<ProjectModel>>> GetProjects(
        TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
    {
        logger.LogDebug("Getting a page {Page} of projects with page size {Size}", page, size);

        var projects = await dbContext.Projects!
            .Include(x => x.Client)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return TypedResults.Ok(new PagedList<ProjectModel>
        {
            Items = projects.Select(ProjectModel.FromProject),
            Page = page,
            PageSize = size,
            TotalCount = await dbContext.Projects!.CountAsync()
        });
    }

    private static async Task<Results<ValidationProblem, NotFound, Ok<ProjectModel>>> UpdateProject(
        long id, ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Updating a project with id {Id}", id);

        var project = await dbContext.Projects!.FindAsync(id);
        var client = await dbContext.Clients!.FindAsync(model.ClientId);

        if (project == null || client == null)
        {
            return TypedResults.NotFound();
        }

        project.Client = client;
        model.MapTo(project);

        dbContext.Projects.Update(project);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(ProjectModel.FromProject(project));
    }
}
