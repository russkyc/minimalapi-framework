namespace Russkyc.MinimalApi.Framework;

internal class CrudEvent
{
    public string Resource { get; set; }
    public string Type { get; set; }
    public dynamic? Data { get; set; }
}