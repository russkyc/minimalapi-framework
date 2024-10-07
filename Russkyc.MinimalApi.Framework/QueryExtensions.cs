using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework;

public static class QueryExtensions
{
    public static IQueryable<T> ApplyIncludes<T>(this IQueryable<T> query, string? includes) where T : class
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
                query = query.Include(actualPropertyName);
            }
        }

        return query;
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, Dictionary<string, string> filters)
        where T : class
    {
        var entityType = typeof(T);
        var parameter = Expression.Parameter(entityType, "e");

        foreach (var filter in filters)
        {
            var propertyInfo = entityType.GetProperty(filter.Key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null || !propertyInfo.GetCustomAttributes<QueryableAttribute>().Any())
            {
                continue;
            }

            var property = Expression.Property(parameter, propertyInfo);
            var value = Expression.Constant(Convert.ChangeType(filter.Value, propertyInfo.PropertyType));
            var comparison = Expression.Equal(property, value);

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        return query;
    }
}