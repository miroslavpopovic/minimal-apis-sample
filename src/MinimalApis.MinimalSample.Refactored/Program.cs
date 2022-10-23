using Carter;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MinimalSample.Refactored.Data;
using MinimalApis.MinimalSample.Refactored.Extensions;

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

app.UseRateLimiter();

app.MapCarter();

app.Run();

// Necessary to make Program class accessible in tests
public partial class Program { }
