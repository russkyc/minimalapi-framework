using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

var builder = WebApplication.CreateBuilder(args);

// You can implement your own auth as needed
// As an example, uncomment this line if you want JWT auth
// builder.Services.AddJwtAuth();
// IMPORTANT: Please remove the AddSwaggerGen if you uncomment this line.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var assembly = Assembly.GetExecutingAssembly();

// The entity endpoints need an EF Core DBContext implementation for each entity
// We can do this automatically by using this extension method
// NOTE: We are using in memory for this example, but you can use all other EF Core providers
builder.Services.AddAllEntityServices(assembly,
    options => options.UseInMemoryDatabase("sample"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// We map endpoints for all discovered entities in the assembly
// MapGroup is not required, we do this to make all the entity routes
// have the `/api` prefix
app.MapGroup("api")
    .MapAllEntityEndpoints<Guid>(assembly);

// You can modify the endpoint options,
// in this case, use this instead if you want the mapped entity routes
// to require Authorization

//app.MapGroup("api")
//    .MapAllEntityEndpoints(assembly, routeOptions => routeOptions.RequireAuthorization());

// You can implement your own auth as needed
// As an example, uncomment this line if you want JWT auth
// app.UseJwtAuth();

app.Run();