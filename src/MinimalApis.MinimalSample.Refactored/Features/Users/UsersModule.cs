using Asp.Versioning.Builder;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Extensions;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Users;

public class UsersModule : ICarterModule
{
    private readonly Lazy<ApiVersionSet> _apiVersionSet;

    public UsersModule(Lazy<ApiVersionSet> apiVersionSet)
    {
        _apiVersionSet = apiVersionSet;
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var usersGroup = app.MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(_apiVersionSet.Value)
            .WithTags("Users");
        var usersAdminGroup = usersGroup.MapGroup("/")
            .RequireAuthorization("AdminPolicy");

        usersGroup
            .MapGet("/", GetUsers)
            .WithName(nameof(GetUsers))
            .WithOpenApi(
                "Get a paged list of users.", 
                "Gets one page of the available users.", 
                "Page number.", 
                "Page size.");

        usersGroup
            .MapGet("/{id:long}", GetUser)
            .WithName(nameof(GetUser))
            .WithOpenApi(
                "Get a user by id.", 
                "Gets a single user by id value.", 
                "");

        usersAdminGroup
            .MapDelete("/{id:long}", DeleteUser)
            .WithName(nameof(DeleteUser))
            .WithOpenApi(
                "Delete a user by id.", 
                "Deletes a single user by id value.",
                "Id of the user to delete.");

        usersAdminGroup
            .MapPost("/", CreateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .WithName(nameof(CreateUser))
            .WithOpenApi(
                "Create a new user.", 
                "Creates a new user with supplied values.");

        usersAdminGroup
            .MapPut("/{id:long}", UpdateUser)
            .AddEndpointFilter<ValidationFilter<UserInputModel>>()
            .WithName(nameof(UpdateUser))
            .WithOpenApi(
                "Update a user by id.", 
                "Updates a user with the given id, using the supplied data.",
                "Id of the user to update.");
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
