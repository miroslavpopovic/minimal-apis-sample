using Asp.Versioning;

namespace MinimalApis.MinimalSample.Extensions;

public static class WebApplicationExtensions
{
    public static void UseSwaggerUI(this WebApplication app, ApiVersion[] apiVersions)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.UseSwagger();
        app.UseSwaggerUI(
            options =>
            {
                foreach (var description in apiVersions)
                {
                    var url = $"/swagger/v{description.MajorVersion}/swagger.json";
                    var name = $"V{description.MajorVersion}";
                    options.SwaggerEndpoint(url, name);
                }
            });
    }
}
