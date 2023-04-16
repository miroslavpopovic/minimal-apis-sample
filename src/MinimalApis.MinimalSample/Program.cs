using FluentValidation;
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

builder.Services.AddOpenApi();

builder.Services.AddCors();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

var clientsGroup = app.MapGroup("/api/v1/clients")
    .WithTags("Clients");
var clientsAdminGroup = clientsGroup.MapGroup("/")
    .RequireAuthorization("AdminPolicy");

clientsGroup
    .MapGet("/", GetClients)
    .Produces<PagedList<ClientModel>>()
    .WithName("GetClients")
    .WithSummary("Get a paged list of clients.")
    .WithDescription("Gets one page of the available clients.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

clientsGroup
    .MapGet("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        return client == null
            ? Results.NotFound()
            : Results.Ok(ClientModel.FromClient(client));
    })
    .Produces<ClientModel>()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetClient")
    .WithSummary("Get a client by id.")
    .WithDescription("Gets a single client by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the client to retrieve.";
        return operation;
    });

clientsAdminGroup
    .MapDelete("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        if (client == null)
        {
            return Results.NotFound();
        }

        dbContext.Clients.Remove(client);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("DeleteClient")
    .WithSummary("Delete a client by id.")
    .WithDescription("Deletes a single client by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the client to delete.";
        return operation;
    });

clientsAdminGroup
    .MapPost("/",
    async (ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new client with name {Name}", model.Name);

        var client = new Client();
        model.MapTo(client);

        await dbContext.Clients!.AddAsync(client);
        await dbContext.SaveChangesAsync();

        var resultModel = ClientModel.FromClient(client);

        return Results.CreatedAtRoute("GetClient", new {id = client.Id}, resultModel);
    })
    .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
    .Produces<ClientModel>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .WithName("CreateClient")
    .WithSummary("Create a new client.")
    .WithDescription("Creates a new client with supplied values.")
    .WithOpenApi();

clientsAdminGroup
    .MapPut("/{id:long}",
    async (long id, ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        if (client == null)
        {
            return Results.NotFound();
        }

        model.MapTo(client);

        dbContext.Clients.Update(client);
        await dbContext.SaveChangesAsync();

        return Results.Ok(ClientModel.FromClient(client));
    })
    .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
    .Produces<ClientModel>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("UpdateClient")
    .WithSummary("Update a client by id.")
    .WithDescription("Updates a client with the given id, using the supplied data.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the client to update.";
        return operation;
    });

var projectsGroup = app.MapGroup("/api/v1/projects")
    .WithTags("Projects");
var projectsAdminGroup = projectsGroup.MapGroup("/")
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

projectsGroup
    .MapGet("/", GetProjects)
    .Produces<PagedList<ProjectModel>>()
    .WithName("GetProjects")
    .WithSummary("Get a paged list of projects.")
    .WithDescription("Gets one page of the available projects.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

projectsGroup
    .MapGet("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a project with id {Id}", id);

        var project = await dbContext.Projects!
            .Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return project == null
            ? Results.NotFound()
            : Results.Ok(ProjectModel.FromProject(project));
    })
    .Produces<ProjectModel>()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetProject")
    .WithSummary("Get a project by id.")
    .WithDescription("Gets a single project by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the project to retrieve.";
        return operation;
    });

projectsAdminGroup
    .MapDelete("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
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
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("DeleteProject")
    .WithSummary("Delete a project by id.")
    .WithDescription("Deletes a single project by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the project to delete.";
        return operation;
    });

projectsAdminGroup
    .MapPost("/",
    async (ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new project with name {Name}", model.Name);

        var client = await dbContext.Clients!.FindAsync(model.ClientId);
        if (client == null)
        {
            return Results.NotFound();
        }

        var project = new Project{Client = client};
        model.MapTo(project);

        await dbContext.Projects!.AddAsync(project);
        await dbContext.SaveChangesAsync();

        var resultModel = ProjectModel.FromProject(project);

        return Results.CreatedAtRoute("GetProject", new {id = project.Id}, resultModel);
    })
    .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
    .Produces<ProjectModel>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .WithName("CreateProject")
    .WithSummary("Create a new project.")
    .WithDescription("Creates a new project with supplied values.")
    .WithOpenApi();

projectsAdminGroup
    .MapPut("/{id:long}",
    async (long id, ProjectInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
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
    })
    .AddEndpointFilter<ValidationFilter<ProjectInputModel>>()
    .Produces<ProjectModel>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("UpdateProject")
    .WithSummary("Update a project by id.")
    .WithDescription("Updates a project with the given id, using the supplied data.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the project to update.";
        return operation;
    });

var timeEntriesGroup = app.MapGroup("/api/v1/time-entries")
    .WithTags("TimeEntries");
var timeEntriesAdminGroup = timeEntriesGroup.MapGroup("/")
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

timeEntriesGroup
    .MapGet("/", GetTimeEntries)
    .Produces<PagedList<TimeEntryModel>>()
    .RequireRateLimiting("get")
    .WithName("GetTimeEntries")
    .WithSummary("Get a paged list of time entries.")
    .WithDescription("Gets one page of the available time entries.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

timeEntriesGroup
    .MapGet("/{userId:long}/{year:int}/{month:int}", GetTimeEntriesByUserAndMonth)
    .Produces<TimeEntryModel[]>()
    .RequireRateLimiting("get")
    .WithName("GetTimeEntriesByUserAndMonth")
    .WithSummary("Get a list of time entries for user and month.")
    .WithDescription("Gets a list of time entries for a specified user and month.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

timeEntriesGroup
    .MapGet("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a time entry with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!
            .Include(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Project.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        return timeEntry == null
            ? Results.NotFound()
            : Results.Ok(TimeEntryModel.FromTimeEntry(timeEntry));
    })
    .Produces<TimeEntryModel>()
    .Produces(StatusCodes.Status404NotFound)
    .RequireRateLimiting("get")
    .WithName("GetTimeEntry")
    .WithSummary("Get a time entry by id.")
    .WithDescription("Gets a single time entry by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the time entry to retrieve.";
        return operation;
    });

timeEntriesAdminGroup
    .MapDelete("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting time entries with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!.FindAsync(id);

        if (timeEntry == null)
        {
            return Results.NotFound();
        }

        dbContext.TimeEntries.Remove(timeEntry);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .RequireRateLimiting("modify")
    .WithName("DeleteTimeEntry")
    .WithSummary("Delete a time entry by id.")
    .WithDescription("Deletes a single time entry by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the time entry to delete.";
        return operation;
    });

timeEntriesAdminGroup
    .MapPost("/",
    async (TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
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
            return Results.NotFound();
        }

        var timeEntry = new TimeEntry {User = user, Project = project, HourRate = user.HourRate};
        model.MapTo(timeEntry);

        await dbContext.TimeEntries!.AddAsync(timeEntry);
        await dbContext.SaveChangesAsync();

        var resultModel = TimeEntryModel.FromTimeEntry(timeEntry);

        return Results.CreatedAtRoute("GetTimeEntry", new { id = timeEntry.Id }, resultModel);
    })
    .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
    .Produces<TimeEntryModel>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .RequireRateLimiting("modify")
    .WithName("CreateTimeEntry")
    .WithSummary("Create a new time entry.")
    .WithDescription("Creates a new time entry with supplied values.")
    .WithOpenApi();

timeEntriesAdminGroup
    .MapPut("/{id:long}",
    async (long id, TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a time entry with id {Id}", id);

        var timeEntry = await dbContext.TimeEntries!
            .Include(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Project.Client)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (timeEntry == null)
        {
            return Results.NotFound();
        }

        model.MapTo(timeEntry);

        dbContext.TimeEntries!.Update(timeEntry);
        await dbContext.SaveChangesAsync();

        return Results.Ok(TimeEntryModel.FromTimeEntry(timeEntry));
    })
    .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
    .Produces<TimeEntryModel>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .RequireRateLimiting("modify")
    .WithName("UpdateTimeEntry")
    .WithSummary("Update a time entry by id.")
    .WithDescription("Updates a time entry with the given id, using the supplied data.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the time entry to update.";
        return operation;
    });

var usersGroup = app.MapGroup("/api/v1/users")
    .WithTags("Users");
var usersAdminGroup = usersGroup.MapGroup("/")
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

usersGroup
    .MapGet("/", GetUsers)
    .Produces<PagedList<UserModel>>()
    .WithName("GetUsers")
    .WithSummary("Get a paged list of users.")
    .WithDescription("Gets one page of the available users.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Page number.";
        operation.Parameters[1].Description = "Page size.";
        return operation;
    });

usersGroup
    .MapGet("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Getting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        return user == null
            ? Results.NotFound()
            : Results.Ok(UserModel.FromUser(user));
    })
    .Produces<UserModel>()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetUser")
    .WithSummary("Get a user by id.")
    .WithDescription("Gets a single user by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the user to retrieve.";
        return operation;
    });

usersAdminGroup
    .MapDelete("/{id:long}",
    async (long id, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Deleting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        if (user == null)
        {
            return Results.NotFound();
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("DeleteUser")
    .WithSummary("Delete a user by id.")
    .WithDescription("Deletes a single user by id value.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the user to delete.";
        return operation;
    });

usersAdminGroup
    .MapPost("/",
    async (UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Creating a new user with name {Name}", model.Name);

        var user = new User();
        model.MapTo(user);

        await dbContext.Users!.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var resultModel = UserModel.FromUser(user);

        return Results.CreatedAtRoute("GetUsers", new { id = user.Id }, resultModel);
    })
    .AddEndpointFilter<ValidationFilter<UserInputModel>>()
    .Produces<UserModel>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .WithName("CreateUser")
    .WithSummary("Create a new user.")
    .WithDescription("Creates a new user with supplied values.")
    .WithOpenApi();

usersAdminGroup
    .MapPut("/{id:long}",
    async (long id, UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger) =>
    {
        logger.LogDebug("Updating a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        if (user == null)
        {
            return Results.NotFound();
        }

        model.MapTo(user);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();

        return Results.Ok(UserModel.FromUser(user));
    })
    .AddEndpointFilter<ValidationFilter<UserInputModel>>()
    .Produces<UserModel>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .WithName("UpdateUser")
    .WithSummary("Update a user by id.")
    .WithDescription("Updates a user with the given id, using the supplied data.")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Id of the user to update.";
        return operation;
    });

app.Run();

// Necessary to make Program class accessible in tests
public partial class Program { }
