namespace Russkyc.MinimalApi.Framework.Core;

public class EntityEvent<T>
{
    public string Resource { get; set; }
    public string Type { get; set; }
    public T? Data { get; set; }
}