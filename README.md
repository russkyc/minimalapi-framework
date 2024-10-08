<h2 align="center">Russkyc.MinimalApi.Framework - A generic MinimalApi Crud Generator for EntityFrameworkCore</h2>

<p align="center">
    <img src="https://img.shields.io/nuget/v/Russkyc.MinimalApi.Framework?color=1f72de" alt="Nuget">
    <img src="https://img.shields.io/badge/-.NET%208.0-blueviolet?color=1f72de&label=NET" alt="">
    <img src="https://img.shields.io/github/license/russkyc/minimalapi-framework">
    <img src="https://img.shields.io/github/issues/russkyc/minimalapi-framework">
    <img src="https://img.shields.io/nuget/dt/Russkyc.MinimalApi.Framework">
</p>

This dynamically generates a generic CRUD API implementation
backed with Entity Framework Core and Minimal API. This can be used for quick prototyping
and for small apps that only require CRUD operations.

<img src="Resources/swagger.jpeg" style="width: 100%;" />

## Potential use-cases
- Quick API prototyping
- Small projects that only require CRUD functionality
- Frontend Testing (if a backend API is needed)

## Important things to consider
- When using generic implementations like this on the server side,
  business logic is now moved into the client and becomes a client concern.
- If your API needs to do complex business logic over the CRUD functionality,
  please consider implementing custom endpoints instead of using generic endpoints
  such as this.
- There is currently no implementation for validation and DTO mapping,
  this can be added later as the project updates.

Certainly! Here is a "Getting Started" section for the `README.md` file, including NuGet installation and setup instructions.

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

2. **Add the required services** in the `Program.cs` file:

    ```csharp
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;
    using Russkyc.MinimalApi.Framework;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var assembly = Assembly.GetExecutingAssembly();

    // Add required db services
    builder.Services.AddAllEntityServices(assembly, options => options.UseInMemoryDatabase("sample"));

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Map CRUD endpoints
    app.MapGroup("api")
        .MapAllEntityEndpoints<int>(assembly);

    app.Run();
    ```

### Setting-up entity classes

All entity classes should inherit from the DbEntity<TKeyType> abstract class.
Where `TKeyType` is the Id type of the entity.

**SampleEntity Class**

```csharp
public class SampleEntity : DbEntity<int>
{
    [Queryable]
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

#### Option 1: Entity registration

```csharp
var builder = youbApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add required db services
builder.Services.AddEntityServices<SampleEntity>(options => options.UseInMemoryDatabase("sample"));
builder.Services.AddEntityServices<SampleEmbeddedEntity>(options => options.UseInMemoryDatabase("sample"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map CRUD endpoints
app.MapGroup("api")
        .MapEntityEndpoints<SampleEntity,int>();
        .MapEntityEndpoints<SampleEmbeddedEntity,int>();

app.Run();
```

#### Option 2: Automatic entity discovery using reflection

```csharp
var builder = youbApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var assembly = Assembly.GetExecutingAssembly();

// Add required db services
builder.Services.AddAllEntityServices(assembly, options => options.UseInMemoryDatabase("sample"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map CRUD endpoints
app.MapGroup("api")
        .MapAllEntityEndpoints<int>(assembly);

app.Run();
```

### Advanced Route Options

You can modify the endpoint options using the `routeOptionsAction` parameter. For example, to require authorization for all endpoints:

```csharp
app.MapGroup("api")
    .MapAllEntityEndpoints<int>(assembly, routeOptions => routeOptions.RequireAuthorization());
```

### Advanced Querying

Apart from the standard CRUD api functionality, there is also some support for
advanced querying.

#### Entity Framework Core Navigation Properties

If you do a get requrest to the endpoint `/api/sampleentity` you will
recieve a response that looks like this:

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
#### Filter query support

Querying entities based on the property values can be done in this format `?filters=PROPERTNAME=VALUE`. As an example:
```http
GET /api/sampleentity?filters=property=Example
```

#### Wildcards support in filters parameter

Wildcards are supported in the `filters` query parameter to perform more flexible searches:

- **CONTAINS**: Matches entities where the property contains the specified value.
  ```http
  GET /api/sampleentity?filters=property=CONTAINS(Example)
  ```

- **STARTSWITH**: Matches entities where the property starts with the specified value.
  ```http
  GET /api/sampleentity?filters=property=STARTSWITH(Exa)
  ```

- **ENDSWITH**: Matches entities where the property ends with the specified value.
  ```http
  GET /api/sampleentity?filters=property=ENDSWITH(mple)
  ```

- **GREATERTHAN**: Matches entities where the property is greater than the specified value.
  ```http
  GET /api/sampleentity?filters=property=GREATERTHAN(10)
  ```

- **LESSTHAN**: Matches entities where the property is less than the specified value.
  ```http
  GET /api/sampleentity?filters=property=LESSTHAN(20)
  ```

- **GREATERTHANOREQUAL**: Matches entities where the property is greater than or equal to the specified value.
  ```http
  GET /api/sampleentity?filters=property=GREATERTHANOREQUAL(20)
  ```

- **LESSTHANOREQUAL**: Matches entities where the property is less than or equal to the specified value.
  ```http
  GET /api/sampleentity?filters=property=LESSTHANOREQUAL(20)
  ```

- **NOTEQUALS**: Matches entities where the property is not equal to the specified value.
  ```http
  GET /api/sampleentity?filters=property=NOTEQUALS(20)
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
PATCH /api/sampleentity/batch?filters=property=CONTAINS(Example)
Content-Type: application/json

{
  "property": "Updated Value"
}
```

#### Batch Delete

```http
DELETE /api/sampleentity/batch?filters=property=CONTAINS(Example)
```