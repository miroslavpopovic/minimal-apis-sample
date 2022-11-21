using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.TimeEntries;

public class TimeEntriesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var timeEntriesGroup = app.MapGroup("/api/v1/time-entries")
            .WithTags("TimeEntries");
        var timeEntriesAdminGroup = timeEntriesGroup.MapGroup("/")
            .RequireAuthorization("AdminPolicy")
            .RequireRateLimiting("modify");

        timeEntriesGroup
            .MapGet("/", GetTimeEntries)
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntries))
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
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntriesByUserAndMonth))
            .WithSummary("Get a list of time entries for user and month.")
            .WithDescription("Gets a list of time entries for a specified user and month.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to retrieve time entries for.";
                operation.Parameters[1].Description = "Year of the time entries.";
                operation.Parameters[2].Description = "Month of the time entries.";
                return operation;
            });

        timeEntriesGroup
            .MapGet("/{id:long}", GetTimeEntry)
            .RequireRateLimiting("get")
            .WithName(nameof(GetTimeEntry))
            .WithSummary("Get a time entry by id.")
            .WithDescription("Gets a single time entry by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to retrieve.";
                return operation;
            });

        timeEntriesAdminGroup
            .MapDelete("/{id:long}", DeleteTimeEntry)
            .WithName(nameof(DeleteTimeEntry))
            .WithSummary("Delete a time entry by id.")
            .WithDescription("Deletes a single time entry by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to delete.";
                return operation;
            });

        timeEntriesAdminGroup
            .MapPost("/", CreateTimeEntry)
            .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
            .WithName("CreateTimeEntry")
            .WithSummary("Create a new time entry.")
            .WithDescription("Creates a new time entry with supplied values.")
            .WithOpenApi();

        timeEntriesAdminGroup
            .MapPut("/{id:long}", UpdateTimeEntry)
            .AddEndpointFilter<ValidationFilter<TimeEntryInputModel>>()
            .WithName(nameof(UpdateTimeEntry))
            .WithSummary("Update a time entry by id.")
            .WithDescription("Updates a time entry with the given id, using the supplied data.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the time entry to update.";
                return operation;
            });
    }

    private static async Task<Results<ValidationProblem, NotFound, Created<TimeEntryModel>>> CreateTimeEntry(
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
            return TypedResults.NotFound();
        }

        var timeEntry = new TimeEntry { User = user, Project = project, HourRate = user.HourRate };
        model.MapTo(timeEntry);

        await dbContext.TimeEntries!.AddAsync(timeEntry);
        await dbContext.SaveChangesAsync();

        var resultModel = TimeEntryModel.FromTimeEntry(timeEntry);

        return TypedResults.Created($"/api/v1/time-entries/{timeEntry.Id}", resultModel);
    }

    private static async Task<Results<NotFound, Ok>> DeleteTimeEntry(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }

    private static async Task<Ok<PagedList<TimeEntryModel>>> GetTimeEntries(
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

        return TypedResults.Ok(new PagedList<TimeEntryModel>
        {
            Items = timeEntries.Select(TimeEntryModel.FromTimeEntry),
            Page = page,
            PageSize = size,
            TotalCount = await dbContext.TimeEntries!.CountAsync()
        });
    }
    private static async Task<Ok<TimeEntryModel[]>> GetTimeEntriesByUserAndMonth(
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

        return TypedResults.Ok(timeEntries
            .Select(TimeEntryModel.FromTimeEntry)
            .ToArray());
    }

    private static async Task<Results<NotFound, Ok<TimeEntryModel>>> GetTimeEntry(ILogger<Program> logger, long id, TimeTrackerDbContext dbContext)
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
    }

    private static async Task<Results<ValidationProblem, NotFound, Ok<TimeEntryModel>>> UpdateTimeEntry(
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
            return TypedResults.NotFound();
        }

        model.MapTo(timeEntry);

        dbContext.TimeEntries!.Update(timeEntry);
        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(TimeEntryModel.FromTimeEntry(timeEntry));
    }
}
