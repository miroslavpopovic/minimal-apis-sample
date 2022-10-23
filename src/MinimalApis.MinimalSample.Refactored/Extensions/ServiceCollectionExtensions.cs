using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FakeAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

namespace MinimalApis.MinimalSample.Refactored.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDemoAuthentication(this IServiceCollection services) =>
        services
            .AddAuthentication("FakeAuth")
            .AddFakeAuth(options =>
            {
                options.Claims.Add(new Claim(ClaimTypes.Name, "Demo User"));
                options.Claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            });

    public static void AddDemoAuthorization(this IServiceCollection services) =>
        services
            .AddAuthorization(options =>
            {
                // Configure Fallback policy to avoid adding .RequireAuthorization() to all endpoints
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                options.AddPolicy("AdminPolicy", builder => builder.RequireRole("Admin"));
            });

    public static void AddDemoRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return new ValueTask();
            };

            options
                .AddConcurrencyLimiter("get", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 2;
                    limiterOptions.QueueLimit = 2;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                })
                .AddPolicy("users", _ => RateLimitPartition.GetNoLimiter(string.Empty))
                .AddPolicy("modify", context => StringValues.IsNullOrEmpty(context.Request.Headers["token"])
                    ? RateLimitPartition.GetFixedWindowLimiter("default", _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            QueueLimit = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            PermitLimit = 1,
                            Window = TimeSpan.FromSeconds(5)
                        })
                    : RateLimitPartition.GetTokenBucketLimiter("token", _ =>
                        new TokenBucketRateLimiterOptions
                        {
                            QueueLimit = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            TokenLimit = 1,
                            TokensPerPeriod = 1,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(5)
                        }));
        });

    public static void AddOpenApi(this IServiceCollection services) =>
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "MinimalApis MinimalSample.Refactored",
                    Description = "An ASP.NET Core Minimal APIs refactored sample",
                    Contact = new OpenApiContact
                    {
                        Name = "Miroslav Popovic",
                        Url = new Uri("https://miroslavpopovic.com/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Sample license",
                        Url = new Uri("https://github.com/miroslavpopovic/minimal-apis-sample/blob/main/LICENSE")
                    }
                });

                options.DescribeAllParametersInCamelCase();
                var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
            });
}
