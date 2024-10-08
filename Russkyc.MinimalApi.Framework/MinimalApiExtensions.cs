using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Russkyc.MinimalApi.Framework;

public static class MinimalApiExtensions
{
    public static void AddEntityServices<T>(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where T : class
    {
        serviceCollection.AddDbContext<EntityContext<T>>(optionsAction, contextLifetime, optionsLifetime);
    }


    public static void MapEntityEndpoints<T>(this IEndpointRouteBuilder endpointBuilder, string? groupName = null, Action<IEndpointConventionBuilder>? routeOptionsAction = null)
    where T : class
{
    var mapGroupName = groupName ?? typeof(T).Name;
    var entityEndpointGroup = endpointBuilder.MapGroup($"{mapGroupName.ToLower()}");
    
    var getCollectionEndpoint = entityEndpointGroup
        .MapGet("/",
            async ([FromServices] EntityContext<T> context, [FromQuery] string? include,
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
        .MapGet("/{id:int}",
            async ([FromServices] EntityContext<T> context, [FromRoute] int id, [FromQuery] string? include,
                [FromQuery] string? property) =>
            {
                var query = context.Entities
                    .AsNoTracking()
                    .ApplyIncludes(include)
                    .SelectProperties(property);
                var entity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
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
        .MapPost("/", async ([FromServices] EntityContext<T> context, [FromBody] T entity) =>
        {
            var entryEntity = await context.Entities
                .AddAsync(entity);
            await context.SaveChangesAsync();
            return Results.Ok(entryEntity.Entity);
        })
        .WithName($"Add {mapGroupName}")
        .WithTags(mapGroupName)
        .WithOpenApi();
    
    var updateEntityEndpoint = entityEndpointGroup
        .MapPatch("/", async ([FromServices] EntityContext<T> context, [FromBody] T entity) =>
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
        .MapDelete("/{id:int}", async ([FromServices] EntityContext<T> context, [FromRoute] int id) =>
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

    public static void AddAllEntityServices(this IServiceCollection serviceCollection, Assembly assembly,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        var entityTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<DbEntityAttribute>() != null);

        foreach (var entityType in entityTypes)
        {
            var method = typeof(MinimalApiExtensions).GetMethod(nameof(AddEntityServices))?
                .MakeGenericMethod(entityType);
            if (optionsAction != null)
                method?.Invoke(null, [serviceCollection, optionsAction, contextLifetime, optionsLifetime]);
        }
    }

    public static void MapAllEntityEndpoints(this IEndpointRouteBuilder endpointBuilder, Assembly assembly, Action<IEndpointConventionBuilder>? routeOptionsAction = null)
    {
        var entityTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<DbEntityAttribute>() != null);

        foreach (var entityType in entityTypes)
        {
            var method = typeof(MinimalApiExtensions).GetMethod(nameof(MapEntityEndpoints))?
                .MakeGenericMethod(entityType);
            method?.Invoke(null, [endpointBuilder, null, routeOptionsAction]);
        }
    }
}