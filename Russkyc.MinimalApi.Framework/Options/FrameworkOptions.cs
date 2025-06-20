using System.Reflection;

namespace Russkyc.MinimalApi.Framework.Options;

public static class FrameworkOptions
{
    public static Assembly? EntityClassesAssembly { get; set; } = null;
    public static bool EnableRealtimeEvents { get; set; } = false;
    public static bool AutoRedirectToApiDocs { get; set; } = true;
    public static bool EnableApiDocs { get; set; } = true;
    public static bool UseHttpsRedirection { get; set; } = false;
    public static string? ApiPrefix { get; set; }
    public static string PermissionHeader { get; set; } = "x-api-permission";
}