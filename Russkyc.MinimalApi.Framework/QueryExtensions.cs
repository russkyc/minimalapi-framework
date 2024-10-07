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
}