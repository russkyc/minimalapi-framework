using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var assembly = Assembly.GetExecutingAssembly();

builder.Services.AddAllEntityServices(assembly,
    options => options.UseInMemoryDatabase("sample"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGroup("api")
    .MapAllEntityEndpoints(assembly);

app.Run();