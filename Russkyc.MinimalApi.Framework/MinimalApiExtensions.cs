using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Russkyc.MinimalApi.Framework;

public static class MinimalApiExtensions
{
    public static void AddEntityServices<TEntity>(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TEntity : class
    {
        serviceCollection.AddDbContext<EntityContext<TEntity>>(optionsAction, contextLifetime, optionsLifetime);
    }

    public static void AddAllEntityServices(this IServiceCollection serviceCollection, Assembly assembly,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
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
                method?.Invoke(null, [serviceCollection, optionsAction, contextLifetime, optionsLifetime]);
        }
    }

    public static void MapEntityEndpoints<TEntity, TKeyType>(this IEndpointRouteBuilder endpointBuilder,
        string? groupName = null,
        Action<IEndpointConventionBuilder>? routeOptionsAction = null)
        where TEntity : class
    {
        var mapGroupName = groupName ?? typeof(TEntity).Name;
        var entityEndpointGroup = endpointBuilder.MapGroup($"{mapGroupName.ToLower()}");

        var getCollectionEndpoint = entityEndpointGroup
            .MapGet("/",
                async ([FromServices] EntityContext<TEntity> context, [FromQuery] string? include,
                    [FromQuery] FilterDictionary? filters, [FromQuery] string? property) =>
                {
                    var entities = context.Entities
                        .AsNoTracking()
                        .ApplyIncludes(include);
                    if (filters is not null)
                    {
                        entities = entities.ApplyFilters(filters);
                    }

                    if (property is not null)
                    {
                        entities = entities.SelectProperties(property);
                    }

                    var result = await entities.ToListAsync();
                    return Results.Ok(result);
                })
            .WithName($"Get {mapGroupName} Collection")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var getSingleEntityEndpoint = entityEndpointGroup
            .MapGet("/{id}",
                async ([FromServices] EntityContext<TEntity> context, [FromRoute] TKeyType id,
                    [FromQuery] string? include,
                    [FromQuery] string? property) =>
                {
                    var query = context.Entities
                        .AsNoTracking()
                        .ApplyIncludes(include)
                        .SelectProperties(property);
                    var entity =
                        await query.FirstOrDefaultAsync(entity => ((IDbEntity<TKeyType>)entity).Id!.Equals(id));
                    if (entity == null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(entity);
                })
            .WithName($"Get {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntityEndpoint = entityEndpointGroup
            .MapPost("/", async ([FromServices] EntityContext<TEntity> context, [FromBody] TEntity entity) =>
            {
                var existingEntity = await context.Entities.FindAsync(((IDbEntity<TKeyType>)entity).Id);
                if (existingEntity != null)
                {
                    return Results.Conflict("An entity with the same key already exists.");
                }

                var entryEntity = await context.Entities
                    .AddAsync(entity);
                await context.SaveChangesAsync();
                return Results.Ok(entryEntity.Entity);
            })
            .WithName($"Add {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var updateEntityEndpoint = entityEndpointGroup
            .MapPatch("/", async ([FromServices] EntityContext<TEntity> context, [FromBody] TEntity entity) =>
            {
                var entryEntity = context.Entities
                    .Update(entity);
                await context.SaveChangesAsync();
                return Results.Ok(entryEntity.Entity);
            })
            .WithName($"Update {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var deleteEntityEndpoint = entityEndpointGroup
            .MapDelete("/{id}",
                async ([FromServices] EntityContext<TEntity> context, [FromRoute] TKeyType id) =>
                {
                    var entity = await context.Entities
                        .FindAsync(id);
                    if (entity is null)
                    {
                        return Results.NotFound();
                    }

                    context.Entities.Remove(entity);
                    await context.SaveChangesAsync();
                    return Results.Ok(entity);
                })
            .WithName($"Delete {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        routeOptionsAction?.Invoke(addEntityEndpoint);
        routeOptionsAction?.Invoke(getCollectionEndpoint);
        routeOptionsAction?.Invoke(getSingleEntityEndpoint);
        routeOptionsAction?.Invoke(updateEntityEndpoint);
        routeOptionsAction?.Invoke(deleteEntityEndpoint);
    }

    public static void MapAllEntityEndpoints<TId>(this IEndpointRouteBuilder endpointBuilder, Assembly assembly,
        Action<IEndpointConventionBuilder>? routeOptionsAction = null)
    {
        var entityTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)));

        foreach (var entityType in entityTypes)
        {
            var method = typeof(MinimalApiExtensions).GetMethod(nameof(MapEntityEndpoints))?
                .MakeGenericMethod(entityType, typeof(TId));
            method?.Invoke(null, [endpointBuilder, null, routeOptionsAction]);
        }
    }
}