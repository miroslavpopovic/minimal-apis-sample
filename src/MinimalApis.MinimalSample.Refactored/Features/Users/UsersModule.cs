using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Users;

public class UsersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var usersGroup = app.MapGroup("/api/v1/users")
            .WithTags("Users");
        var usersAdminGroup = app.MapGroup("/")
            .RequireAuthorization("AdminPolicy");

        usersGroup
            .MapGet("/", GetUsers)
            .WithName(nameof(GetUsers))
            .WithSummary("Get a paged list of users.")
            .WithDescription("Gets one page of the available users.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        usersGroup
            .MapGet("/{id:long}", GetUser)
            .WithName(nameof(GetUser))
            .WithSummary("Get a user by id.")
            .WithDescription("Gets a single user by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to retrieve.";
                return operation;
            });

        usersAdminGroup
            .MapDelete("/{id:long}", DeleteUser)
            .WithName(nameof(DeleteUser))
            .WithSummary("Delete a user by id.")
            .WithDescription("Deletes a single user by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to delete.";
                return operation;
            });

        usersAdminGroup
            .MapPost("/", CreateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .WithName(nameof(CreateUser))
            .WithSummary("Create a new user.")
            .WithDescription("Creates a new user with supplied values.")
            .WithOpenApi();

        usersAdminGroup
            .MapPut("/{id:long}", UpdateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .WithName(nameof(UpdateUser))
            .WithSummary("Update a user by id.")
            .WithDescription("Updates a user with the given id, using the supplied data.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the user to update.";
                return operation;
            });
    }

    private static async Task<Results<ValidationProblem, Created<UserModel>>> CreateUser(
        UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Creating a new user with name {Name}", model.Name);

        var user = new User();
        model.MapTo(user);

        await dbContext.Users!.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var resultModel = UserModel.FromUser(user);

        return TypedResults.Created($"/api/v1/users/{user.Id}", resultModel);
    }

    private static async Task<Results<NotFound, Ok>> DeleteUser(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }

    private static async Task<Results<NotFound, Ok<UserModel>>> GetUser(long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a user with id {Id}", id);

        var user = await dbContext.Users!.FindAsync(id);

        return user == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(UserModel.FromUser(user));
    }

    private static async Task<Ok<PagedList<UserModel>>> GetUsers(
        TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
    {
        logger.LogDebug("Getting a page {Page} of users with page size {Size}", page, size);

        var users = await dbContext.Users!
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return TypedResults.Ok(new PagedList<UserModel>
        {
            Items = users.Select(UserModel.FromUser),
            Page = page,
            PageSize = size,
            TotalCount = await dbContext.Users!.CountAsync()
        });
    }

    private static async Task<Results<ValidationProblem, NotFound, Ok<UserModel>>> UpdateUser(
        long id, UserInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }
}
