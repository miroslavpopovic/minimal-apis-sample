﻿using Asp.Versioning.Builder;
using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Features.Common;

namespace MinimalApis.MinimalSample.Refactored.Features.Clients;

public class ClientsModule : ICarterModule
{
    private readonly Lazy<ApiVersionSet> _apiVersionSet;

    public ClientsModule(Lazy<ApiVersionSet> apiVersionSet)
    {
        _apiVersionSet = apiVersionSet;
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var clientsGroup = app.MapGroup("/api/v{version:apiVersion}/clients")
            .WithApiVersionSet(_apiVersionSet.Value)
            .WithTags("Clients");
        var clientsAdminGroup = clientsGroup.MapGroup("/")
            .RequireAuthorization("AdminPolicy");

        clientsGroup
            .MapGet("/", GetClients)
            .WithName(nameof(GetClients))
            .WithSummary("Get a paged list of clients.")
            .WithDescription("Gets one page of the available clients.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Page number.";
                operation.Parameters[1].Description = "Page size.";
                return operation;
            });

        clientsGroup
            .MapGet("/{id:long}", GetClient)
            .WithName(nameof(GetClient))
            .WithSummary("Get a client by id.")
            .WithDescription("Gets a single client by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to retrieve.";
                return operation;
            });

        clientsAdminGroup
            .MapDelete("/{id:long}", DeleteClient)
            .WithName(nameof(DeleteClient))
            .WithSummary("Delete a client by id.")
            .WithDescription("Deletes a single client by id value.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to delete.";
                return operation;
            });

        clientsAdminGroup
            .MapPost("/", CreateClient)
            .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
            .WithName(nameof(CreateClient))
            .WithSummary("Create a new client.")
            .WithDescription("Creates a new client with supplied values.")
            .WithOpenApi();

        clientsAdminGroup
            .MapPut("/{id:long}", UpdateClient)
            .AddEndpointFilter<ValidationFilter<ClientInputModel>>()
            .WithName("UpdateClient")
            .WithSummary("Update a client by id.")
            .WithDescription("Updates a client with the given id, using the supplied data.")
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Description = "Id of the client to update.";
                return operation;
            });
    }

    private static async Task<Results<ValidationProblem, Created<ClientModel>>> CreateClient(
        ILogger<Program> logger, ClientInputModel model, TimeTrackerDbContext dbContext)
    {
        logger.LogDebug("Creating a new client with name {Name}", model.Name);

        var client = new Client();
        model.MapTo(client);

        await dbContext.Clients!.AddAsync(client);
        await dbContext.SaveChangesAsync();

        var resultModel = ClientModel.FromClient(client);

        return TypedResults.Created($"/api/v1/clients/{client.Id}", resultModel);
    }

    private static async Task<Results<NotFound, Ok>> DeleteClient(
        ILogger<Program> logger, long id, TimeTrackerDbContext dbContext)
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
    }

    private static async Task<Results<NotFound, Ok<ClientModel>>> GetClient(
        long id, TimeTrackerDbContext dbContext, ILogger<Program> logger)
    {
        logger.LogDebug("Getting a client with id {Id}", id);

        var client = await dbContext.Clients!.FindAsync(id);

        return client == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ClientModel.FromClient(client));
    }

    private static async Task<Ok<PagedList<ClientModel>>> GetClients(
        TimeTrackerDbContext dbContext, ILogger<Program> logger, int page = 1, int size = 5)
    {
        logger.LogDebug("Getting a page {Page} of clients with page size {Size}", page, size);

        var clients = await dbContext.Clients!
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return TypedResults.Ok(new PagedList<ClientModel>
        {
            Items = clients.Select(ClientModel.FromClient),
            Page = page,
            PageSize = size,
            TotalCount = await dbContext.Clients!.CountAsync()
        });
    }

    private static async Task<Results<ValidationProblem, NotFound, Ok<ClientModel>>> UpdateClient(
        long id, ClientInputModel model, TimeTrackerDbContext dbContext, ILogger<Program> logger)
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
    }
}
