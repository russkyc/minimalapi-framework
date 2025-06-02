using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// You can implement your own auth as needed
// As an example, uncomment this line if you want JWT auth
// builder.Services.AddJwtAuth();
// IMPORTANT: Please remove the AddSwaggerGen if you uncomment this line.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Uncomment to add required service for realtime events
builder.Services.AddRealtimeService();

// The entity endpoints uses a BaseDbContext to access the database,
// We can register this automatically by using this extension method
// NOTE: We are using sqlite for this example, but you can use all other EF Core providers
// For database creation, the `databaseAction` sets how it will be created.
// By default, it is set to `DatabaseAction.EnsureCreated`
builder.Services.AddDbContextService(optionsAction: options => options.UseSqlite("Data Source=test.sqlite"));

// If the Entity classes are not in the same assembly, you can specify the assembly where they are located.
// builder.Services.AddDbContextService(assembly: <assembly>, optionsAction: options => options.UseSqlite("Data Source=test.sqlite"));

// To use migrations, we can also define and register our own DbContext using a generic overload
// `AddDbContextService<T>()`. The custom DbContext need to inherit from `BaseDbContext` and
// ensure that all entity DbSets are implemented with naming as `<EntityType>Collection`
// eg;`public DbSet<SampleEntity> SampleEntityCollection { get; set; }`
// An example implementation can be found at `CustomDbContext.cs`

// builder.Services.AddDbContextService<CustomDbContext>(
//     options => options.UseSqlite("Data Source=test.sqlite"),
//     databaseAction: DatabaseAction.EnsureCreated);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference(options =>
    {
        options.WithSidebar(false);
        options.WithLayout(ScalarLayout.Classic);
    });
}

app.UseHttpsRedirection();

// We map endpoints for all discovered entities in the assembly
// MapGroup is not required, we do this to make all the entity routes
// have the `/api` prefix
app.MapGroup("api")
    .MapAllEntityEndpoints<Guid>();

// You can also set the assembly where the entities are located if they are not in the same assembly
// app.MapGroup("api")
//     .MapAllEntityEndpoints<Guid>(assembly: <assembly>);

// Uncomment to enable realtime events
// by default the endpoint used is "/crud-events"
// this can be changed by providing a string parameter, eg; `MapRealtimeHub("/api-events")`
app.MapRealtimeHub();

// You can modify the endpoint options,
// in this case, use this instead if you want the mapped entity routes
// to require Authorization

//app.MapGroup("api")
//    .MapAllEntityEndpoints(assembly, routeOptions => routeOptions.RequireAuthorization());

// You can implement your own auth as needed
// As an example, uncomment this line if you want JWT auth
// app.UseJwtAuth();

app.Run();