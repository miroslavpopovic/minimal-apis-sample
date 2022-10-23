using Carter;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Projects;

public class ProjectsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/projects", GetProjects)
            .Produces<PagedList<ProjectModel>>()
            .WithName(nameof(GetProjects))
            .WithSummary("Get a paged list of projects.")
            .WithDescription("Gets one page of the available projects.")
            .WithTags("Projects")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        app.MapGet("/api/v1/projects/{id:long}", GetProject)
            .Produces<ProjectModel>()
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetProject))
            .WithSummary("Get a project by id.")
            .WithDescription("Gets a single project by id value.")
            .WithTags("Projects")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to retrieve.";
                return operation;
            });

        app.MapDelete("/api/v1/projects/{id:long}", DeleteProject)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(DeleteProject))
            .WithSummary("Delete a project by id.")
            .WithDescription("Deletes a single project by id value.")
            .WithTags("Projects")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to delete.";
                return operation;
            });

        app.MapPost("/api/v1/projects", CreateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .Produces<ProjectModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(CreateProject))
            .WithSummary("Create a new project.")
            .WithTags("Projects")
            .WithDescription("Creates a new project with supplied values.")
            .WithOpenApi();

        app.MapPut("/api/v1/projects/{id:long}", UpdateProject)
            .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
            .Produces<ProjectModel>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(UpdateProject))
            .WithSummary("Update a project by id.")
            .WithDescription("Updates a project with the given id, using the supplied data.")
            .WithTags("Projects")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the project to update.";
                return operation;
            });
    }

    private static async Task<IResult> CreateProject(
        ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Creating a new project with name {Name}", model.Name);

        var client = await dbContext.Clients!.FindAsync(model.ClientId);
        if (client == null)
        {
            return Results.NotFound();
        }

        var project = new Project { Client = client };
        model.MapTo(project);

        await dbContext.Projects!.AddAsync(project);
        await dbContext.SaveChangesAsync();

        var resultModel = ProjectModel.FromProject(project);

        return Results.CreatedAtRoute(nameof(GetProject), new { id = project.Id }, resultModel);
    }

    private static async Task<IResult> DeleteProject(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Deleting a project with id {Id}", id);

        var project = await dbContext.Projects!.FindAsync(id);

        if (project == null)
        {
            return Results.NotFound();
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }

    private static async Task<IResult> GetProject(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a project with id {Id}", id);

        var project = await dbContext.Projects!
            .Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return project == null
            ? Results.NotFound()
            : Results.Ok(ProjectModel.FromProject(project));
    }

    private static async Task<PagedList<ProjectModel>> GetProjects(
        TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
    {
        logger.LogDebug("Getting a page {Page} of projects with page size {Size}", page, size);

        var projects = await dbContext.Projects!
            .Include(x => x.Client)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedList<ProjectModel>
        {
            Items = projects.Select(ProjectModel.FromProject),
            Page = page,
            PageSize = size,
            TotalCount = await dbContext.Projects!.CountAsync()
        };
    }

    private static async Task<IResult> UpdateProject(long id, ProjectInputModel model, TimeTrackerDbContext dbContext,
        ILogger<Program> logger)
    {
        logger.LogDebug("Updating a project with id {Id}", id);

        var project = await dbContext.Projects!.FindAsync(id);
        var client = await dbContext.Clients!.FindAsync(model.ClientId);

        if (project == null || client == null)
        {
            return Results.NotFound();
        }

        project.Client = client;
        model.MapTo(project);

        dbContext.Projects.Update(project);
        await dbContext.SaveChangesAsync();

        return Results.Ok(ProjectModel.FromProject(project));
    }
}
