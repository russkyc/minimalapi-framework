using Microsoft.AspNetCore.SignalR;
using Russkyc.MinimalApi.Framework.Server.Options;

namespace Russkyc.MinimalApi.Framework.Server.Realtime;

internal class EventHub : Hub
{
    private readonly RealtimeClientStore _clientStore;

    public EventHub(RealtimeClientStore clientStore)
    {
        _clientStore = clientStore;
    }

    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var headers = httpContext?.Request.Headers;
        var permissions = headers?.GetCommaSeparatedValues(FrameworkOptions.PermissionHeader);
        _clientStore.AddClient(Context.ConnectionId, permissions);
        return base.OnConnectedAsync();
    }
    
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _clientStore.RemoveClient(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

}