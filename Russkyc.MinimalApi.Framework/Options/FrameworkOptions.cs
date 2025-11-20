using System.Reflection;

namespace Russkyc.MinimalApi.Framework.Options;

public static class FrameworkOptions
{
    public static Assembly? EntityClassesAssembly { get; set; } = null;
    public static bool EnableRealtimeEvents { get; set; } = true;
    public static bool MapIndexToApiDocs { get; set; } = true;
    public static bool EnableApiDocs { get; set; } = true;
    public static string? ApiPrefix { get; set; }
    public static string PermissionHeader { get; set; } = "x-api-permission";
    public static bool EnableRoleBasedPermissions { get; set; } = false;
    public static bool EnableJwtAuthentication { get; set; } = false;
    public static string? JwtIssuer { get; set; }
    public static string? JwtAudience { get; set; }
    public static string? JwtKey { get; set; }
    public static bool EnableCookieAuthentication { get; set; } = false;
}