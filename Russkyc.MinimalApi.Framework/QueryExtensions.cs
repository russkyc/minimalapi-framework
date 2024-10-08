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

    internal static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, FilterDictionary filters) where T : class
    {
        var entityType = typeof(T);
        var parameter = Expression.Parameter(entityType, "e");

        foreach (var filter in filters)
        {
            try
            {
                var propertyInfo = entityType.GetProperty(filter.Key,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null || !propertyInfo.GetCustomAttributes<QueryableAttribute>().Any())
                {
                    continue;
                }

                var property = Expression.Property(parameter, propertyInfo);
                var value = Expression.Constant(Convert.ChangeType(filter.Value.Value, propertyInfo.PropertyType));

                Expression comparison = filter.Value.Operation switch
                {
                    "CONTAINS" => Expression.Call(property, "Contains", null, value),
                    "STARTSWITH" => Expression.Call(property, "StartsWith", null, value),
                    "ENDSWITH" => Expression.Call(property, "EndsWith", null, value),
                    "GREATERTHAN" => Expression.GreaterThan(property, value),
                    "LESSTHAN" => Expression.LessThan(property, value),
                    "GREATERTHANOREQUAL" => Expression.GreaterThanOrEqual(property, value),
                    "LESSTHANOREQUAL" => Expression.LessThanOrEqual(property, value),
                    "NOTEQUALS" => Expression.NotEqual(property, value),
                    _ => Expression.Equal(property, value),
                };

                var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
                query = query.Where(lambda);
            }
            catch (InvalidOperationException)
            {
                // Skip applying the filter if an InvalidOperationException occurs
                continue;
            }
            catch (FormatException)
            {
                // Skip applying the filter if a FormatException occurs
                continue;
            }
        }

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
            .Select(propertyInfo => Expression.Bind(propertyInfo, Expression.Property(parameter, propertyInfo)))
            .ToList();

        var selector = Expression.Lambda<Func<T, T>>(
            Expression.MemberInit(Expression.New(typeof(T)), bindings),
            parameter
        );

        return query.Select(selector);
    }
}