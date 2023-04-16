using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FakeAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using MinimalApis.MinimalSample.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MinimalApis.MinimalSample.Refactored.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApiVersioning(this IServiceCollection services, ApiVersion defaultVersion) =>
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = defaultVersion;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

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
            .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
            .AddSwaggerGen(options => options.OperationFilter<SwaggerDefaultValues>());
}
