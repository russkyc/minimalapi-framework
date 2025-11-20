using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
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
            .SelectMany(attribute => attribute.Permission)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true && FrameworkOptions.EnableJwtAuthentication)
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = FrameworkOptions.JwtIssuer,
                        ValidAudience = FrameworkOptions.JwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FrameworkOptions.JwtKey ?? throw new InvalidOperationException("JwtKey must be set when EnableJwtAuthentication is true")))
                    };
                    user = handler.ValidateToken(token, validationParameters, out _);
                }
                catch
                {
                    // invalid token, remain unauthenticated
                }
            }
        }

        // collect permissions from header
        var permissionsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headerPermissions = httpContext.Request.Headers
            .GetCommaSeparatedValues(FrameworkOptions.PermissionHeader)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p));
        foreach (var p in headerPermissions) permissionsSet.Add(p);

        // also collect permissions from authenticated user's claims (common claim names: "permissions", "permission")
        if (user.Identity?.IsAuthenticated == true)
        {
            IEnumerable<string> claimPermissions = Enumerable.Empty<string>();

            claimPermissions = claimPermissions
                .Concat(user.FindAll("permissions").SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                .Concat(user.FindAll("permission").SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)));

            foreach (var cp in claimPermissions.Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)))
            {
                permissionsSet.Add(cp);
            }
        }

        var hasPermission = permissionsSet.Any(permission => attributePermissions.Any(ap => string.Equals(permission, ap, StringComparison.OrdinalIgnoreCase)));

        if (!hasPermission && FrameworkOptions.EnableRoleBasedPermissions && user.Identity?.IsAuthenticated == true)
        {
            var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
            hasPermission = userRoles.Any(role => attributePermissions.Any(ap => string.Equals(role, ap, StringComparison.OrdinalIgnoreCase)));
        }

        return hasPermission;
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