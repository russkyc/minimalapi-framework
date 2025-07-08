using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Options;
using Russkyc.MinimalApi.Framework.Extensions;

namespace Russkyc.MinimalApi.Framework;

public static class MinimalApiFramework
{
    public static WebApplication CreateDefault(Action<DbContextOptionsBuilder> options,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureWebApp = null)
    {
        FrameworkDbContextOptions.DbContextConfiguration = options;
        
        var builder = WebApplication
            .CreateBuilder();
        
        configureServices?.Invoke(builder.Services);
        
        builder.Services
            .AddMinimalApiFramework();

        return builder.Build()
            .UseMinimalApiFramework();
    }
}