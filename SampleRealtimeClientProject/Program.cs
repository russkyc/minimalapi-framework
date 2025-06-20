using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Russkyc.MinimalApi.Framework.Core;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7102/realtime", options =>
    {
        options.Headers.Add("x-api-permission", "xcxs");
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<CrudEvent>("crud-event", obj =>
{
    var serialized = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
    {
        WriteIndented = true
    });
    Console.WriteLine(serialized);
});

await connection.StartAsync();
Console.Read();