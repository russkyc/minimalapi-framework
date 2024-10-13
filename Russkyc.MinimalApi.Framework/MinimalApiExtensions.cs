using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework;

public static class MinimalApiExtensions
{
    public static void MapRealtimeHub(this IEndpointRouteBuilder endpointBuilder, string endpoint = "/crud-events")
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
                async (
                    [FromServices] BaseDbContext context,
                    [FromQuery] string? include,
                    [FromQuery] string? filter,
                    [FromQuery] string? property,
                    [FromQuery] int page = 1,
                    [FromQuery] int pageSize = 10,
                    [FromQuery] bool paginate = false) =>
                {
                    try
                    {
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
            .WithName($"Get a {mapGroupName} collection")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var getSingleEntityEndpoint = entityEndpointGroup
            .MapGet("/{id}",
                async ([FromServices] BaseDbContext context, [FromRoute] TKeyType id,
                    [FromQuery] string? include,
                    [FromQuery] string? property) =>
                {
                    try
                    {
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
            .WithDescription($"Get a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntityEndpoint = entityEndpointGroup
            .MapPost("/",
                async (
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices] BaseDbContext context,
                    [FromBody] TEntity entity) =>
                {
                    try
                    {
                        var dbSet = context.DbSet<TEntity>();

                        var existingEntity = await dbSet.FindAsync(((IDbEntity<TKeyType>)entity).Id);
                        if (existingEntity != null)
                        {
                            return Results.Conflict("An entity with the same key already exists.");
                        }

                        var entryEntity = await dbSet.AddAsync(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null)
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "create",
                                Data = entryEntity.Entity,
                                Resource = mapGroupName.ToLower()
                            });
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
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices] BaseDbContext context,
                    [FromBody] TEntity entity) =>
                {
                    try
                    {
                        var dbSet = context.DbSet<TEntity>();

                        var entryEntity = dbSet.Update(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null)
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "update",
                                Data = entryEntity.Entity,
                                Resource = mapGroupName.ToLower()
                            });
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
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices] BaseDbContext context,
                    [FromRoute] TKeyType id) =>
                {
                    try
                    {
                        var dbSet = context.DbSet<TEntity>();

                        var entity = await dbSet.FindAsync(id);
                        if (entity is null)
                        {
                            return Results.NotFound();
                        }

                        dbSet.Remove(entity);
                        await context.SaveChangesAsync();

                        if (eventHub is not null)
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "delete",
                                Data = entity,
                                Resource = mapGroupName.ToLower()
                            });
                        }

                        return Results.Ok(entity);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(e.Message);
                    }
                })
            .WithDescription($"Delete a single {mapGroupName}")
            .WithTags(mapGroupName)
            .WithOpenApi();

        var addEntitiesEndpoint = entityEndpointGroup
            .MapPost("/batch", async (
                [FromServices, Optional] IHubContext<EventHub>? eventHub,
                [FromServices] BaseDbContext context,
                [FromBody] TEntity[] entities) =>
            {
                try
                {
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

                    if (eventHub is not null)
                    {
                        await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                        {
                            Type = "batch-create",
                            Data = entityEntries,
                            Resource = mapGroupName.ToLower()
                        });
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
                [FromServices, Optional] IHubContext<EventHub>? eventHub,
                [FromServices] BaseDbContext context,
                [FromBody] TEntity[] entities) =>
            {
                try
                {
                    var dbSet = context.DbSet<TEntity>();

                    dbSet.UpdateRange(entities);
                    var result = await context.SaveChangesAsync();

                    if (eventHub is not null)
                    {
                        await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                        {
                            Type = "update",
                            Data = entities,
                            Resource = mapGroupName.ToLower()
                        });
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
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices] BaseDbContext context,
                    [FromQuery] string? filter, [FromBody] Dictionary<string, object> updateFields) =>
                {
                    try
                    {
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

                        if (eventHub is not null)
                        {
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "batch-update",
                                Data = entityList,
                                Resource = mapGroupName.ToLower()
                            });
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
                    [FromServices, Optional] IHubContext<EventHub>? eventHub,
                    [FromServices] BaseDbContext context,
                    [FromQuery] string? include,
                    [FromQuery] string? filter) =>
                {
                    try
                    {
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
                            await eventHub.Clients.All.SendAsync("crud-event", new CrudEvent
                            {
                                Type = "batch-delete",
                                Data = entities,
                                Resource = mapGroupName.ToLower()
                            });
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