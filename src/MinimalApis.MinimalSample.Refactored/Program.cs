using System.Threading.RateLimiting;
using Carter;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<TimeTrackerDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!));

builder.Services.AddDemoAuthorization();
builder.Services.AddDemoAuthentication();

builder.Services.AddOpenApi();

builder.Services.AddCors();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddCarter();

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

// https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/#ratelimiting-middleware
app.UseRateLimiter(new RateLimiterOptions
    {
        OnRejected = (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return new ValueTask();
        }
        //GlobalLimiter = new ConcurrencyLimiter(...)
    }
    .AddConcurrencyLimiter("get", new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 2))
    .AddNoLimiter("users")
    .AddPolicy("modify", context =>
    {
        // This is just a sample on how to use partitioning per request parameters
        if (!StringValues.IsNullOrEmpty(context.Request.Headers["token"]))
        {
            return RateLimitPartition.CreateTokenBucketLimiter("token", _ =>
                new TokenBucketRateLimiterOptions(5, QueueProcessingOrder.OldestFirst, 1, TimeSpan.FromSeconds(5), 1));
        }

        return RateLimitPartition.CreateFixedWindowLimiter("default", _ =>
            new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1, TimeSpan.FromSeconds(5)));
    }));

app.MapCarter();

app.Run();

// Necessary to make Program class accessible in tests
public partial class Program { }
