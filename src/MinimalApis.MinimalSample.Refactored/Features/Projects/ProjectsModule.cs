using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Projects;

public class ProjectsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var projectsGroup = app.MapGroup("/api/v1/projects")
            .WithTags("Projects");
        var projectsAdminGroup = projectsGroup.MapGroup("/")
            .RequireAuthorization("AdminPolicy");

        projectsGroup
            .MapGet("/", GetProjects)
            .WithName(nameof(GetProjects))
            .WithSummary("Get a paged list of projects.")
            .WithDescription("Gets one page of the available projects.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        projectsGroup
            .MapGet("/{id:long}", GetProject)
            .WithName(nameof(GetProject))
            .WithSummary("Get a project by id.")
            .WithDescription("Gets a single project by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to retrieve.";
                return operation;
            });

        projectsAdminGroup
            .MapDelete("/{id:long}", DeleteProject)
            .WithName(nameof(DeleteProject))
            .WithSummary("Delete a project by id.")
            .WithDescription("Deletes a single project by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to delete.";
                return operation;
            });

        projectsAdminGroup
            .MapPost("/", CreateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .WithName(nameof(CreateProject))
            .WithSummary("Create a new project.")
            .WithDescription("Creates a new project with supplied values.")
            .WithOpenApi();

        projectsAdminGroup
            .MapPut("/{id:long}", UpdateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .WithName(nameof(UpdateProject))
            .WithSummary("Update a project by id.")
            .WithDescription("Updates a project with the given id, using the supplied data.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to update.";
                return operation;
            });
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
