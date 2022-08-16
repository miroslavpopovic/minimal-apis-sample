using System.Reflection;
using System.Security.Claims;
using FakeAuth;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;

namespace MinimalApis.MvcSample.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDemoAuthentication(this IServiceCollection services) =>
            services
                .AddAuthentication()
                .AddFakeAuth(options =>
                {
                    options.Claims.Add(new Claim(ClaimTypes.Name, "Demo User"));
                    options.Claims.Add(new Claim(ClaimTypes.Role, "Admin"));
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
                        Title = "MinimalApis MvcSample",
                        Description = "A regular non-minimal ASP.NET Core MVC sample",
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
                    options.SwaggerDoc("v2", new OpenApiInfo
                    {
                        Version = "v2",
                        Title = "MinimalApis MvcSample",
                        Description = "A regular non-minimal ASP.NET Core MVC sample",
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

                    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
                });

        public static void AddVersioning(this IServiceCollection services) =>
            services
                .AddApiVersioning(
                    options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.ReportApiVersions = true;
                        options.ApiVersionReader = new UrlSegmentApiVersionReader();
                    })
                .AddVersionedApiExplorer(
                    options =>
                    {
                        options.GroupNameFormat = "'v'VVV";
                        options.SubstitutionFormat = "VVV";
                        options.SubstituteApiVersionInUrl = true;
                        options.ApiVersionParameterSource = new UrlSegmentApiVersionReader();
                    });
    }
}
