namespace Russkyc.MinimalApi.Framework.Realtime;

public class RealtimeClientStore
{
    private readonly Dictionary<string, string[]?> _clients = new();
    
    public void AddClient(string clientId, string[]? permissions)
    {
        _clients.Add(clientId, permissions);
    }
    
    public void RemoveClient(string clientId)
    {
        _clients.Remove(clientId);
    }
    
    public IEnumerable<string> GetClientIdsWithoutPermissions(params string[] permissions)
    {
        return _clients
            .Where(kv => kv.Value == null || !kv.Value.Any(permissions.Contains))
            .Select(kv => kv.Key);
    }
}