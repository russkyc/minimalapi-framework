using System.Collections;
using System.Reflection;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Extensions;
using Russkyc.MinimalApi.Framework.Options;

namespace Russkyc.MinimalApi.Framework.Utils;

internal static class Permissions
{
    internal static bool HasPermission<TEntity>(HttpContext httpContext, ApiMethod method)
    {
        return HasPermission(typeof(TEntity), httpContext, method);
    }

    internal static bool HasPermission(Type entityType, HttpContext httpContext, ApiMethod method)
    {
        var permissionsAttributes = entityType.GetAttributeValue<RequirePermission>();
        if (!permissionsAttributes.Any()) return true;
        var methods = permissionsAttributes.Select(attribute => attribute.Method);
        if (!methods.Contains(method)) return true;
        var attributePermissions = permissionsAttributes
            .Where(attribute => attribute.Method == method)
            .SelectMany(attribute => attribute.Permission);
        var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
        return permissions.Any(permission => attributePermissions.Any(permission.Equals));
    }

    private static IEnumerable<Type> GetNestedEntityTypes(Type type)
    {
        var types = new HashSet<Type>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = prop.PropertyType;
            if (propType.IsClass && propType != typeof(string))
            {
                if (propType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
                {
                    types.Add(propType);
                }

                // Recursive for nested
                types.UnionWith(GetNestedEntityTypes(propType));
            }

            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elemType = propType.GetGenericArguments()[0];
                if (elemType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
                {
                    types.Add(elemType);
                }

                types.UnionWith(GetNestedEntityTypes(elemType));
            }
        }

        return types;
    }

    internal static bool HasPermissionForGraph<TEntity>(HttpContext httpContext, ApiMethod method)
    {
        var types = new HashSet<Type> { typeof(TEntity) };
        types.UnionWith(GetNestedEntityTypes(typeof(TEntity)));
        return types.All(t => HasPermission(t, httpContext, method));
    }

    private static void AddNestedTypes(object obj, HashSet<Type> types)
    {
        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            if (value == null)
            {
                continue;
            }
            var propType = prop.PropertyType;
            if (propType.IsClass && propType != typeof(string))
            {
                if (propType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
                {
                    types.Add(propType);
                }

                AddNestedTypes(value, types);
            }

            if (!propType.IsGenericType || propType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                continue;
            }
            var elemType = propType.GetGenericArguments()[0];
            if (!elemType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbEntity<>)))
            {
                continue;
            }
            if (value is not IEnumerable enumerable)
            {
                continue;
            }
            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    continue;
                }
                types.Add(elemType);
                AddNestedTypes(item, types);
            }
        }
    }

    internal static bool HasPermissionForEntityGraph<TEntity>(TEntity entity, HttpContext httpContext, ApiMethod method)
    {
        if (entity == null) return true; // or false? but assume not null
        var types = new HashSet<Type> { typeof(TEntity) };
        AddNestedTypes(entity, types);
        return types.All(t => HasPermission(t, httpContext, method));
    }
}