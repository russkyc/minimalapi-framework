namespace Russkyc.MinimalApi.Framework.Core;

public class RealtimeEvent
{
    public required string Resource { get; set; }
    public required string Type { get; set; }
    public dynamic? Data { get; set; }
}