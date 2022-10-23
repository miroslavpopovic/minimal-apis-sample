using Carter;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Users;

public class UsersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users", GetUsers)
            .Produces<PagedList<UserModel>>()
            .WithName(nameof(GetUsers))
            .WithSummary("Get a paged list of users.")
            .WithDescription("Gets one page of the available users.")
            .WithTags("Users")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        app.MapGet("/api/v1/users/{id:long}", GetUser)
            .Produces<UserModel>()
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetUser))
            .WithSummary("Get a user by id.")
            .WithDescription("Gets a single user by id value.")
            .WithTags("Users")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to retrieve.";
                return operation;
            });

        app.MapDelete("/api/v1/users/{id:long}", DeleteUser)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(DeleteUser))
            .WithSummary("Delete a user by id.")
            .WithDescription("Deletes a single user by id value.")
            .WithTags("Users")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to delete.";
                return operation;
            });

        app.MapPost("/api/v1/users", CreateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .Produces<UserModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(CreateUser))
            .WithSummary("Create a new user.")
            .WithTags("Users")
            .WithDescription("Creates a new user with supplied values.")
            .WithOpenApi();

        app.MapPut("/api/v1/users/{id:long}", UpdateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .Produces<UserModel>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(UpdateUser))
            .WithSummary("Update a user by id.")
            .WithDescription("Updates a user with the given id, using the supplied data.")
            .WithTags("Users")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to update.";
                return operation;
            });
    }

    private static async Task<IResult> CreateUser(
        UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Creating a new user with name {Name}", model.Name);

        var user = new User();
        model.MapTo(user);

        await dbContext.Users!.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var resultModel = UserModel.FromUser(user);

        return Results.CreatedAtRoute(nameof(GetUser), new { id = user.Id }, resultModel);
    }

    private static async Task<IResult> DeleteUser(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }

    private static async Task<IResult> GetUser(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        return user == null
            ? Results.NotFound()
            : Results.Ok(UserModel.FromUser(user));
    }

    private static async Task<PagedList<UserModel>> GetUsers(
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

    private static async Task<IResult> UpdateUser(
        long id, UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }
}
