using Carter;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Clients;

public class ClientsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/clients", GetClients)
            .Produces<PagedList<ClientModel>>()
            .WithName(nameof(GetClients))
            .WithSummary("Get a paged list of clients.")
            .WithDescription("Gets one page of the available clients.")
            .WithTags("Clients")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        app.MapGet("/api/v1/clients/{id:long}", GetClient)
            .Produces<ClientModel>()
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetClient))
            .WithSummary("Get a client by id.")
            .WithDescription("Gets a single client by id value.")
            .WithTags("Clients")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to retrieve.";
                return operation;
            });

        app.MapDelete("/api/v1/clients/{id:long}", DeleteClient)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(DeleteClient))
            .WithSummary("Delete a client by id.")
            .WithDescription("Deletes a single client by id value.")
            .WithTags("Clients")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to delete.";
                return operation;
            });

        app.MapPost("/api/v1/clients", CreateClient)
            .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
            .Produces<ClientModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization("AdminPolicy")
            .WithName(nameof(CreateClient))
            .WithSummary("Create a new client.")
            .WithTags("Clients")
            .WithDescription("Creates a new client with supplied values.")
            .WithOpenApi(operation =>
            {
                operation.Parameters.RemoveAt(0);
                return operation;
            });

        app.MapPut("/api/v1/clients/{id:long}", UpdateClient)
            .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
            .Produces<ClientModel>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy")
            .WithName("UpdateClient")
            .WithSummary("Update a client by id.")
            .WithDescription("Updates a client with the given id, using the supplied data.")
            .WithTags("Clients")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to update.";
                operation.Parameters.RemoveAt(1);
                return operation;
            });
    }

    private static async Task<IResult> CreateClient(
        ILogger<Program> logger, ClientInputModel model, TimeTrackerDbContext dbContext)
    {
        logger.LogDebug("Creating a new client with name {Name}", model.Name);

        var client = new Client();
        model.MapTo(client);

        await dbContext.Clients!.AddAsync(client);
        await dbContext.SaveChangesAsync();

        var resultModel = ClientModel.FromClient(client);

        return Results.CreatedAtRoute(nameof(GetClient), new { id = client.Id }, resultModel);
    }

    private static async Task<IResult> DeleteClient(ILogger<Program> logger, long id, TimeTrackerDbContext dbContext)
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
    }

    private static async Task<IResult> GetClient(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        return client == null
            ? Results.NotFound()
            : Results.Ok(ClientModel.FromClient(client));
    }

    private static async Task<PagedList<ClientModel>> GetClients(
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

    private static async Task<IResult> UpdateClient(
        long id, ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }
}
