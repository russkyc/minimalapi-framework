using Microsoft.OpenApi.Models;

namespace SampleProject;

public static class BuilderExtensions
{
    public static void AddJwtAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddJwtBearer();

        services
            .AddAuthorization();

        services
            .AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme, Id = "bearerAuth"
                            }
                        },
                        new string[] { }
                    }
                });
            });
    }

    public static void UseJwtAuth(this IApplicationBuilder webApplication)
    {
        webApplication
            .UseAuthentication()
            .UseAuthorization();
    }
}