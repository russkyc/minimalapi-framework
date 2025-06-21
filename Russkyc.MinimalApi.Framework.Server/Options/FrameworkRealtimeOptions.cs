using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework.Server.Options;

public static class FrameworkRealtimeOptions
{
    public static string RealtimeEventsEndpoint { get; set; } = ConfigurationStrings.RealtimeHubEndpoint;
}