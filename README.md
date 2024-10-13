<h2 align="center">Russkyc.MinimalApi.Framework - A Generic API Crud Generator for EntityFrameworkCore</h2>

<p align="center">
    <img src="https://img.shields.io/nuget/v/Russkyc.MinimalApi.Framework?color=1f72de" alt="Nuget">
    <img src="https://img.shields.io/badge/-.NET%208.0-blueviolet?color=1f72de&label=NET" alt="">
    <img src="https://img.shields.io/github/license/russkyc/minimalapi-framework">
    <img src="https://img.shields.io/github/issues/russkyc/minimalapi-framework">
    <img src="https://img.shields.io/nuget/dt/Russkyc.MinimalApi.Framework">
</p>

Dynamically generates a generic CRUD API implementation
backed with Entity Framework Core and Minimal API. This can be used
to create a quick backend for prototyping apps that use CRUD operations.

<img src="Resources/swagger.jpeg" style="width: 100%;" />

> ### ✨ What's New:
> - Realtime support using SignalR [learn more...](#realtime-support)
> - Advanced querying with filtering, ordering, and pagination [learn more...](#advanced-querying)
> - Batch endpoints for adding, updating, and deleting multiple entities [learn more...](#batch-endpoints)

## Potential use-cases

- Quick API prototyping
- Small projects that only require CRUD functionality
- Frontend Testing (if a backend API is needed)

## Getting Started

### Installation

To install the `Russkyc.MinimalApi.Framework` package, you can use the NuGet Package Manager or the .NET CLI.

#### Using .NET CLI

Run the following command in your terminal:

```sh
dotnet add package Russkyc.MinimalApi.Framework
```

### Setup

Follow these steps to set up the `Russkyc.MinimalApi.Framework` in your project.

1. **Create a new ASP.NET Core Web API project** if you don't already have one.

2. **Add the required services and API endpoint mappings** in the `Program.cs` file:

    ```csharp
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;
    using Russkyc.MinimalApi.Framework;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var assembly = Assembly.GetExecutingAssembly();

    // Add Entity Context Services
    builder.Services.AddDbContextService(assembly, options => options.UseInMemoryDatabase("sample"));

    // Uncomment to enable realtime events
    // by default the endpoint used is "/crud-events"
    // this can be changed by providing a string parameter, eg; `MapRealtimeHub("/api-events")`
    builder.Services.AddRealtimeService();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Map Entity CRUD Endpoints
    app.MapGroup("api")
        .MapAllEntityEndpoints<int>(assembly);

    // Map Realtime Hub
    app.MapRealtimeHub();

    app.Run();
    ```

### Setting-up entity classes

All entity classes should inherit from the DbEntity<TKeyType> abstract class.
Where `TKeyType` is the Id type of the entity.

**SampleEntity Class**

```csharp
public class SampleEntity : DbEntity<int>
{
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity EmbeddedEntity { get; set; }
}
```

**SampleEmbeddedEntity Class**

```csharp
public class SampleEmbeddedEntity : DbEntity<int>
{
    public string Property2 { get; set; }
}
```

You now have a fully working EntityFrameworkCore backed MinimalApi CRUD project.

### Advanced Setup

#### Custom DbContext (Useful for migrations support)

To support database migrations, we can create a custom dbcontext class that inherits from `BaseDbContext`

```csharp
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

namespace SampleProject;

public class CustomDbContext : BaseDbContext
{
    public CustomDbContext(DbContextOptions options) : base(options)
    {
    }
    
    // Entity collections are required to be defined
    // using the naming convention `<ClassName>Collection`
    
    public DbSet<SampleEmbeddedEntity> SampleEmbeddedEntityCollection { get; set; }
    public DbSet<SampleEntity> SampleEntityCollection { get; set; }
}
```

### Database Initialization

These are different database action types that can be defined in the `AddAllEntityServices` or `AddEntityServices`
extensions using the `databaseAction` parameter:

- None - Default Action
- DatabaseAction.EnsureCreated - Run `Database.EnsureCreated()`
- DatabaseAction.DeleteAndCreate - Run `Database.EnsureDeleted()` before `Database.EnsureCreated()`

As an example:

```csharp
builder.Services.AddDbContextService(assembly,
    options => options.UseSqlite("Data Source=test.sqlite"),
    databaseAction: DatabaseAction.DeleteAndCreate);
```

This ensures that a fresh database is used in every run. If you only need
to set the database once, `DatabaseAction.EnsureCreated` is the recommended option

### Advanced Route Options

You can modify the endpoint options using the `routeOptionsAction` parameter. For example, to require authorization for
all endpoints:

```csharp
app.MapGroup("api")
    .MapAllEntityEndpoints<int>(assembly, routeOptions => routeOptions.RequireAuthorization());
```

### Advanced Querying

Apart from the standard CRUD api functionality, there is also some support for
advanced querying.

#### Entity Framework Core Navigation Properties

If you do a get request to the endpoint `/api/sampleentity` you will
receive a response that looks like this:

```json
[
  {
    "id": 1,
    "property": "Entity 1",
    "embeddedEntity": null
  },
  {
    "id": 2,
    "property": "Entity 2",
    "embeddedEntity": null
  },
  {
    "id": 3,
    "property": "Entity 3",
    "embeddedEntity": null
  },
  {
    "id": 4,
    "property": "Entity 4",
    "embeddedEntity": null
  }
]
```

This is because navigation properties for referenced entities are not
automatically included (for performance purposes). you can use the `include`
query parameter to include the referenced entity when needed.

```http
GET /api/sampleentity?include=embeddedentity
```

Then you will have this result:

```json
[
  {
    "id": 1,
    "property": "Entity 1",
    "embeddedEntity": {
      "id": 1,
      "property2": "Embedded Entity 1"
    }
  },
  {
    "id": 2,
    "property": "Entity 2",
    "embeddedEntity": {
      "id": 2,
      "property2": "Embedded Entity 2"
    }
  },
  {
    "id": 3,
    "property": "Entity 3",
    "embeddedEntity": {
      "id": 3,
      "property2": "Embedded Entity 3"
    }
  },
  {
    "id": 4,
    "property": "Entity 4",
    "embeddedEntity": {
      "id": 4,
      "property2": "Embedded Entity 4"
    }
  }
]
```

#### Filter query support (with the help of DynamicExpressionParser in System.Linq.Dynamic.Core)

Entities can now be filtered with the `filter` queryParam and supports standard expressions. Parameters should be
prefixed with `@` in order to be valid, eg; a parameter named `Content` should be used as `@Content`. Here are a few
examples:

```http
GET /api/sampleentity?filter=@Content.StartsWith("hello")
```

```http
GET /api/sampleentity?filter=@Content.StartsWith("hi") && !@Content.Contains("user")
```

```http
GET /api/sampleentity?filter=@Count == 1 || @Count > 8
```

```http
GET /api/sampleentity?filter=@ContactPerson != null
```

These are visualized for readability, in actual use, the filter value should be URL Encoded.

#### Ordering Support

Entities can now be ordered using the `orderBy` and `orderByDescending` query parameters. Multiple properties can be specified for ordering, separated by commas. The first property will be ordered using `OrderBy` or `OrderByDescending`, and subsequent properties will be ordered using `ThenBy` or `ThenByDescending`.

```http
GET /api/sampleentity?orderBy=property,embeddedEntity.property2&orderByDescending=true
```
the `orderBy` query param will define what properties are taken into consideration in ordering.
the `orderByDescending` query param is a bool property that changes the behavior to descending when set to true.
#### Pagination

By default, pagination is disabled and the query collection response returns something like this

```json
[
  {
    "id": 1,
    "property": "Entity 1",
    "embeddedEntity": {
      "id": 1,
      "property2": "Embedded Entity 1"
    }
  },
  {
    "id": 2,
    "property": "Entity 2",
    "embeddedEntity": {
      "id": 2,
      "property2": "Embedded Entity 2"
    }
  }
]
```

To enable pagination, set the `paginate` query param to true
and set the `page`, `pageSize` query params as needed. as an example:

```http
GET /api/sampleentity?paginate=true&page=1&pageSize=1
```

This will now return a `PaginatedCollection` object with this JSON schema:

```json
{
  "data": [
    {
      "property": "Entity 1",
      "embeddedEntity": null,
      "id": "84e93f60-b2bc-4303-af0a-c51c205addb9"
    }
  ],
  "page": 1,
  "pageSize": 1,
  "totalRecords": 2,
  "totalPages": 2
}
```

### Batch Endpoints

Batch endpoints are supported for adding, updating, and deleting multiple entities at once.

#### Batch Insert

```http
POST /api/sampleentity/batch
Content-Type: application/json

[
  {
    "id": 1,
    "property": "Entity 1",
    "embeddedEntity": null
  },
  {
    "id": 2,
    "property": "Entity 2",
    "embeddedEntity": null
  }
]
```

#### Batch Update

```http
PUT /api/sampleentity/batch
Content-Type: application/json

[
  {
    "id": 1,
    "property": "Updated Entity 1",
    "embeddedEntity": null
  },
  {
    "id": 2,
    "property": "Updated Entity 2",
    "embeddedEntity": null
  }
]
```

#### Batch Update with Filters and Dynamic Fields

```http
PATCH /api/sampleentity/batch?filter=@property.Contains("Old")
Content-Type: application/json

{
  "property": "Updated Value"
}
```

#### Batch Delete

```http
DELETE /api/sampleentity/batch?filter=@Count > 8
```

### Realtime Support

To enable realtime events support, add the `AddRealtimeService` method to your service collection and map the realtime hub using `MapRealtimeHub` in your `Program.cs` file:

```csharp
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var assembly = Assembly.GetExecutingAssembly();

// Add Entity Context Services
builder.Services.AddDbContextService(assembly, options => options.UseInMemoryDatabase("sample"));

// Add Realtime Service
builder.Services.AddRealtimeService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map Entity CRUD Endpoints
app.MapGroup("api")
    .MapAllEntityEndpoints<int>(assembly);

// Map Realtime Hub
app.MapRealtimeHub();

app.Run();
```
The default websocket endpoint is `/crud-events` but can be changed by
adding a string parameter of the desired endpint in the `MapRealtimeHub`
method, eg; `.MapRealtimeHub("/api-events")`.

Each event returned is an `EntityEvent<T>` type, where T is the type of data
returned by the resource event that triggered the websocket message. Eg; when
creating a `SampleEntity` using `post` the `EntityEvent` being sent in realtime is of
type `EntityEvent<SampleEntity>`.

#### EntityEvent<T> Class

```csharp
public class EntityEvent<T>
{
    public string Resource { get; set; }
    public string Type { get; set; }
    public T? Data { get; set; }
}
```
**Schema Information**

- The `Resource` property will be the name of the entity in lowercase, eg; "sampleentity".
- The `Type` property will be the type of event, eg; `created`, `updated`, `deleted`, `batch-created`, `batch-updated`, `batch-deleted`
- The `Data` property contains the data being returned by that resource method.

## Important things to consider

- When using generic implementations like this on the server side,
  business logic is now moved into the client and becomes a client concern.
- If your API needs to do complex business logic over the CRUD functionality,
  please consider implementing custom endpoints instead of using generic endpoints
  such as this.
- There is currently no implementation for validation and DTO mapping,
  this can be added later as the project updates.