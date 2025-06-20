using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Extensions;
using Russkyc.MinimalApi.Framework.Options;

namespace Russkyc.MinimalApi.Framework;

public static class MinimalApiFramework
{
    public static WebApplication CreateDefault(Action<DbContextOptionsBuilder> options)
    {
        FrameworkDbContextOptions.DbContextConfiguration = options;
        
        var builder = WebApplication
            .CreateBuilder();
        
        builder.Services
            .AddMinimalApiFramework();

        return builder.Build()
            .UseMinimalApiFramework();
    }
}