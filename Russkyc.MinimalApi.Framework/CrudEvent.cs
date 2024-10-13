namespace Russkyc.MinimalApi.Framework;

internal class CrudEvent
{
    public required string Resource { get; set; }
    public required string Type { get; set; }
    public dynamic? Data { get; set; }
}