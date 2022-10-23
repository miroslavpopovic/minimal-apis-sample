using Carter;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.TimeEntries;

public class TimeEntriesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/time-entries", GetTimeEntries)
            .Produces<PagedList<TimeEntryModel>>()
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntries))
            .WithSummary("Get a paged list of time entries.")
            .WithDescription("Gets one page of the available time entries.")
            .WithTags("TimeEntries")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        app.MapGet("/api/v1/time-entries/{userId:long}/{year:int}/{month:int}", GetTimeEntriesByUserAndMonth)
            .Produces<TimeEntryModel[]>()
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntriesByUserAndMonth))
            .WithSummary("Get a list of time entries for user and month.")
            .WithDescription("Gets a list of time entries for a specified user and month.")
            .WithTags("TimeEntries")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to retrieve time entries for.";
                operation.Parameters[1].Description = "Year of the time entries.";
                operation.Parameters[2].Description = "Month of the time entries.";
                return operation;
            });

        app.MapGet("/api/v1/time-entries/{id:long}", GetTimeEntry)
            .Produces<TimeEntryModel>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntry))
            .WithSummary("Get a time entry by id.")
            .WithDescription("Gets a single time entry by id value.")
            .WithTags("TimeEntries")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to retrieve.";
                return operation;
            });

        app.MapDelete("/api/v1/time-entries/{id:long}", DeleteTimeEntry)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .RequireRateLimiting("modify")
            .WithName(nameof(DeleteTimeEntry))
            .WithSummary("Delete a time entry by id.")
            .WithDescription("Deletes a single time entry by id value.")
            .WithTags("TimeEntries")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to delete.";
                return operation;
            });

        app.MapPost("/api/v1/time-entries", CreateTimeEntry)
            .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
            .Produces<TimeEntryModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization("AdminPolicy")
            .RequireRateLimiting("modify")
            .WithName("CreateTimeEntry")
            .WithSummary("Create a new time entry.")
            .WithTags("TimeEntries")
            .WithDescription("Creates a new time entry with supplied values.")
            .WithOpenApi();

        app.MapPut("/api/v1/time-entries/{id:long}", UpdateTimeEntry)
            .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
            .Produces<TimeEntryModel>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .RequireRateLimiting("modify")
            .WithName(nameof(UpdateTimeEntry))
            .WithSummary("Update a time entry by id.")
            .WithDescription("Updates a time entry with the given id, using the supplied data.")
            .WithTags("TimeEntries")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to update.";
                return operation;
            });
    }

    private static async Task<IResult> CreateTimeEntry(
        TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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

        var timeEntry = new TimeEntry { User = user, Project = project, HourRate = user.HourRate };
        model.MapTo(timeEntry);

        await dbContext.TimeEntries!.AddAsync(timeEntry);
        await dbContext.SaveChangesAsync();

        var resultModel = TimeEntryModel.FromTimeEntry(timeEntry);

        return Results.CreatedAtRoute(nameof(GetTimeEntry), new { id = timeEntry.Id }, resultModel);
    }

    private static async Task<IResult> DeleteTimeEntry(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }

    private static async Task<PagedList<TimeEntryModel>> GetTimeEntries(
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
    private static async Task<TimeEntryModel[]> GetTimeEntriesByUserAndMonth(
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

    private static async Task<IResult> GetTimeEntry(ILogger<Program> logger, long id, TimeTrackerDbContext dbContext)
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
    }

    private static async Task<IResult> UpdateTimeEntry(
        long id, TimeEntryInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }
}
