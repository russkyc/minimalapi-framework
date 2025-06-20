using Scalar.AspNetCore;

namespace Russkyc.MinimalApi.Framework.Options;

public static class FrameworkApiDocsOptions
{
    public static bool EnableSidebar { get; set; } = false;
    public static ScalarLayout Layout { get; set; } = ScalarLayout.Classic;
    public static ScalarTheme Theme { get; set; } = ScalarTheme.Default;
    
}