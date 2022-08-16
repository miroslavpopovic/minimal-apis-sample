using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MinimalApis.MvcSample.Data;
using MinimalApis.MvcSample.Extensions;

namespace MinimalApis.MvcSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<TimeTrackerDbContext>(
                options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDemoAuthentication();

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            builder.Services.AddCors();

            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddVersioning();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LimitingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            // Demo purpose only! Restrict CORS in production.
            app.UseCors(policyBuilder => policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.MapControllers();

            app.Run();
        }
    }
}
