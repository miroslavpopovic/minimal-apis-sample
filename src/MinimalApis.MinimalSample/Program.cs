using Asp.Versioning;
using Asp.Versioning.Conventions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample;
using MinimalApis.MinimalSample.Data;
using MinimalApis.MinimalSample.Domain;
using MinimalApis.MinimalSample.Extensions;
using MinimalApis.MinimalSample.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<TimeTrackerDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!));

builder.Services.AddDemoAuthorization();
builder.Services.AddDemoAuthentication();
builder.Services.AddDemoRateLimiter();

builder.Services.AddProblemDetails();

var version1 = new ApiVersion(1);
var version2 = new ApiVersion(2);
var apiVersions = new[] { version1, version2 };

builder.Services.AddApiVersioning(version2);
builder.Services.AddOpenApi();

builder.Services.AddCors();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

var versionSet = app.NewApiVersionSet().HasApiVersions(apiVersions).Build();

app.UseSwaggerUI(apiVersions);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Demo purpose only! Restrict CORS in production.
app.UseCors(policyBuilder => policyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseRateLimiter();

async Task<PagedList<ClientModel>> GetClients(
    TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
{
    logger.LogDebug("Getting a page {Page} of clients with page size {Size}", page, size);

    var clients = await dbContext.Clients!
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();

    return new PagedList<ClientModel>
    {
        Items = clients.Select(ClientModel.FromClient),
        Page = page,
        PageSize = size,
        TotalCount = await dbContext.Clients!.CountAsync()
    };
}

var clientsGroup = app
    .MapGroup("/api/v{version:apiVersion}/clients/")
    .WithApiVersionSet(versionSet)
    .WithTags("Clients");
var clientsAdminGroup = clientsGroup.MapGroup(string.Empty)
    .RequireAuthorization("AdminPolicy");

clientsGroup.MapGet(string.Empty, GetClients)
    .WithName("GetClients")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a paged list of clients.";
        operation.Description = "Gets one page of the available clients.";
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

clientsGroup.MapGet("{id:long}", async Task<Results<NotFound, Ok<ClientModel>>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        return client == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ClientModel.FromClient(client));
    })
    .WithName("GetClient")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a client by id.";
        operation.Description = "Gets a single client by id value.";
        operation.Parameters[0].Description = "Id of the client to retrieve.";
        return operation;
    });

clientsAdminGroup.MapDelete("{id:long}",
    async Task<Results<NotFound, Ok>> (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        if (client == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Clients.Remove(client);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok();
    })
    .WithName("DeleteClient")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Delete a client by id.";
        operation.Description = "Deletes a single client by id value.";
        operation.Parameters[0].Description = "Id of the client to delete.";
        return operation;
    });

clientsAdminGroup.MapPost(string.Empty, async Task<Results<ValidationProblem, CreatedAtRoute<ClientModel>>> (
        ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new client with name {Name}", model.Name);

        var client = new Client();
        model.MapTo(client);

        await dbContext.Clients!.AddAsync(client);
        await dbContext.SaveChangesAsync();

        var resultModel = ClientModel.FromClient(client);

        return TypedResults.CreatedAtRoute(resultModel, "GetClient", new { id = client.Id });
    })
    .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
    .WithName("CreateClient")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Create a new client.";
        operation.Description = "Creates a new client with supplied values.";
        return operation;
    });

clientsAdminGroup.MapPut("{id:long}", async Task<Results<ValidationProblem, NotFound, Ok<ClientModel>>> (
        long id, ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        if (client == null)
        {
            return TypedResults.NotFound();
        }

        model.MapTo(client);

        dbContext.Clients.Update(client);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(ClientModel.FromClient(client));
    })
    .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
    .WithName("UpdateClient")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Update a client by id.";
        operation.Description = "Updates a client with the given id, using the supplied data.";
        operation.Parameters[0].Description = "Id of the client to update.";
        return operation;
    });

var projectsGroup = app
    .MapGroup("/api/v{version:apiVersion}/projects/")
    .WithApiVersionSet(versionSet)
    .WithTags("Projects");
var projectsAdminGroup = projectsGroup.MapGroup(string.Empty)
    .RequireAuthorization("AdminPolicy");

async Task<PagedList<ProjectModel>> GetProjects(
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

projectsGroup.MapGet(string.Empty, GetProjects)
    .WithName("GetProjects")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a paged list of projects.";
        operation.Description = "Gets one page of the available projects.";
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

projectsGroup.MapGet("{id:long}", async Task<Results<NotFound, Ok<ProjectModel>>> (
    long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a project with id {Id}", id);

        var project = await dbContext.Projects!
            .Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return project == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProjectModel.FromProject(project));
    })
    .WithName("GetProject")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a project by id.";
        operation.Description = "Gets a single project by id value.";
        operation.Parameters[0].Description = "Id of the project to retrieve.";
        return operation;
    });

projectsAdminGroup.MapDelete("{id:long}", async Task<Results<NotFound, Ok>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
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
    })
    .WithName("DeleteProject")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Delete a project by id.";
        operation.Description = "Deletes a single project by id value.";
        operation.Parameters[0].Description = "Id of the project to delete.";
        return operation;
    });

projectsAdminGroup.MapPost(string.Empty,
    async Task<Results<ValidationProblem, NotFound, CreatedAtRoute<ProjectModel>>> (
        ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new project with name {Name}", model.Name);

        var client = await dbContext.Clients!.FindAsync(model.ClientId);
        if (client == null)
        {
            return TypedResults.NotFound();
        }

        var project = new Project{Client = client};
        model.MapTo(project);

        await dbContext.Projects!.AddAsync(project);
        await dbContext.SaveChangesAsync();

        var resultModel = ProjectModel.FromProject(project);

        return TypedResults.CreatedAtRoute(resultModel, "GetProject", new {id = project.Id});
    })
    .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
    .WithName("CreateProject")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Create a new project.";
        operation.Description = "Creates a new project with supplied values.";
        return operation;
    });

projectsAdminGroup.MapPut("{id:long}", async Task<Results<ValidationProblem, NotFound, Ok<ProjectModel>>> (
        long id, ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
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
    })
    .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
    .WithName("UpdateProject")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Update a project by id.";
        operation.Description = "Updates a project with the given id, using the supplied data.";
        operation.Parameters[0].Description = "Id of the project to update.";
        return operation;
    });

var timeEntriesGroup = app
    .MapGroup("/api/v{version:apiVersion}/time-entries/")
    .WithApiVersionSet(versionSet)
    .WithTags("TimeEntries");
var timeEntriesAdminGroup = timeEntriesGroup.MapGroup(string.Empty)
    .RequireAuthorization("AdminPolicy");

async Task<PagedList<TimeEntryModel>> GetTimeEntries(
    TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
{
    logger.LogDebug("Getting a page {Page} of time entries with page size {Size}", page, size);

    var timeEntries = await dbContext.TimeEntries!
        .Include(x => x.User)
        .Include(x => x.Project)
        .Include(x => x.Project.Client)
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();

    return new PagedList<TimeEntryModel>
    {
        Items = timeEntries.Select(TimeEntryModel.FromTimeEntry),
        Page = page,
        PageSize = size,
        TotalCount = await dbContext.TimeEntries!.CountAsync()
    };
}

async Task<TimeEntryModel[]> GetTimeEntriesByUserAndMonth(
    TimeTrackerDbContext dbContext, ILogger<Program> logger, long userId, int year, int month)
{
    logger.LogDebug(
        "Getting all time entries for month {Year}-{Month} for user with id {UserId}", year, month, userId);

    var startDate = new DateTime(year, month, 1);
    var endDate = startDate.AddMonths(1);

    var timeEntries = await dbContext.TimeEntries!
        .Include(x => x.User)
        .Include(x => x.Project)
        .Include(x => x.Project.Client)
        .Where(x => x.User.Id == userId && x.EntryDate >= startDate && x.EntryDate < endDate)
        .OrderBy(x => x.EntryDate)
        .ToListAsync();

    return timeEntries
        .Select(TimeEntryModel.FromTimeEntry)
        .ToArray();
}

timeEntriesGroup.MapGet(string.Empty, GetTimeEntries)
    .RequireRateLimiting("get")
    .WithName("GetTimeEntries")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a paged list of time entries.";
        operation.Description = "Gets one page of the available time entries.";
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

timeEntriesGroup.MapGet("{userId:long}/{year:int}/{month:int}", GetTimeEntriesByUserAndMonth)
    .RequireRateLimiting("get")
    .WithName("GetTimeEntriesByUserAndMonth")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a list of time entries for user and month.";
        operation.Description = "Gets a list of time entries for a specified user and month.";
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

timeEntriesGroup.MapGet("{id:long}", async Task<Results<NotFound, Ok<TimeEntryModel>>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a time entry with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!
            .Include(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Project.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return timeEntry == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(TimeEntryModel.FromTimeEntry(timeEntry));
    })
    .RequireRateLimiting("get")
    .WithName("GetTimeEntry")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a time entry by id.";
        operation.Description = "Gets a single time entry by id value.";
        operation.Parameters[0].Description = "Id of the time entry to retrieve.";
        return operation;
    });

timeEntriesAdminGroup.MapDelete("{id:long}", async Task<Results<NotFound, Ok>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting time entries with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!.FindAsync(id);

        if (timeEntry == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.TimeEntries.Remove(timeEntry);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok();
    })
    .RequireRateLimiting("modify")
    .WithName("DeleteTimeEntry")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Delete a time entry by id.";
        operation.Description = "Deletes a single time entry by id value.";
        operation.Parameters[0].Description = "Id of the time entry to delete.";
        return operation;
    });

timeEntriesAdminGroup.MapPost(string.Empty, async Task<Results<ValidationProblem, NotFound, CreatedAtRoute<TimeEntryModel>>> (
        TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug(
            "Creating a new time entry for user {UserId}, project {ProjectId} and date {EntryDate}",
            model.UserId, model.ProjectId, model.EntryDate);

        var user = await dbContext.Users!.FindAsync(model.UserId);
        var project = await dbContext.Projects!
            .Include(x => x.Client) // Necessary for mapping to TimeEntryModel later
            .SingleOrDefaultAsync(x => x.Id == model.ProjectId);

        if (user == null || project == null)
        {
            return TypedResults.NotFound();
        }

        var timeEntry = new TimeEntry {User = user, Project = project, HourRate = user.HourRate};
        model.MapTo(timeEntry);

        await dbContext.TimeEntries!.AddAsync(timeEntry);
        await dbContext.SaveChangesAsync();

        var resultModel = TimeEntryModel.FromTimeEntry(timeEntry);

        return TypedResults.CreatedAtRoute(resultModel, "GetTimeEntry", new { id = timeEntry.Id });
    })
    .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
    .RequireRateLimiting("modify")
    .WithName("CreateTimeEntry")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Create a new time entry.";
        operation.Description = "Creates a new time entry with supplied values.";
        return operation;
    });

timeEntriesAdminGroup.MapPut("{id:long}", async Task<Results<ValidationProblem, NotFound, Ok<TimeEntryModel>>> (
        long id, TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a time entry with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!
            .Include(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Project.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (timeEntry == null)
        {
            return TypedResults.NotFound();
        }

        model.MapTo(timeEntry);

        dbContext.TimeEntries!.Update(timeEntry);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(TimeEntryModel.FromTimeEntry(timeEntry));
    })
    .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
    .RequireRateLimiting("modify")
    .WithName("UpdateTimeEntry")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Update a time entry by id.";
        operation.Description = "Updates a time entry with the given id, using the supplied data.";
        operation.Parameters[0].Description = "Id of the time entry to update.";
        return operation;
    });

var usersGroup = app
    .MapGroup("/api/v{version:apiVersion}/users/")
    .WithApiVersionSet(versionSet)
    .WithTags("Users");
var usersAdminGroup = usersGroup.MapGroup(string.Empty)
    .RequireAuthorization("AdminPolicy");

async Task<PagedList<UserModel>> GetUsers(
    TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
{
    logger.LogDebug("Getting a page {Page} of users with page size {Size}", page, size);

    var users = await dbContext.Users!
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();

    return new PagedList<UserModel>
    {
        Items = users.Select(UserModel.FromUser),
        Page = page,
        PageSize = size,
        TotalCount = await dbContext.Users!.CountAsync()
    };
}

usersGroup.MapGet(string.Empty, GetUsers)
    .WithName("GetUsers")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a paged list of users.";
        operation.Description = "Gets one page of the available users.";
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

usersGroup.MapGet("{id:long}", async Task<Results<NotFound, Ok<UserModel>>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        return user == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(UserModel.FromUser(user));
    })
    .WithName("GetUser")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get a user by id.";
        operation.Description = "Gets a single user by id value.";
        operation.Parameters[0].Description = "Id of the user to retrieve.";
        return operation;
    });

usersAdminGroup.MapDelete("{id:long}", async Task<Results<NotFound, Ok>> (
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        if (user == null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok();
    })
    .WithName("DeleteUser")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Delete a user by id.";
        operation.Description = "Deletes a single user by id value.";
        operation.Parameters[0].Description = "Id of the user to delete.";
        return operation;
    });

usersAdminGroup.MapPost(string.Empty, async Task<Results<ValidationProblem, CreatedAtRoute<UserModel>>> (
        UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new user with name {Name}", model.Name);

        var user = new User();
        model.MapTo(user);

        await dbContext.Users!.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var resultModel = UserModel.FromUser(user);

        return TypedResults.CreatedAtRoute(resultModel, "GetUsers", new { id = user.Id });
    })
    .AddEndpointFilter<ValidationFilter<UserInputModel>>()
    .WithName("CreateUser")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Create a new user.";
        operation.Description = "Creates a new user with supplied values.";
        return operation;
    });

usersAdminGroup.MapPut("{id:long}", async Task<Results<ValidationProblem, NotFound, Ok<UserModel>>> (
        long id, UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        if (user == null)
        {
            return TypedResults.NotFound();
        }

        model.MapTo(user);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(UserModel.FromUser(user));
    })
    .AddEndpointFilter<ValidationFilter<UserInputModel>>()
    .WithName("UpdateUser")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Update a user by id.";
        operation.Description = "Updates a user with the given id, using the supplied data.";
        operation.Parameters[0].Description = "Id of the user to update.";
        return operation;
    });

app.Run();

// Necessary to make Program class accessible in tests
public partial class Program { }
