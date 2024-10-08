using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

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
            .WithName($"Get a {mapGroupName} collection")
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
            .WithDescription($"Get a single {mapGroupName}")
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
            .WithDescription($"Add a single {mapGroupName}")
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
            .WithDescription($"Update a single {mapGroupName}")
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
            .WithDescription($"Delete a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntitiesEndpoint = entityEndpointGroup
            .MapPost("/batch", async ([FromServices] EntityContext<TEntity> context, [FromBody] TEntity[] entities) =>
            {
                var entityEntries = new List<TEntity>();
                foreach (var entity in entities)
                {
                    var existingEntity = await context.Entities.FindAsync(((IDbEntity<TKeyType>)entity).Id);
                    if (existingEntity != null)
                    {
                        return Results.Conflict("An entity with the same key already exists.");
                    }

                    var entryEntity = await context.Entities
                        .AddAsync(entity);
                    entityEntries.Add(entryEntity.Entity);
                }

                await context.SaveChangesAsync();
                return Results.Ok(entityEntries);
            })
            .WithDescription($"Batch Insert {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var updateEntitiesEndpoint = entityEndpointGroup
            .MapPut("/batch", async ([FromServices] EntityContext<TEntity> context, [FromBody] TEntity[] entities) =>
            {
                context.Entities
                    .UpdateRange(entities);
                var result = await context.SaveChangesAsync();
                return Results.Ok($"Updated {result} items");
            })
            .WithDescription($"Batch update {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();
        
        var updateEntitiesWithFiltersEndpoint = entityEndpointGroup
        .MapPatch("/batch",
            async ([FromServices] EntityContext<TEntity> context, [FromQuery] FilterDictionary? filters, [FromBody] Dictionary<string, object> updateFields) =>
            {
                var entities = context.Entities.AsQueryable();

                if (filters is not null)
                {
                    entities = entities.ApplyFilters(filters);
                }

                var entityList = await entities.ToListAsync();

                foreach (var entity in entityList)
                {
                    foreach (var field in updateFields)
                    {
                        var property = typeof(TEntity).GetProperty(field.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null)
                        {
                            try
                            {
                                var jsonValue = JsonSerializer.Serialize(field.Value);
                                var convertedValue = JsonSerializer.Deserialize(jsonValue, property.PropertyType);
                                property.SetValue(entity, convertedValue);
                            }
                            catch (Exception)
                            {
                                return Results.BadRequest($"Error setting property {field.Key} to {JsonSerializer.Serialize(field.Value)}");
                            }
                        }
                    }
                }

                context.Entities.UpdateRange(entityList);
                var result = await context.SaveChangesAsync();
                return Results.Ok($"Updated {result} items");
            })
        .WithDescription($"Batch update {mapGroupName} with filters and dynamic fields")
        .WithTags(mapGroupName)
        .WithOpenApi();

        var deleteEntitiesEndpoint = entityEndpointGroup
            .MapDelete("/batch",
                async ([FromServices] EntityContext<TEntity> context, [FromQuery] string? include,
                    [FromQuery] FilterDictionary? filters) =>
                {
                    var entities = context.Entities
                        .AsNoTracking()
                        .ApplyIncludes(include);

                    if (filters is not null)
                    {
                        entities = entities.ApplyFilters(filters);
                    }

                    context.Entities.RemoveRange(entities);
                    var result = await context.SaveChangesAsync();
                    return Results.Ok($"Deleted {result} items");
                })
            .WithDescription($"Batch delete {mapGroupName} based on query parameters")
            .WithTags(mapGroupName)
            .WithOpenApi();

        routeOptionsAction?.Invoke(addEntityEndpoint);
        routeOptionsAction?.Invoke(getCollectionEndpoint);
        routeOptionsAction?.Invoke(getSingleEntityEndpoint);
        routeOptionsAction?.Invoke(updateEntityEndpoint);
        routeOptionsAction?.Invoke(deleteEntityEndpoint);

        routeOptionsAction?.Invoke(addEntitiesEndpoint);
        routeOptionsAction?.Invoke(updateEntitiesEndpoint);
        routeOptionsAction?.Invoke(updateEntitiesWithFiltersEndpoint);
        routeOptionsAction?.Invoke(deleteEntitiesEndpoint);
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