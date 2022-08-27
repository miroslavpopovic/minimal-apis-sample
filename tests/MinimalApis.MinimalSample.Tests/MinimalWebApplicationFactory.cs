using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalApis.MinimalSample.Data;
using MinimalApis.MinimalSample.Domain;

namespace MinimalApis.MinimalSample.Tests;

public class MinimalWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TimeTrackerDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Must initialize and open Sqlite connection in order to keep in-memory database tables
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<TimeTrackerDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var dbContext = scopedServices.GetRequiredService<TimeTrackerDbContext>();

            dbContext.Database.EnsureCreated();

            SeedTestData(dbContext);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection!.DisposeAsync();
    }

    private static void SeedTestData(TimeTrackerDbContext dbContext)
    {
        var users = new List<User>();
        for (var i = 0; i < 50; i++)
        {
            users.Add(new User
            {
                Name = $"Test user {i}",
                HourRate = 10 + i
            });
        }

        dbContext.Users!.AddRange(users);
        dbContext.SaveChanges();
    }
}
