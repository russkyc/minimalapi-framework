using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Russkyc.MinimalApi.Framework.Core;

var connection = new HubConnectionBuilder()
    .WithUrl($"https://localhost:7102{ConfigurationStrings.RealtimeHubEndpoint}", options =>
    {
        options.Headers.Add(ConfigurationStrings.ApiPermissionHeader, "xcxs");
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<RealtimeEvent>(ConfigurationStrings.RealtimeEvent, obj =>
{
    var serialized = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
    {
        WriteIndented = true
    });
    Console.WriteLine(serialized);
});

await connection.StartAsync();
Console.Read();