using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MiniValidation;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Core.Attributes;
using Russkyc.MinimalApi.Framework.Data;
using Russkyc.MinimalApi.Framework.Options;
using Russkyc.MinimalApi.Framework.Realtime;

namespace Russkyc.MinimalApi.Framework.Extensions;

public static class MinimalApiExtensions
{
    public static void MapRealtimeHub(this IEndpointRouteBuilder endpointBuilder, string endpoint)
    {
        endpointBuilder.MapHub<EventHub>(endpoint);
    }

    public static void MapEntityEndpoints<TEntity, TKeyType>(this IEndpointRouteBuilder endpointBuilder,
        string? groupName = null,
        Action<IEndpointConventionBuilder>? routeOptionsAction = null)
        where TEntity : class
    {
        var mapGroupName = groupName ?? typeof(TEntity).Name;
        var entityEndpointGroup = endpointBuilder.MapGroup($"{mapGroupName.ToLower()}");

        var getCollectionEndpoint = entityEndpointGroup
            .MapGet("/", GetCollectionHandler<TEntity>())
            .Produces<PaginatedCollection<TEntity>>()
            .Produces<IEnumerable<TEntity>>()
            .WithName($"AllowGet a {mapGroupName} collection")
            .WithTags(mapGroupName);
        var getSingleEntityEndpoint = entityEndpointGroup
            .MapGet("/{id}", GetSingleEntityHandler<TEntity, TKeyType>())
            .Produces<TEntity>()
            .WithDescription($"AllowGet a single {mapGroupName}")
            .WithTags(mapGroupName);
        var addEntityEndpoint = entityEndpointGroup
            .MapPost("/", AddEntityHandler<TEntity, TKeyType>(mapGroupName))
            .Produces<TEntity>()
            .WithDescription($"Add a single {mapGroupName}")
            .WithTags(mapGroupName);
        var updateEntityEndpoint = entityEndpointGroup
            .MapPatch("/", UpdateEntityHandler<TEntity>(mapGroupName))
            .Produces<TEntity>()
            .WithDescription($"Update a single {mapGroupName}")
            .WithTags(mapGroupName);
        var deleteEntityEndpoint = entityEndpointGroup
            .MapDelete("/{id}", DeleteEntityHandler<TEntity, TKeyType>(mapGroupName))
            .Produces<TEntity>()
            .WithDescription($"AllowDelete a single {mapGroupName}")
            .WithTags(mapGroupName);
        var addEntitiesEndpoint = entityEndpointGroup
            .MapPost("/batch", AddEntitiesHandler<TEntity, TKeyType>(mapGroupName))
            .Produces<IEnumerable<TEntity>>()
            .WithDescription($"Batch Insert {mapGroupName}")
            .WithTags(mapGroupName);
        var updateEntitiesEndpoint = entityEndpointGroup
            .MapPut("/batch", UpdateEntitiesHandler<TEntity>(mapGroupName))
            .Produces<IEnumerable<TEntity>>()
            .WithDescription($"Batch update {mapGroupName}")
            .WithTags(mapGroupName);
        var updateEntitiesWithFiltersEndpoint = entityEndpointGroup
            .MapPatch("/batch", UpdateEntitiesWithFiltersHandler<TEntity>(mapGroupName))
            .Produces<IEnumerable<TEntity>>()
            .WithDescription($"Batch update {mapGroupName} with filters and dynamic fields")
            .WithTags(mapGroupName);
        var deleteEntitiesEndpoint = entityEndpointGroup
            .MapDelete("/batch", DeleteEntitiesHandler<TEntity>(mapGroupName))
            .Produces<IEnumerable<TEntity>>()
            .WithDescription($"Batch delete {mapGroupName} based on query parameters")
            .WithTags(mapGroupName);
        
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

    private static bool HasPermission(HttpContext httpContext, string[]? requiredPermissions)
    {
        if (requiredPermissions == null) return true;
        var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
        return permissions.Any(permission => requiredPermissions.Any(permission.Equals));
    }

    private static async Task BroadcastCrudEvent(
        IHubContext<EventHub>? eventHub,
        RealtimeClientStore? realtimeClientStore,
        string[]? permissions,
        string eventType,
        object data,
        string resource)
    {
        if (eventHub is null || realtimeClientStore is null)
            return;

        var crudEvent = new CrudEvent
        {
            Type = eventType,
            Data = data,
            Resource = resource
        };

        if (permissions != null)
        {
            var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(permissions);
            await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", crudEvent);
        }
        else
        {
            await eventHub.Clients.All.SendAsync("crud-event", crudEvent);
        }
    }

    private static bool TryValidateEntity<TEntity>(TEntity entity, out IDictionary<string, string[]> errors)
    {
        return MiniValidator.TryValidate(entity, out errors);
    }

    private static Delegate GetCollectionHandler<TEntity>()
        where TEntity : class
    {
        return async (HttpContext httpContext,
            [FromServices] BaseDbContext context,
            [FromQuery] string? include,
            [FromQuery] string? filter,
            [FromQuery] string? property,
            [FromQuery] string? orderBy,
            [FromQuery] bool orderByDescending = false,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = int.MaxValue,
            [FromQuery] bool paginate = false) =>
        {
            try
            {
                var getAttribute = typeof(TEntity).GetAttributeValue<AllowGet>();
                if (getAttribute is not null && !HasPermission(httpContext, getAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var entities = dbSet.AsNoTracking();

                if (!string.IsNullOrEmpty(include))
                    entities = entities.ApplyIncludes(include);

                if (!string.IsNullOrEmpty(filter))
                    entities = entities.ApplyFilter(filter);

                if (!string.IsNullOrEmpty(orderBy))
                    entities = entities.ApplyOrdering(orderBy, orderByDescending);

                if (property is not null)
                    entities = entities.SelectProperties(property);

                if (paginate)
                {
                    var totalRecords = await entities.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                    var paginatedEntities = await entities
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var result = new PaginatedCollection<TEntity>
                    {
                        Data = paginatedEntities,
                        Page = page,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = totalPages
                    };

                    return Results.Ok(result);
                }
                else
                {
                    var result = await entities.ToListAsync();
                    return Results.Ok(result);
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate GetSingleEntityHandler<TEntity, TKeyType>()
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices] BaseDbContext context, [FromRoute] TKeyType id,
            [FromQuery] string? include,
            [FromQuery] string? property) =>
        {
            try
            {
                var getAttribute = typeof(TEntity).GetAttributeValue<AllowGet>();
                if (getAttribute is not null && !HasPermission(httpContext, getAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var query = dbSet
                    .AsNoTracking()
                    .ApplyIncludes(include)
                    .SelectProperties(property);
                var entity =
                    await query.FirstOrDefaultAsync(entity => ((IDbEntity<TKeyType>)entity).Id!.Equals(id));
                if (entity == null)
                    return Results.NotFound();

                return Results.Ok(entity);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate AddEntityHandler<TEntity, TKeyType>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromBody] TEntity entity) =>
        {
            try
            {
                var postAttribute = typeof(TEntity).GetAttributeValue<AllowPost>();
                if (postAttribute is not null && !HasPermission(httpContext, postAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();

                if (!TryValidateEntity(entity, out var errors))
                {
                    var validationError = new ValidationError
                    {
                        Message = $"Validation for {entity.GetType().Name} failed.",
                        Errors = errors
                    };
                    return Results.BadRequest(validationError);
                }

                var existingEntity = await dbSet.FindAsync(((IDbEntity<TKeyType>)entity).Id);
                if (existingEntity != null)
                    return Results.Conflict("An entity with the same key already exists.");

                var entryEntity = await dbSet.AddAsync(entity);
                await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, postAttribute?.Permission, "create", entryEntity.Entity, mapGroupName.ToLower());

                return Results.Ok(entryEntity.Entity);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate UpdateEntityHandler<TEntity>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromBody] TEntity entity) =>
        {
            try
            {
                var patchAttribute = typeof(TEntity).GetAttributeValue<AllowPatch>();
                if (patchAttribute is not null && !HasPermission(httpContext, patchAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var entryEntity = dbSet.Update(entity);
                await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, patchAttribute?.Permission, "update", entryEntity.Entity, mapGroupName.ToLower());

                return Results.Ok(entryEntity.Entity);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate DeleteEntityHandler<TEntity, TKeyType>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromRoute] TKeyType id) =>
        {
            try
            {
                var deleteAttribute = typeof(TEntity).GetAttributeValue<AllowDelete>();
                if (deleteAttribute is not null && !HasPermission(httpContext, deleteAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var entity = await dbSet.FindAsync(id);
                if (entity is null)
                    return Results.NotFound();

                dbSet.Remove(entity);
                await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, deleteAttribute?.Permission, "delete", entity, mapGroupName.ToLower());

                return Results.Ok(entity);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate AddEntitiesHandler<TEntity, TKeyType>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromBody] TEntity[] entities) =>
        {
            try
            {
                var postAttribute = typeof(TEntity).GetAttributeValue<AllowPost>();
                if (postAttribute is not null && !HasPermission(httpContext, postAttribute.Permission))
                    return Results.Unauthorized();

                foreach (var entity in entities)
                {
                    if (!TryValidateEntity(entity, out var errors))
                    {
                        var validationError = new ValidationError
                        {
                            Message = $"Validation for {entity.GetType().Name} failed.",
                            Errors = errors
                        };
                        return Results.BadRequest(validationError);
                    }
                }

                var dbSet = context.DbSet<TEntity>();
                var entityEntries = new List<TEntity>();
                foreach (var entity in entities)
                {
                    var existingEntity = await dbSet.FindAsync(((IDbEntity<TKeyType>)entity).Id);
                    if (existingEntity != null)
                        return Results.Conflict("An entity with the same key already exists.");

                    var entryEntity = await dbSet.AddAsync(entity);
                    entityEntries.Add(entryEntity.Entity);
                }

                await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, postAttribute?.Permission, "batch-create", entityEntries, mapGroupName.ToLower());

                return Results.Ok(entityEntries);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate UpdateEntitiesHandler<TEntity>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromBody] TEntity[] entities) =>
        {
            try
            {
                var putAttribute = typeof(TEntity).GetAttributeValue<AllowPut>();
                if (putAttribute is not null && !HasPermission(httpContext, putAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                dbSet.UpdateRange(entities);
                var result = await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, putAttribute?.Permission, "update", entities, mapGroupName.ToLower());

                return Results.Ok($"Updated {result} items");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate UpdateEntitiesWithFiltersHandler<TEntity>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
            [FromServices] BaseDbContext context,
            [FromQuery] string? filter, [FromBody] Dictionary<string, object> updateFields) =>
        {
            try
            {
                var patchAttribute = typeof(TEntity).GetAttributeValue<AllowPatch>();
                if (patchAttribute is not null && !HasPermission(httpContext, patchAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var entities = dbSet.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    try
                    {
                        entities = entities.ApplyFilter(filter);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                }

                var entityList = await entities.ToListAsync();

                foreach (var entity in entityList)
                {
                    foreach (var field in updateFields)
                    {
                        var property = typeof(TEntity).GetProperty(field.Key,
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null)
                        {
                            try
                            {
                                var jsonValue = JsonSerializer.Serialize(field.Value);
                                var convertedValue =
                                    JsonSerializer.Deserialize(jsonValue, property.PropertyType);
                                property.SetValue(entity, convertedValue);
                            }
                            catch (Exception)
                            {
                                return Results.BadRequest(
                                    $"Error setting property {field.Key} to {JsonSerializer.Serialize(field.Value)}");
                            }
                        }
                    }
                }

                dbSet.UpdateRange(entityList);
                var result = await context.SaveChangesAsync();

                await BroadcastCrudEvent(eventHub, realtimeClientStore, patchAttribute?.Permission, "batch-update", entityList, mapGroupName.ToLower());

                return Results.Ok($"Updated {result} items");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    private static Delegate DeleteEntitiesHandler<TEntity>(string mapGroupName)
        where TEntity : class
    {
        return async (
            HttpContext httpContext,
            [FromServices] RealtimeClientStore realtimeClientStore,
            [FromServices, Optional] IHubContext<EventHub>? eventHub,
            [FromServices] BaseDbContext context,
            [FromQuery] string? include,
            [FromQuery] string? filter) =>
        {
            try
            {
                var deleteAttribute = typeof(TEntity).GetAttributeValue<AllowDelete>();
                if (deleteAttribute is not null && !HasPermission(httpContext, deleteAttribute.Permission))
                    return Results.Unauthorized();

                var dbSet = context.DbSet<TEntity>();
                var entities = dbSet
                    .AsNoTracking()
                    .ApplyIncludes(include);

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    try
                    {
                        entities = entities.ApplyFilter(filter);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                }

                dbSet.RemoveRange(entities);
                var result = await context.SaveChangesAsync();

                if (eventHub is not null)
                {
                    await BroadcastCrudEvent(eventHub, realtimeClientStore, deleteAttribute?.Permission, "batch-delete", entities, mapGroupName.ToLower());
                }

                return Results.Ok($"Deleted {result} items");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e.Message);
            }
        };
    }

    public static void MapAllEntityEndpoints(this IEndpointRouteBuilder endpointBuilder, Assembly? assembly = null,
        Action<IEndpointConventionBuilder>? routeOptionsAction = null)
    {
        assembly ??= Assembly.GetEntryAssembly()!;

        var entityTypes = assembly
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)));

        foreach (var entityType in entityTypes)
        {
            var property = entityType.GetProperty("Id");
            var method = typeof(MinimalApiExtensions).GetMethod(nameof(MapEntityEndpoints))?
                .MakeGenericMethod(entityType, property!.PropertyType);
            method?.Invoke(null, [endpointBuilder, null, routeOptionsAction]);
        }
    }
}