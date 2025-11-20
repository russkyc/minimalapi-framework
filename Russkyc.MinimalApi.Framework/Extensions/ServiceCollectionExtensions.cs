using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Data;
using Russkyc.MinimalApi.Framework.Options;
using Russkyc.MinimalApi.Framework.Realtime;
using System.Text;

namespace Russkyc.MinimalApi.Framework.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddMinimalApiFramework(this IServiceCollection serviceCollection)
    {
        FrameworkOptions.EntityClassesAssembly ??= Assembly.GetEntryAssembly()!;
        
        var entityTypes = FrameworkOptions.EntityClassesAssembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
            .ToList();

        var executingAssembly = Assembly.GetExecutingAssembly();
        var baseDbContextType = DbContextBuilder.CreateDynamicDbContext(entityTypes, executingAssembly.GetName());

        var optionsBuilder = new DbContextOptionsBuilder();
        FrameworkDbContextOptions.DbContextConfiguration?.Invoke(optionsBuilder);
        
        var dbContextOptions = optionsBuilder.Options;

        serviceCollection.Add(new ServiceDescriptor(
            typeof(DbContextOptions),
            _ => dbContextOptions,
            FrameworkDbContextOptions.DbContextOptionsLifetime));

        serviceCollection.Add(new ServiceDescriptor(
            typeof(BaseDbContext),
            _ => Activator.CreateInstance(FrameworkDbContextOptions.DbContextType ?? baseDbContextType, dbContextOptions)!,
            FrameworkDbContextOptions.DbContextLifetime));

        serviceCollection.AddSingleton<RealtimeClientStore>();
        
        if (FrameworkOptions.EnableApiDocs)
        {
            serviceCollection.AddEndpointsApiExplorer();
            serviceCollection.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("ApiPermissions", new OpenApiSecurityScheme
                {
                    Description = "Contains the permissions required to access permission-protected endpoints",
                    Name = ConfigurationStrings.ApiPermissionHeader,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "vxx,vsg,wrw,ttw"
                });
                if (FrameworkOptions.EnableJwtAuthentication)
                {
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey
                    });
                }
            });
        }

        if (FrameworkOptions.EnableRealtimeEvents)
        {
            serviceCollection.AddSignalR();
        }

        if (FrameworkOptions.EnableJwtAuthentication)
        {
            serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = FrameworkOptions.JwtIssuer,
                        ValidAudience = FrameworkOptions.JwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FrameworkOptions.JwtKey ?? throw new InvalidOperationException("JwtKey must be set when EnableJwtAuthentication is true")))
                    };
                });
            serviceCollection.AddAuthorization();
        }

        if (FrameworkOptions.EnableCookieAuthentication)
        {
            serviceCollection.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();
            serviceCollection.AddAuthorization();
        }

        try
        {
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BaseDbContext>();

            switch (FrameworkDbContextOptions.DatabaseAction)
            {
                case DatabaseAction.EnsureCreated:
                    context.Database.EnsureCreated();
                    Console.WriteLine("EnsureCreated ran successfully");
                    break;
                case DatabaseAction.DeleteAndCreate:
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                    Console.WriteLine("EnsureDeleted ran successfully");
                    break;
                case DatabaseAction.ApplyPendingMigrations:
                    var pendingMigrations = context.Database.GetPendingMigrations();
                    if (pendingMigrations.Any())
                    {
                        context.Database.Migrate();
                        Console.WriteLine("Pending migrations applied successfully");
                    }

                    break;
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to perform database schema updates.");
        }
    }

}