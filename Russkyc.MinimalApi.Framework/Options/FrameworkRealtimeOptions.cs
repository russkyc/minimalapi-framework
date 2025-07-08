using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework.Options;

public static class FrameworkRealtimeOptions
{
    public static string RealtimeEventsEndpoint { get; set; } = ConfigurationStrings.RealtimeHubEndpoint;
}