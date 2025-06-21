using Russkyc.MinimalApi.Framework.Server.Options;
using Scalar.AspNetCore;

namespace Russkyc.MinimalApi.Framework.Server.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseMinimalApiFramework(
        this WebApplication webApplication)
    {
        if (FrameworkOptions.EnableApiDocs)
        {
            webApplication.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
            webApplication.MapScalarApiReference(options =>
            {
                options.WithSidebar(FrameworkApiDocsOptions.EnableSidebar);
                options.WithLayout(FrameworkApiDocsOptions.Layout);
                options.WithTheme(FrameworkApiDocsOptions.Theme);
            });
        }

        if (FrameworkOptions.UseHttpsRedirection)
        {
            webApplication.UseHttpsRedirection();
        }

        if (FrameworkOptions.ApiPrefix != null)
        {
            webApplication.MapGroup(FrameworkOptions.ApiPrefix)
                .MapAllEntityEndpoints(FrameworkOptions.EntityClassesAssembly);
        }
        else
        {
            webApplication.MapAllEntityEndpoints();
        }

        if (FrameworkOptions.EnableRealtimeEvents)
        {
            webApplication.MapRealtimeHub(FrameworkRealtimeOptions.RealtimeEventsEndpoint);
        }

        if (FrameworkOptions.AutoRedirectToApiDocs)
        {
            webApplication.MapGet("/", context =>
            {
                context.Response.Redirect("/scalar");
                return Task.CompletedTask;
            });
        }

        return webApplication;
    }
}