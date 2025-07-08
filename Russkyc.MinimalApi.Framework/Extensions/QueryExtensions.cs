using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework.Extensions;

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

    internal static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> query, string? orderBy, bool descending)
        where T : class
    {
        if (string.IsNullOrEmpty(orderBy))
        {
            return query;
        }

        var entityType = typeof(T);
        var properties = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList();

        if (properties.Count == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(entityType, "e");
        var firstProperty = entityType.GetProperty(properties[0],
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (firstProperty == null)
        {
            throw new ArgumentException($"Property '{properties[0]}' not found on type '{entityType.Name}'");
        }

        var firstPropertyAccess = Expression.MakeMemberAccess(parameter, firstProperty);
        var firstOrderByExpression = Expression.Lambda(firstPropertyAccess, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(typeof(Queryable), methodName,
            [entityType, firstProperty.PropertyType],
            query.Expression, Expression.Quote(firstOrderByExpression));

        for (int i = 1; i < properties.Count; i++)
        {
            var property = entityType.GetProperty(properties[i],
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                throw new ArgumentException($"Property '{properties[i]}' not found on type '{entityType.Name}'");
            }

            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);

            methodName = descending ? "ThenByDescending" : "ThenBy";
            resultExpression = Expression.Call(typeof(Queryable), methodName, [entityType, property.PropertyType],
                resultExpression, Expression.Quote(orderByExpression));
        }

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}