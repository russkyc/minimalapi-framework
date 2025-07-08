using Russkyc.MinimalApi.Framework.Options;
using Scalar.AspNetCore;

namespace Russkyc.MinimalApi.Framework.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseMinimalApiFramework(
        this WebApplication webApplication, bool mapEntityEndpoints = true)
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

        if (FrameworkOptions.ApiPrefix != null)
        {
            var group = webApplication.MapGroup(FrameworkOptions.ApiPrefix);
            if (mapEntityEndpoints)
            {
                group.MapAllEntityEndpoints(FrameworkOptions.EntityClassesAssembly);
            }
        }
        else
        {
            if (mapEntityEndpoints)
            {
                webApplication.MapAllEntityEndpoints(FrameworkOptions.EntityClassesAssembly);
            }
        }

        if (FrameworkOptions.EnableRealtimeEvents)
        {
            webApplication.MapRealtimeHub(FrameworkRealtimeOptions.RealtimeEventsEndpoint);
        }

        if (FrameworkOptions.MapIndexToApiDocs)
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