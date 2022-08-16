using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MinimalApis.MvcSample;

// Alternatively, you can use https://github.com/stefanprodan/AspNetCoreRateLimit
public class LimitingMiddleware
{
    private static readonly IDictionary<string, DateTime> TokenAccess = new Dictionary<string, DateTime>();

    private readonly RequestDelegate _next;
    private readonly ILogger<LimitingMiddleware> _logger;

    public LimitingMiddleware(RequestDelegate next, ILogger<LimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.HasValue ? request.Path.Value! : string.Empty;

        if (path.ToLowerInvariant().Contains("/api/"))
        {
            var token = request
                .Headers["Authorization"]
                .FirstOrDefault()?
                .ToLowerInvariant()
                .Replace("bearer ", "") ?? string.Empty;

            if (!TokenAccess.ContainsKey(token))
                TokenAccess.Add(token, DateTime.UtcNow);
            else
            {
                var lastAccess = TokenAccess[token];
                TokenAccess[token] = DateTime.UtcNow;

                if (lastAccess.AddSeconds(1) >= DateTime.UtcNow)
                {
                    const string message = "Token limit reached, operation cancelled";

                    _logger.LogInformation(message);

                    var problem = new ProblemDetails
                    {
                        Type = "https://yourdomain.com/errors/limit-reached",
                        Title = "Limit reached",
                        Detail = message,
                        Instance = "",
                        Status = StatusCodes.Status429TooManyRequests
                    };

                    var result = JsonSerializer.Serialize(
                        problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(result);

                    return;
                }
            }
        }

        await _next(context);
    }
}
