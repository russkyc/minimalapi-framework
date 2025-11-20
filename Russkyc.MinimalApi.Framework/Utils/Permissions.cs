using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Core.Attributes;
using Russkyc.MinimalApi.Framework.Extensions;
using Russkyc.MinimalApi.Framework.Options;

namespace Russkyc.MinimalApi.Framework.Utils;

internal static class Permissions
{
    internal static bool HasPermission<TEntity>(HttpContext httpContext, ApiMethod method)
    {
        var permissionsAttributes = typeof(TEntity).GetAttributeValue<RequirePermission>();
        if (!permissionsAttributes.Any()) return true;
        var methods = permissionsAttributes.Select(attribute => attribute.Method);
        if (!methods.Contains(method)) return true;
        var attributePermissions = permissionsAttributes
            .Where(attribute => attribute.Method == method)
            .SelectMany(attribute => attribute.Permission);
        var permissions = httpContext.Request.Headers.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
        return permissions.Any(permission => attributePermissions.Any(permission.Equals));
    }

}