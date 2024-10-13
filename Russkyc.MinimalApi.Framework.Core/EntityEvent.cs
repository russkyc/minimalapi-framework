namespace Russkyc.MinimalApi.Framework.Core;

public class EntityEvent<T>
{
    public required string Resource { get; set; }
    public required string Type { get; set; }
    public T? Data { get; set; }
}