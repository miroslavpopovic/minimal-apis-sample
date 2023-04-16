using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
using Carter;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Extensions;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Extensions;

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

builder.Services.AddCarter();

ApiVersionSet versionSet = null!;
// ReSharper disable once AccessToModifiedClosure
builder.Services.AddSingleton(new Lazy<ApiVersionSet>(() => versionSet));

var app = builder.Build();

versionSet = app.NewApiVersionSet().HasApiVersions(apiVersions).Build();

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

app.MapCarter();

app.Run();

// Necessary to make Program class accessible in tests
public partial class Program { }
