using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework;

internal static class QueryExtensions
{
    internal static IQueryable<T> ApplyIncludes<T>(this IQueryable<T> query, string? includes) where T : class
    {
        if (string.IsNullOrEmpty(includes))
        {
            return query;
        }

        var includeProperties = includes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLower());

        var entityType = typeof(T);
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name.ToLower(), p => p.Name);

        foreach (var includeProperty in includeProperties)
        {
            if (properties.TryGetValue(includeProperty, out var actualPropertyName))
            {
                var navigationProperty = entityType.GetProperty(actualPropertyName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (navigationProperty != null)
                {
                    query = query.Include(navigationProperty.Name);
                }
            }
        }

        return query;
    }

    internal static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string filter) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var expression = DynamicExpressionParser.ParseLambda([parameter], typeof(bool), filter);
        query = query.Where(expression);

        return query;
    }

    internal static IQueryable<T> SelectProperties<T>(this IQueryable<T> query, string? properties) where T : class
    {
        if (string.IsNullOrEmpty(properties))
        {
            return query.Cast<T>();
        }

        var propertyNames = properties.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim());

        var entityType = typeof(T);
        var parameter = Expression.Parameter(entityType, "e");

        var bindings = propertyNames
            .Select(propertyName => entityType.GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance))
            .Where(propertyInfo => propertyInfo != null)
#pragma warning disable CS8604 // Possible null reference argument.
            .Select(propertyInfo => Expression.Bind(propertyInfo, Expression.Property(parameter, propertyInfo)))
#pragma warning restore CS8604 // Possible null reference argument.
            .ToList();

        var selector = Expression.Lambda<Func<T, T>>(
            Expression.MemberInit(Expression.New(typeof(T)), bindings),
            parameter
        );

        return query.Select(selector);
    }
}