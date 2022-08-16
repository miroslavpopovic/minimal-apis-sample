namespace MinimalApis.MvcSample.Extensions;

public static class WebApplicationExtensions
{
    public static void UseOpenApi(this WebApplication app) =>
        app
            .UseSwagger()
            .UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("v1/swagger.json", "MinimalApis.MvcSample v1");
                options.SwaggerEndpoint("v2/swagger.json", "MinimalApis.MvcSample v2");
            });
}
