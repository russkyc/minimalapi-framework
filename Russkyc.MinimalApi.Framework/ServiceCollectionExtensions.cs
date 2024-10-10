using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework;

public static class ServiceCollectionExtensions
{
    public static void AddEntityServices<TEntity>(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
        DatabaseAction databaseAction = DatabaseAction.None) where TEntity : class
    {
        serviceCollection.AddDbContextFactory<EntityContext<TEntity>>(optionsAction, serviceLifetime);

        try
        {
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<EntityContext<TEntity>>>();
            using var context = contextFactory.CreateDbContext();

            switch (databaseAction)
            {
                case DatabaseAction.EnsureCreated:
                    context.CreateDatabase();
                    break;
                case DatabaseAction.DeleteAndCreate:
                    context.DeleteDatabase();
                    context.CreateDatabase();
                    break;
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to perform database schema updates.");
        }
    }

    public static void AddAllEntityServices(this IServiceCollection serviceCollection, Assembly assembly,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton,
        DatabaseAction databaseAction = DatabaseAction.None)
    {
        var entityTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)));

        foreach (var entityType in entityTypes)
        {
            var method = typeof(MinimalApiExtensions).GetMethod(nameof(AddEntityServices))?
                .MakeGenericMethod(entityType);
            if (optionsAction != null)
                method?.Invoke(null, [serviceCollection, optionsAction, contextLifetime, databaseAction]);
        }
    }

}