using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework;

public static class ServiceCollectionExtensions
{
    public static void AddDbContextService(this IServiceCollection serviceCollection,
        Assembly assembly,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped,
        DatabaseAction databaseAction = DatabaseAction.EnsureCreated)
    {
        var entityTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
            .ToList();

        var executingAssembly = Assembly.GetExecutingAssembly();
        var baseDbContextType = DbContextBuilder.CreateDynamicDbContext(entityTypes, executingAssembly.GetName());

        var optionsBuilder = new DbContextOptionsBuilder();
        optionsAction?.Invoke(optionsBuilder);
        var dbContextOptions = optionsBuilder.Options;

        serviceCollection.Add(new ServiceDescriptor(
            typeof(DbContextOptions),
            _ => dbContextOptions,
            optionsLifetime));

        serviceCollection.Add(new ServiceDescriptor(
            typeof(BaseDbContext),
            _ => Activator.CreateInstance(baseDbContextType, dbContextOptions)!,
            contextLifetime));

        try
        {
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BaseDbContext>();

            switch (databaseAction)
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

    public static void AddDbContextService<T>(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped,
        DatabaseAction databaseAction = DatabaseAction.EnsureCreated) where T : BaseDbContext
    {
        serviceCollection.AddDbContext<BaseDbContext, T>(optionsAction, contextLifetime, optionsLifetime);

        try
        {
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BaseDbContext>();

            switch (databaseAction)
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