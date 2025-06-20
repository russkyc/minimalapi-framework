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
            .MapGet("/",
                async (HttpContext httpContext,
                    [FromServices] BaseDbContext context,
                    [FromQuery] string? include,
                    [FromQuery] string? filter,
                    [FromQuery] string? property,
                    [FromQuery] string? orderBy,
                    [FromQuery] bool orderByDescending = false,
                    [FromQuery] int page = 1,
                    [FromQuery] int pageSize = 10,
                    [FromQuery] bool paginate = false) =>
                {
                    try
                    {
                        var getAttribute = typeof(TEntity).GetAttributeValue<AllowGet>();

                        if (getAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    getAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }

                        var dbSet = context.DbSet<TEntity>();

                        var entities = dbSet.AsNoTracking();

                        if (!string.IsNullOrEmpty(include))
                        {
                            entities = entities.ApplyIncludes(include);
                        }

                        if (!string.IsNullOrEmpty(filter))
                        {
                            entities = entities.ApplyFilter(filter);
                        }

                        if (!string.IsNullOrEmpty(orderBy))
                        {
                            entities = entities.ApplyOrdering(orderBy, orderByDescending);
                        }

                        if (property is not null)
                        {
                            entities = entities.SelectProperties(property);
                        }

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
                })
            .WithName($"AllowGet a {mapGroupName} collection")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var getSingleEntityEndpoint = entityEndpointGroup
            .MapGet("/{id}",
                async (
                    HttpContext httpContext,
                    [FromServices] BaseDbContext context, [FromRoute] TKeyType id,
                    [FromQuery] string? include,
                    [FromQuery] string? property) =>
                {
                    try
                    {
                        var getAttribute = typeof(TEntity).GetAttributeValue<AllowGet>();

                        if (getAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    getAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }

                        var dbSet = context.DbSet<TEntity>();

                        var query = dbSet
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
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"AllowGet a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntityEndpoint = entityEndpointGroup
            .MapPost("/",
                async (
                    HttpContext httpContext,
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                    [FromServices] BaseDbContext context,
                    [FromBody] TEntity entity) =>
                {
                    try
                    {
                        var postAttribute = typeof(TEntity).GetAttributeValue<AllowPost>();

                        if (postAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    postAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }

                        var dbSet = context.DbSet<TEntity>();

                        var isValid = MiniValidator.TryValidate(entity, out var errors);

                        if (!isValid)
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
                        {
                            return Results.Conflict("An entity with the same key already exists.");
                        }

                        var entryEntity = await dbSet.AddAsync(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null && realtimeClientStore is not null)
                        {
                            if (postAttribute?.Permission != null)
                            {
                                var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(postAttribute.Permission);
                                await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "create",
                                    Data = entryEntity.Entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                            else
                            {
                                await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "create",
                                    Data = entryEntity.Entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                        }

                        return Results.Ok(entryEntity.Entity);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"Add a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var updateEntityEndpoint = entityEndpointGroup
            .MapPatch("/",
                async (
                    HttpContext httpContext,
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                    [FromServices] BaseDbContext context,
                    [FromBody] TEntity entity) =>
                {
                    try
                    {
                        var patchAttribute = typeof(TEntity).GetAttributeValue<AllowPatch>();
                        if (patchAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    patchAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }
                        var dbSet = context.DbSet<TEntity>();
                        var entryEntity = dbSet.Update(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null && realtimeClientStore is not null)
                        {
                            if (patchAttribute?.Permission != null)
                            {
                                var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(patchAttribute.Permission);
                                await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "update",
                                    Data = entryEntity.Entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                            else
                            {
                                await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "update",
                                    Data = entryEntity.Entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                        }

                        return Results.Ok(entryEntity.Entity);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"Update a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var deleteEntityEndpoint = entityEndpointGroup
            .MapDelete("/{id}",
                async (
                    HttpContext httpContext,
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                    [FromServices] BaseDbContext context,
                    [FromRoute] TKeyType id) =>
                {
                    try
                    {
                        var deleteAttribute = typeof(TEntity).GetAttributeValue<AllowDelete>();
                        if (deleteAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    deleteAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }
                        var dbSet = context.DbSet<TEntity>();

                        var entity = await dbSet.FindAsync(id);
                        if (entity is null)
                        {
                            return Results.NotFound();
                        }

                        dbSet.Remove(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null && realtimeClientStore is not null)
                        {
                            if (deleteAttribute?.Permission != null)
                            {
                                var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(deleteAttribute.Permission);
                                await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "delete",
                                    Data = entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                            else
                            {
                                await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "delete",
                                    Data = entity,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                        }

                        return Results.Ok(entity);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"AllowDelete a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntitiesEndpoint = entityEndpointGroup
            .MapPost("/batch", async (
                HttpContext httpContext,
                [FromServices, Optional] IHubContext<EventHub>? eventHub,
                [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                [FromServices] BaseDbContext context,
                [FromBody] TEntity[] entities) =>
            {
                try
                {
                    var postAttribute = typeof(TEntity).GetAttributeValue<AllowPost>();
                    if (postAttribute is not null)
                    {
                        var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                        if (!permissions.Any(permission =>
                                postAttribute.Permission.Any(permission.Equals)))
                        {
                            return Results.Unauthorized();
                        }
                    }
                    foreach (var entity in entities)
                    {
                        var isValid = MiniValidator.TryValidate(entity, out var errors);

                        if (!isValid)
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
                        {
                            return Results.Conflict("An entity with the same key already exists.");
                        }

                        var entryEntity = await dbSet.AddAsync(entity);
                        entityEntries.Add(entryEntity.Entity);
                    }

                    await context.SaveChangesAsync();

                    if (eventHub is not null && realtimeClientStore is not null)
                    {
                        if (postAttribute?.Permission != null)
                        {
                            var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(postAttribute.Permission);
                            await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                            {
                                Type = "batch-create",
                                Data = entityEntries,
                                Resource = mapGroupName.ToLower()
                            });
                        }
                        else
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "batch-create",
                                Data = entityEntries,
                                Resource = mapGroupName.ToLower()
                            });
                        }
                    }

                    return Results.Ok(entityEntries);
                }
                catch (Exception e)
                {
                    return Results.BadRequest(e.Message);
                }
            })
            .WithDescription($"Batch Insert {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var updateEntitiesEndpoint = entityEndpointGroup
            .MapPut("/batch", async (
                HttpContext httpContext,
                [FromServices, Optional] IHubContext<EventHub>? eventHub,
                [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                [FromServices] BaseDbContext context,
                [FromBody] TEntity[] entities) =>
            {
                try
                {
                    var putAttribute = typeof(TEntity).GetAttributeValue<AllowPut>();
                    if (putAttribute is not null)
                    {
                        var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                        if (!permissions.Any(permission =>
                                putAttribute.Permission.Any(permission.Equals)))
                        {
                            return Results.Unauthorized();
                        }
                    }
                    var dbSet = context.DbSet<TEntity>();

                    dbSet.UpdateRange(entities);
                    var result = await context.SaveChangesAsync();

                    if (eventHub is not null && realtimeClientStore is not null)
                    {
                        if (putAttribute?.Permission != null)
                        {
                            var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(putAttribute.Permission);
                            await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                            {
                                Type = "update",
                                Data = entities,
                                Resource = mapGroupName.ToLower()
                            });
                        }
                        else
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "update",
                                Data = entities,
                                Resource = mapGroupName.ToLower()
                            });
                        }
                    }

                    return Results.Ok($"Updated {result} items");
                }
                catch (Exception e)
                {
                    return Results.BadRequest(e.Message);
                }
            })
            .WithDescription($"Batch update {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var updateEntitiesWithFiltersEndpoint = entityEndpointGroup
            .MapPatch("/batch",
                async (
                    HttpContext httpContext,
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices, Optional] RealtimeClientStore? realtimeClientStore,
                    [FromServices] BaseDbContext context,
                    [FromQuery] string? filter, [FromBody] Dictionary<string, object> updateFields) =>
                {
                    try
                    {
                        var patchAttribute = typeof(TEntity).GetAttributeValue<AllowPatch>();
                        if (patchAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    patchAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }
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

                        if (eventHub is not null && realtimeClientStore is not null)
                        {
                            if (patchAttribute?.Permission != null)
                            {
                                var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(patchAttribute.Permission);
                                await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "batch-update",
                                    Data = entityList,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                            else
                            {
                                await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "batch-update",
                                    Data = entityList,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                        }

                        return Results.Ok($"Updated {result} items");
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"Batch update {mapGroupName} with filters and dynamic fields")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var deleteEntitiesEndpoint = entityEndpointGroup
            .MapDelete("/batch",
                async (
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
                        if (deleteAttribute is not null)
                        {
                            var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
                            if (!permissions.Any(permission =>
                                    deleteAttribute.Permission.Any(permission.Equals)))
                            {
                                return Results.Unauthorized();
                            }
                        }
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
                            if (deleteAttribute?.Permission != null)
                            {
                                var unauthorizedClients = realtimeClientStore.GetClientIdsWithoutPermissions(deleteAttribute.Permission);
                                await eventHub.Clients.AllExcept(unauthorizedClients).SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "batch-delete",
                                    Data = entities,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                            else
                            {
                                await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                                {
                                    Type = "batch-delete",
                                    Data = entities,
                                    Resource = mapGroupName.ToLower()
                                });
                            }
                        }

                        return Results.Ok($"Deleted {result} items");
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
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

    public static void MapAllEntityEndpoints<TId>(this IEndpointRouteBuilder endpointBuilder, Assembly? assembly = null,
        Action<IEndpointConventionBuilder>? routeOptionsAction = null)
    {
        assembly ??= Assembly.GetEntryAssembly()!;

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